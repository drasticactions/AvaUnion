using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Logging;
using AvaUnion.Engine.Managed;
using AvaUnion.Engine.Native;

namespace AvaUnion.Engine;

/// <summary>
/// Selects the engine one time for each process. The <c>AVAUNION_ENGINE</c>
/// variable has priority over the requested kind. <see cref="UnionEngineKind.Auto"/>
/// selects the native engine when the shim matches the installed Union and
/// starts without errors.
/// </summary>
public static class EngineSelector
{
    /// <summary>The environment variable that forces an engine. The values are <c>native</c> and <c>managed</c>.</summary>
    public const string EnvironmentVariable = "AVAUNION_ENGINE";

    private static IUnionEngine? s_selected;

    /// <summary>Options from <see cref="AppBuilderExtensions.WithAvaUnion"/>.</summary>
    public static AvaUnionOptions? Options { get; internal set; }

    /// <summary>The reason that the native engine is not in use, or null.</summary>
    public static string? NativeUnavailableReason { get; private set; }

    /// <summary>Selects the engine, or returns the engine that is already selected.</summary>
    /// <exception cref="NativeInitException">The native engine was requested explicitly and cannot start.</exception>
    public static IUnionEngine Select(UnionEngineKind requested, AvaUnionOptions? options)
    {
        if (s_selected is not null)
        {
            return s_selected;
        }

        options ??= Options ?? new AvaUnionOptions();
        var kind = requested == UnionEngineKind.Auto ? options.Engine : requested;
        var forced = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.Equals(forced, "native", StringComparison.OrdinalIgnoreCase))
        {
            kind = UnionEngineKind.Native;
        }
        else if (string.Equals(forced, "managed", StringComparison.OrdinalIgnoreCase))
        {
            kind = UnionEngineKind.Managed;
        }

        if (kind != UnionEngineKind.Managed && OperatingSystem.IsLinux() && !OperatingSystem.IsBrowser())
        {
            try
            {
                var native = NativeEngine.Create(options.HostRunsGLibMainLoop);
                native.AddStylePackages(StylePackages(options));
                return s_selected = native;
            }
            catch (Exception e) when (kind == UnionEngineKind.Auto)
            {
                NativeUnavailableReason = e.Message;
                Logger.TryGet(LogEventLevel.Information, "AvaUnion")?.Log(null, "AvaUnion: native engine unavailable: {Reason}", e.Message);
            }
        }
        else if (kind == UnionEngineKind.Native)
        {
            throw new NativeInitException("the native engine needs Linux");
        }
        else if (kind == UnionEngineKind.Auto)
        {
            NativeUnavailableReason = OperatingSystem.IsBrowser() ? "browser" : "not Linux";
        }

        var managed = new ManagedEngine();
        managed.AddStylePackages(StylePackages(options));
        return s_selected = managed;
    }

    internal static List<UnionStylePackage> StylePackages(AvaUnionOptions options)
    {
        var result = new List<UnionStylePackage>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var package in options.StylePackages)
        {
            if (seen.Add(package.Id))
            {
                result.Add(package);
            }
        }

        if (options.DiscoverStylePackages && Assembly.GetEntryAssembly() is { } entry)
        {
            foreach (var package in UnionStylePackage.Discover(entry))
            {
                if (seen.Add(package.Id))
                {
                    result.Add(package);
                }
            }
        }

        return result;
    }

    /// <summary>The engine selected so far, or null.</summary>
    public static IUnionEngine? Selected => s_selected;

    internal static void Reset(IUnionEngine? engine) => s_selected = engine;
}
