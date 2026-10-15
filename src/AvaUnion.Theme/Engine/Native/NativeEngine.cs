using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;

namespace AvaUnion.Engine.Native;

/// <summary>
/// The engine that uses Union through libavaunion-native. Qt runs on the
/// Avalonia UI thread. All calls are synchronous.
/// </summary>
public sealed unsafe class NativeEngine : IUnionEngine, IDisposable
{
    private static NativeEngine? s_instance;

    private readonly Dictionary<int, string> _strings = new();
    private readonly Dictionary<string, int> _stringIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UnionStyleHandle?> _styles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UnionStylePackage> _appPackages = new(StringComparer.Ordinal);
    private readonly List<string> _appOrder = [];
    private readonly AppStyleLog _appStyleLog = new();
    private readonly IntPtr _defaultHandle;
    private readonly Func<int, string?> _stringLookup;
    private IReadOnlyList<UnionStyleMetadata>? _stylesList;
    private NativePump? _pump;
    private int _epoch;
    private string? _colorScheme;
    private UnionFonts? _fonts;
    private UnionPlatform? _platform;
    private bool _disposed;

    private NativeEngine(AvuInitResult init, string shimVersion)
    {
        _stringLookup = Str;
        PumpMode = init.PumpMode;
        PlatformThemeLoaded = init.PlatformThemeLoaded != 0;
        PlatformPlugin = Str(init.PlatformPluginId) ?? string.Empty;
        QtVersion = Str(init.QtVersionId) ?? string.Empty;
        UnionVersion = Str(init.UnionVersionId) ?? shimVersion;
        _defaultHandle = NativeMethods.StyleDefault();
        DefaultStyle = new UnionStyleHandle(StyleId(_defaultHandle), true, _defaultHandle, InstalledMetadata(_defaultHandle));
    }

    /// <summary>The Union version that the shim uses.</summary>
    public string UnionVersion { get; }

    /// <summary>The Qt version in the process.</summary>
    public string QtVersion { get; }

    /// <summary>The class name of the Union platform plugin.</summary>
    public string PlatformPlugin { get; }

    /// <summary>True when the KDE platform theme is loaded.</summary>
    public bool PlatformThemeLoaded { get; }

    /// <summary>How Qt events run. 0 is a pump timer. 1 is the shared GLib loop.</summary>
    public int PumpMode { get; }

    /// <inheritdoc/>
    public UnionEngineKind Kind => UnionEngineKind.Native;

    /// <inheritdoc/>
    public string Description =>
        $"Native · Union {UnionVersion} · style {DefaultStyle.Id}{(DefaultStyle.Metadata.Origin == UnionStyleOrigin.Application ? " (app)" : string.Empty)}{(PlatformThemeLoaded ? " · KDE platform theme" : string.Empty)}";

    /// <inheritdoc/>
    public UnionStyleHandle DefaultStyle { get; }

    /// <inheritdoc/>
    public string? ColorScheme => _colorScheme;

    /// <inheritdoc/>
    public int Epoch => _epoch;

    /// <inheritdoc/>
    public event Action<UnionChange>? Changed;

    /// <summary>Occurs when Union writes a warning to the Qt log.</summary>
    public event Action<string>? Log;

    /// <summary>
    /// Loads the shim, compares the versions and starts Qt on the calling
    /// thread. The calling thread must be the Avalonia UI thread.
    /// </summary>
    /// <exception cref="NativeInitException">The native engine cannot start. The message gives the cause.</exception>
    public static NativeEngine Create(bool hostRunsGLib = false)
    {
        if (s_instance is not null)
        {
            return s_instance;
        }

        if (!NativeLibraryResolver.TryLoad(out var reason))
        {
            throw new NativeInitException(reason);
        }

        AvuProbeResult probe;
        var status = NativeMethods.Probe(&probe);
        var shim = Ascii(probe.ShimUnionVersion);
        var loaded = Ascii(probe.LoadedUnionVersion);
        if (probe.AbiVersion != NativeMethods.AbiVersion)
        {
            throw new NativeInitException($"shim ABI {probe.AbiVersion}, expected {NativeMethods.AbiVersion}");
        }

        if (probe.ResolvedSize != sizeof(AvuResolved))
        {
            throw new NativeInitException($"avu_resolved is {probe.ResolvedSize} bytes in the shim, {sizeof(AvuResolved)} here");
        }

        if (status != NativeMethods.Ok)
        {
            throw new NativeInitException($"shim built for Union {shim}, loaded Union {loaded} (status {status})");
        }

        var options = new AvuInitOptions { HostRunsGLib = hostRunsGLib ? 1 : 0 };
        AvuInitResult init;
        status = NativeMethods.Init(&options, &init);
        if (status == NativeMethods.ErrorForeignApplication)
        {
            throw new NativeInitException("a Qt application already exists on another thread");
        }

        if (status != NativeMethods.Ok)
        {
            throw new NativeInitException($"avu_init failed ({status})");
        }

        var engine = new NativeEngine(init, shim);
        AvuStyleInfo info;
        if (NativeMethods.StyleInfo((IntPtr)engine.DefaultStyle.Payload, &info) == NativeMethods.Ok && info.HasErrors != 0)
        {
            throw new NativeInitException($"style {engine.DefaultStyle.Id} has CSS errors");
        }

        NativeMethods.SetCallback(&OnCallback, null);
        if (engine.PumpMode == NativeMethods.PumpTimer)
        {
            engine._pump = new NativePump();
        }

        s_instance = engine;
        return engine;
    }

