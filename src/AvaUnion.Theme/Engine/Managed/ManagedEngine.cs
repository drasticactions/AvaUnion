using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>
/// The managed engine. It resolves styles from the Breeze snapshot and from
/// the snapshots of the application's style packages.
/// </summary>
public sealed class ManagedEngine : IUnionEngine
{
    /// <summary>The scheme for a light theme variant.</summary>
    public const string LightScheme = "BreezeLight";

    /// <summary>The scheme for a dark theme variant.</summary>
    public const string DarkScheme = "BreezeDark";

    private readonly Snapshot _snapshot;
    private readonly LruCache<string, ResolvedStyle> _cache = new(500);
    private readonly Dictionary<string, UnionStyleHandle> _handles = new(StringComparer.Ordinal);
    private readonly List<string> _appStyles = [];
    private readonly Dictionary<string, UnionImageGeometry> _appImages = new(StringComparer.Ordinal);
    private readonly AppStyleLog _appStyleLog = new();
    private IReadOnlyList<UnionStyleMetadata>? _styles;
    private readonly HashSet<string> _warnedSources = new(StringComparer.Ordinal);
    private readonly Func<string, IReadOnlyList<string>, UnionRgba?> _provider;
    private readonly Func<UnionColor, Color> _resolveColor;
    private readonly StringBuilder _keyBuilder = new(256);
    private SchemeTable _scheme;
    private string? _explicitScheme;
    private bool _dark;
    private int _epoch;

    /// <summary>Creates an engine for a snapshot.</summary>
    public ManagedEngine(Snapshot snapshot)
    {
        _snapshot = snapshot;
        _scheme = snapshot.Schemes[LightScheme];
        _provider = ResolveProviderColor;
        _resolveColor = c => c.Resolve(_provider);
        foreach (var id in snapshot.Styles.Keys)
        {
            _handles[id] = new UnionStyleHandle(id, false, snapshot.Styles[id], Metadata(snapshot, id, UnionStyleOrigin.Snapshot));
        }

        DefaultStyle = new UnionStyleHandle("breeze", true, snapshot.Styles["breeze"], _handles["breeze"].Metadata);
    }

    /// <summary>Creates an engine for the embedded snapshot.</summary>
    public ManagedEngine()
        : this(Snapshot.LoadEmbedded())
    {
    }

    /// <inheritdoc/>
    public UnionEngineKind Kind => UnionEngineKind.Managed;

    /// <inheritdoc/>
    public string Description => $"Managed · Union {_snapshot.UnionVersion} · {_scheme.Name}";

    /// <summary>The snapshot.</summary>
    public Snapshot Snapshot => _snapshot;

    /// <summary>The current scheme.</summary>
    public SchemeTable Scheme => _scheme;

    /// <inheritdoc/>
    public UnionStyleHandle DefaultStyle { get; }

    /// <inheritdoc/>
    public string? ColorScheme => _explicitScheme;

    /// <inheritdoc/>
    public int Epoch => _epoch;

    /// <summary>Tells if a font family is installed.</summary>
    public Func<string, bool> IsFontInstalled { get; set; } = _ => true;

    /// <inheritdoc/>
    public event Action<UnionChange>? Changed;

    /// <inheritdoc/>
    public UnionFonts Fonts
    {
        get
        {
            var general = new UnionFontInfo("$Default", UnionFontInfo.PointsToDips(10.0), 400, false);
            foreach (var (family, points) in _snapshot.Fonts)
            {
                if (family == "$Default" || IsFontInstalled(family))
                {
                    general = new UnionFontInfo(family, UnionFontInfo.PointsToDips(points), 400, false);
                    break;
                }
            }

            var small = general with { SizeInDips = UnionFontInfo.PointsToDips(8.0) };
            var fixedFont = general with { Family = "monospace" };
            return new UnionFonts(general, fixedFont, small, general, general, general);
        }
    }

    /// <inheritdoc/>
    public UnionPlatform Platform { get; private set; } = new(true, 1.0, false, null);

    internal void SetAnimationSpeed(double speed)
    {
        Platform = Platform with { AnimationSpeed = speed };
        Changed?.Invoke(UnionChange.Animation);
    }

    /// <inheritdoc/>
    public UnionStyleHandle? GetStyle(string id) => _handles.GetValueOrDefault(id);

    /// <inheritdoc/>
    public IReadOnlyList<UnionStyleMetadata> Styles => _styles ??= ListStyles();

    internal IReadOnlyList<string> AppStyleLogLines => _appStyleLog.Lines;