    /// <summary>Runs the queued Qt work now.</summary>
    public int Pump() => NativeMethods.Pump(2);

    /// <inheritdoc/>
    public UnionStyleHandle? GetStyle(string id)
    {
        if (_styles.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var result = _appPackages.TryGetValue(id, out var package) ? LoadPackage(package) : null;
        if (result is null)
        {
            IntPtr handle;
            fixed (char* p = id)
            {
                handle = NativeMethods.StyleById(p, id.Length);
            }

            result = handle == IntPtr.Zero ? null : new UnionStyleHandle(id, false, handle, InstalledMetadata(handle));
        }

        _styles[id] = result;
        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<UnionStyleMetadata> Styles => _stylesList ??= ListStyles();

    internal IReadOnlyList<string> AppStyleLogLines => _appStyleLog.Lines;

    internal void AddStylePackages(IEnumerable<UnionStylePackage> packages)
    {
        foreach (var package in packages)
        {
            if (_appPackages.TryAdd(package.Id, package))
            {
                _appOrder.Add(package.Id);
                _styles.Remove(package.Id);
            }
        }

        _stylesList = null;
        RefreshDefaultStyle();
    }

    internal void RemoveStylePackages()
    {
        foreach (var id in _appOrder)
        {
            _styles.Remove(id);
        }

        _appPackages.Clear();
        _appOrder.Clear();
        _stylesList = null;
        RefreshDefaultStyle();
    }

    private UnionStyleHandle? LoadPackage(UnionStylePackage package)
    {
        if (package.Directory is null)
        {
            _appStyleLog.Unavailable(this, package.Id, "no package folder for the native engine");
            return null;
        }

        IntPtr handle;
        int status;
        fixed (char* p = package.Directory)
        {
            status = NativeMethods.StyleFromPackage(p, package.Directory.Length, &handle);
        }

        if (status != NativeMethods.Ok || handle == IntPtr.Zero)
        {
            _appStyleLog.Unavailable(this, package.Id, StatusReason(status));
            return null;
        }

        AvuStyleInfo info;
        var metadata = NativeMethods.StyleInfo(handle, &info) == NativeMethods.Ok ? Str(info.MetadataString) : null;
        if (info.HasErrors != 0)
        {
            _appStyleLog.HasErrors(this, package.Id);
        }

        return new UnionStyleHandle(package.Id, false, handle, UnionStyleMetadata.Parse(package.Id, metadata, UnionStyleOrigin.Application));
    }

    private static string StatusReason(int status) => status switch
    {
        NativeMethods.ErrorInvalidPackage => "not a valid Union style package",
        NativeMethods.ErrorNoStyle => "the style could not be loaded",
        NativeMethods.ErrorNotInitialized => "the engine is not started",
        _ => $"status {status}",
    };

    private List<UnionStyleMetadata> ListStyles()
    {
        var result = new List<UnionStyleMetadata>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in _appOrder)
        {
            var directory = _appPackages[id].Directory;
            if (directory is null)
            {
                continue;
            }

            string? json = null;
            try
            {
                json = File.ReadAllText(Path.Combine(directory, "metadata.json"));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            Add(UnionStyleMetadata.Parse(id, json, UnionStyleOrigin.Application));
        }

        var count = NativeMethods.InstalledStyles(null, 0);
        if (count > 0)
        {
            var packages = new AvuPackageInfo[count];
            fixed (AvuPackageInfo* p = packages)
            {
                count = Math.Min(count, NativeMethods.InstalledStyles(p, count));
            }

            for (var i = 0; i < count; i++)
            {
                var id = Str(packages[i].IdString) ?? string.Empty;
                Add(UnionStyleMetadata.Parse(id, Str(packages[i].MetadataString), UnionStyleOrigin.Installed));
            }
        }

        return result;

        void Add(UnionStyleMetadata metadata)
        {
            if (metadata.Id.Length > 0 && !metadata.IsHidden && seen.Add(metadata.Id))
            {
                result.Add(metadata);
            }
        }
    }

    private void RefreshDefaultStyle()
    {
        var id = StyleId(_defaultHandle);
        if (_appPackages.ContainsKey(id) && GetStyle(id) is { Metadata.Origin: UnionStyleOrigin.Application } app)
        {
            DefaultStyle.Id = id;
            DefaultStyle.Payload = app.Payload;
            DefaultStyle.Metadata = app.Metadata;
            return;
        }

        DefaultStyle.Id = id;
        DefaultStyle.Payload = _defaultHandle;
        DefaultStyle.Metadata = InstalledMetadata(_defaultHandle);
    }

    private UnionStyleMetadata InstalledMetadata(IntPtr style)
    {
        AvuStyleInfo info;
        return NativeMethods.StyleInfo(style, &info) == NativeMethods.Ok
            ? UnionStyleMetadata.Parse(Str(info.IdString) ?? "breeze", Str(info.MetadataString), UnionStyleOrigin.Installed)
            : UnionStyleMetadata.Parse("breeze", null, UnionStyleOrigin.Installed);
    }

    /// <summary>Gets the rule count and the error state of a style.</summary>
    public (int RuleCount, bool HasErrors, string? Path) GetStyleInfo(UnionStyleHandle style)
    {
        AvuStyleInfo info;
        if (NativeMethods.StyleInfo((IntPtr)style.Payload, &info) != NativeMethods.Ok)
        {
            return (0, false, null);
        }

        return (info.RuleCount, info.HasErrors != 0, Str(info.PathString));
    }

    /// <inheritdoc/>
    public ElementHandle CreateElement() => new NativeElement();

    /// <inheritdoc/>
    public ResolvedStyle Query(UnionStyleHandle style, ReadOnlySpan<ElementHandle> chain)
    {
        if (chain.Length == 0)
        {
            return ResolvedStyle.Unmatched;
        }

        var handles = chain.Length <= 32 ? stackalloc IntPtr[chain.Length] : new IntPtr[chain.Length];
        for (var i = 0; i < chain.Length; i++)
        {
            handles[i] = ((NativeElement)chain[i]).Handle;
        }

        AvuResolved resolved;
        int status;
        fixed (IntPtr* p = handles)
        {
            status = NativeMethods.Query((IntPtr)style.Payload, p, chain.Length, &resolved);
        }

        if (status <= 0)
        {
            return ResolvedStyle.Unmatched;
        }

        return ResolvedStyle.FromNative(resolved, _stringLookup) with { IsMatch = true };
    }

    /// <inheritdoc/>
    public Color SchemeColor(ColorGroup group, ColorSet set, ColorRoleKind kind, int role)
    {
        uint argb;
        return NativeMethods.SchemeColor((int)group, (int)set, (int)kind, role, &argb) == NativeMethods.Ok
            ? Color.FromUInt32(argb)
            : Color.FromArgb(0, 0, 0, 0);
    }

    /// <inheritdoc/>
    public IconRaster? RenderIcon(string name, Color? tint, PixelSize size, double scale)
    {
        var argb = tint is { } c ? (c.ToUInt32() == 0 ? 0x01000000u : c.ToUInt32()) : 0u;
        return Render((buffer, capacity, w, h) =>
        {
            fixed (char* p = name)
            {
                return NativeMethods.IconRender(p, name.Length, argb, size.Width, size.Height, (float)scale, buffer, capacity, w, h);
            }
        }, size, scale);
    }

    /// <inheritdoc/>
    public IconRaster? RenderImage(string source, Color? maskColor, bool invertedMask, PixelSize size, double scale)
    {
        if (!_stringIds.TryGetValue(source, out var id))
        {
            return null;
        }

        var mode = maskColor is null ? 0 : invertedMask ? 2 : 1;
        var argb = maskColor?.ToUInt32() ?? 0u;
        return Render((buffer, capacity, w, h) => NativeMethods.ImageRender(id, argb, mode, size.Width, size.Height, (float)scale, buffer, capacity, w, h), size, scale);
    }

    /// <inheritdoc/>
    public UnionImageGeometry? GetImageGeometry(string source) => null;

    private delegate int RenderCall(byte* buffer, int capacity, int* width, int* height);

    private static IconRaster? Render(RenderCall call, PixelSize size, double scale)
    {
        var w = (int)Math.Ceiling(size.Width * scale) + 2;
        var h = (int)Math.Ceiling(size.Height * scale) + 2;
        var pixels = new byte[w * h * 4];
        int outW, outH;
        int status;
        fixed (byte* p = pixels)
        {
            status = call(p, pixels.Length, &outW, &outH);
        }

        if (status == NativeMethods.ErrorBuffer && outW > 0 && outH > 0)
        {
            pixels = new byte[outW * outH * 4];
            fixed (byte* p = pixels)
            {
                status = call(p, pixels.Length, &outW, &outH);
            }
        }

        if (status != 1 || outW <= 0 || outH <= 0)
        {
            return null;
        }

        if (pixels.Length != outW * outH * 4)
        {
            Array.Resize(ref pixels, outW * outH * 4);
        }

        return new IconRaster(outW, outH, pixels);
    }

    /// <inheritdoc/>
    public UnionFonts Fonts => _fonts ??= new UnionFonts(
        ReadFont(0), ReadFont(1), ReadFont(2), ReadFont(3), ReadFont(4), ReadFont(5));

    private UnionFontInfo ReadFont(int which)
    {
        AvuFontInfo info;
        if (NativeMethods.Font(which, &info) != NativeMethods.Ok)
        {
            return new UnionFontInfo("$Default", UnionFontInfo.PointsToDips(10.0), 400, false);
        }

        var size = info.PixelSize > 0 ? info.PixelSize : UnionFontInfo.PointsToDips(info.PointSize);
        return new UnionFontInfo(Str(info.FamilyId) ?? "$Default", size, info.Weight, info.Italic != 0);
    }

    /// <inheritdoc/>
    public UnionPlatform Platform
    {
        get
        {
            if (_platform is null)
            {
                AvuPlatformInfo info;
                _platform = NativeMethods.Platform(&info) == NativeMethods.Ok
                    ? new UnionPlatform(info.SmoothScroll != 0, info.AnimationSpeed, info.RightToLeft != 0, Str(info.IconThemeId))
                    : new UnionPlatform(true, 1.0, false, null);
            }

            return _platform;
        }
    }

    /// <inheritdoc/>
    public void SetColorScheme(string? pathOrName)
    {
        var value = pathOrName ?? string.Empty;
        int status;
        fixed (char* p = value)
        {
            status = NativeMethods.SetColorSchemePath(p, value.Length);
        }

        if (status != NativeMethods.Ok)
        {
            Logger.TryGet(LogEventLevel.Warning, "AvaUnion")?.Log(this, "Color scheme {Scheme} not found", pathOrName);
            return;
        }

        _colorScheme = pathOrName;
    }

    private string? Str(int id)
    {
        if (id <= 0)
        {
            return id == 0 ? string.Empty : null;
        }

        if (_strings.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var length = NativeMethods.String(id, null, 0);
        var buffer = length <= 256 ? stackalloc char[length] : new char[length];
        fixed (char* p = buffer)
        {
            NativeMethods.String(id, p, length);
        }

        var value = new string(buffer);
        _strings[id] = value;
        _stringIds[value] = id;
        return value;
    }

    private string StyleId(IntPtr style)
    {
        AvuStyleInfo info;
        return NativeMethods.StyleInfo(style, &info) == NativeMethods.Ok ? Str(info.IdString) ?? "breeze" : "breeze";
    }

    private static string Ascii(byte* value)
    {
        var length = 0;
        while (length < 32 && value[length] != 0)
        {
            length++;
        }

        return Encoding.ASCII.GetString(value, length);
    }

    [UnmanagedCallersOnly]
    private static void OnCallback(uint flags, int argument, void* user)
    {
        try
        {
            s_instance?.Dispatch(flags, argument);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, "AvaUnion")?.Log(null, "Change handler failed: {Exception}", e);
        }
    }

    private void Dispatch(uint flags, int argument)
    {
        if ((flags & NativeMethods.ChangeLog) != 0)
        {
            var message = Str(argument) ?? string.Empty;
            var level = message.StartsWith("union.css", StringComparison.Ordinal) ? LogEventLevel.Warning : LogEventLevel.Debug;
            Logger.TryGet(level, "AvaUnion")?.Log(this, "{Message}", message);
            Log?.Invoke(message);
            return;
        }

        var change = UnionChange.None;
        if ((flags & NativeMethods.ChangeColors) != 0)
        {
            change |= UnionChange.Colors;
        }

        if ((flags & NativeMethods.ChangeStyle) != 0)
        {
            change |= UnionChange.Style;
            _stylesList = null;
            RefreshDefaultStyle();
        }

        if ((flags & NativeMethods.ChangeFonts) != 0)
        {
            change |= UnionChange.Fonts;
            _fonts = null;
        }

        if ((flags & NativeMethods.ChangeIcons) != 0)
        {
            change |= UnionChange.Icons;
            _platform = null;
        }

        if ((flags & NativeMethods.ChangeAnimation) != 0)
        {
            change |= UnionChange.Animation;
            _platform = null;
        }

        if ((change & (UnionChange.Colors | UnionChange.Style)) != 0)
        {
            _epoch++;
        }

        _pump?.Wake();
        if (change != UnionChange.None)
        {
            Changed?.Invoke(change);
        }
    }

    /// <summary>Releases the engine.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pump?.Dispose();
        NativeMethods.SetCallback(null, null);
        NativeMethods.Shutdown();
        s_instance = null;
    }
}