    internal void AddStylePackages(IEnumerable<UnionStylePackage> packages)
    {
        foreach (var package in packages)
        {
            if (package.OpenSnapshot is null)
            {
                _appStyleLog.Unavailable(this, package.Id, "no snapshot for the managed engine");
                continue;
            }

            Snapshot snapshot;
            try
            {
                using var stream = package.OpenSnapshot();
                snapshot = Snapshot.Load(stream);
            }
            catch (Exception e)
            {
                _appStyleLog.Unavailable(this, package.Id, e.Message);
                continue;
            }

            var single = snapshot.Styles.Count == 1;
            foreach (var (snapshotId, snapshotStyle) in snapshot.Styles)
            {
                var id = single ? package.Id : snapshotId;
                if (_appStyles.Contains(id))
                {
                    continue;
                }

                var style = snapshotStyle with { Id = id };
                var metadata = Metadata(snapshot, snapshotId, UnionStyleOrigin.Application) with { Id = id };
                var handle = new UnionStyleHandle(id, false, style, metadata);
                _handles[id] = handle;
                _appStyles.Add(id);
                if (id == DefaultStyle.Id)
                {
                    DefaultStyle.Payload = style;
                    DefaultStyle.Metadata = handle.Metadata;
                }
            }

            foreach (var (path, geometry) in snapshot.Images)
            {
                _appImages.TryAdd(path, geometry);
            }
        }

        _styles = null;
        _cache.Clear();
    }

    private List<UnionStyleMetadata> ListStyles()
    {
        var result = new List<UnionStyleMetadata>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in _appStyles)
        {
            Add(_handles[id].Metadata);
        }

        foreach (var id in new[] { "breeze", "breeze-rtl", "breeze-mobile" })
        {
            if (_handles.TryGetValue(id, out var handle))
            {
                Add(handle.Metadata);
            }
        }

        return result;

        void Add(UnionStyleMetadata metadata)
        {
            if (!metadata.IsHidden && seen.Add(metadata.Id))
            {
                result.Add(metadata);
            }
        }
    }

    private static UnionStyleMetadata Metadata(Snapshot snapshot, string id, UnionStyleOrigin origin) =>
        UnionStyleMetadata.Parse(id, snapshot.StyleMetadata.GetValueOrDefault(id), origin);

    /// <inheritdoc/>
    public ElementHandle CreateElement() => new ManagedElement();

    /// <inheritdoc/>
    public ResolvedStyle Query(UnionStyleHandle style, ReadOnlySpan<ElementHandle> chain)
    {
        if (chain.Length == 0)
        {
            return ResolvedStyle.Unmatched;
        }

        var managedStyle = (ManagedStyle)style.Payload;
        var elements = new ManagedElement[chain.Length];
        var sb = _keyBuilder.Clear().Append(managedStyle.Id);
        for (var i = 0; i < chain.Length; i++)
        {
            elements[i] = (ManagedElement)chain[i];
            sb.Append('\u001d').Append(elements[i].CacheKey);
        }

        var key = sb.ToString();
        if (_cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var result = Cascade.Resolve(managedStyle, elements, _resolveColor);
        _cache.Set(key, result);
        return result;
    }

    /// <inheritdoc/>
    public Color SchemeColor(ColorGroup group, ColorSet set, ColorRoleKind kind, int role) => _scheme.Get(group, set, kind, role);

    /// <inheritdoc/>
    public IconRaster? RenderIcon(string name, Color? tint, PixelSize size, double scale) => null;

    /// <inheritdoc/>
    public IconRaster? RenderImage(string source, Color? maskColor, bool invertedMask, PixelSize size, double scale) => null;

    /// <inheritdoc/>
    public UnionImageGeometry? GetImageGeometry(string source) => FindImage(_appImages, source) ?? FindImage(_snapshot.Images, source);

    private static UnionImageGeometry? FindImage(IReadOnlyDictionary<string, UnionImageGeometry> images, string source)
    {
        if (images.TryGetValue(source, out var geometry))
        {
            return geometry;
        }

        foreach (var (path, value) in images)
        {
            if (source.EndsWith("/" + path, StringComparison.Ordinal))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets the scheme by name. The known names are <c>BreezeLight</c> and
    /// <c>BreezeDark</c>. For other names, the engine logs a message once and
    /// follows the theme variant.
    /// </summary>
    public void SetColorScheme(string? pathOrName)
    {
        string? name = null;
        if (!string.IsNullOrEmpty(pathOrName))
        {
            var bare = System.IO.Path.GetFileNameWithoutExtension(pathOrName);
            if (_snapshot.Schemes.ContainsKey(bare))
            {
                name = bare;
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, "AvaUnion")?.Log(this, "Color scheme {Scheme} needs the native engine; following the theme variant", pathOrName);
            }
        }

        _explicitScheme = name;
        ApplyScheme();
    }

    /// <summary>
    /// Sets the theme variant. When no scheme is set, the variant selects
    /// Breeze Light or Breeze Dark.
    /// </summary>
    public void SetVariant(bool dark)
    {
        _dark = dark;
        ApplyScheme();
    }

    private void ApplyScheme()
    {
        var name = _explicitScheme ?? (_dark ? DarkScheme : LightScheme);
        var table = _snapshot.Schemes[name];
        if (ReferenceEquals(table, _scheme))
        {
            return;
        }

        _scheme = table;
        _cache.Clear();
        _epoch++;
        Changed?.Invoke(UnionChange.Colors);
    }

    private UnionRgba? ResolveProviderColor(string source, IReadOnlyList<string> arguments)
    {
        if (string.Equals(source, "kcolorscheme", StringComparison.Ordinal))
        {
            return _scheme.Resolve(arguments);
        }

        if (_warnedSources.Add(source))
        {
            Logger.TryGet(LogEventLevel.Warning, "AvaUnion")?.Log(this, "No color provider {Source}", source);
        }

        return null;
    }
}
