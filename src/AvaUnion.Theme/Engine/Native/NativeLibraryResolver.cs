using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AvaUnion.Engine.Native;

internal static class NativeLibraryResolver
{
    private static readonly object s_lock = new();
    private static IntPtr s_handle;
    private static bool s_resolverInstalled;

    public const string PathVariable = "AVAUNION_NATIVE_PATH";

    private static readonly string[] s_libraryDirectories =
    [
        "/usr/lib", "/usr/lib64", "/usr/lib/x86_64-linux-gnu", "/usr/lib/aarch64-linux-gnu", "/usr/local/lib", "/usr/local/lib64",
    ];

    public static bool TryLoad(out string reason)
    {
        lock (s_lock)
        {
            if (s_handle != IntPtr.Zero)
            {
                reason = string.Empty;
                return true;
            }

            if (!OperatingSystem.IsLinux())
            {
                reason = "not Linux";
                return false;
            }

            string? path = Environment.GetEnvironmentVariable(PathVariable);
            if (string.IsNullOrEmpty(path))
            {
                var unionVersion = SystemUnionVersion();
                if (unionVersion is null)
                {
                    reason = "libUnion.so.6 not found";
                    return false;
                }

                var candidates = Candidates().ToList();
                if (candidates.Count == 0)
                {
                    reason = "no libavaunion-native shim next to the assembly";
                    return false;
                }

                path = candidates.FirstOrDefault(c => c.Version == unionVersion).Path
                    ?? candidates.FirstOrDefault(c => Compatible(c.Version, unionVersion)).Path;
                if (path is null)
                {
                    reason = $"no shim has the property schema of Union {unionVersion}";
                    return false;
                }
            }

            if (!NativeLibrary.TryLoad(path, out var handle))
            {
                reason = $"could not load {path}";
                return false;
            }

            s_handle = handle;
            if (!s_resolverInstalled)
            {
                NativeLibrary.SetDllImportResolver(typeof(NativeLibraryResolver).Assembly, Resolve);
                s_resolverInstalled = true;
            }

            reason = string.Empty;
            return true;
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
        libraryName == NativeMethods.Library ? s_handle : IntPtr.Zero;

    public static string? SystemUnionVersion()
    {
        foreach (var dir in s_libraryDirectories)
        {
            var link = Path.Combine(dir, "libUnion.so.6");
            if (!File.Exists(link))
            {
                continue;
            }

            var target = File.ResolveLinkTarget(link, returnFinalTarget: true)?.FullName ?? link;
            var name = Path.GetFileName(target);
            var marker = name.IndexOf(".so.", StringComparison.Ordinal);
            if (marker >= 0)
            {
                return name[(marker + 4)..];
            }
        }

        return null;
    }

    public static IEnumerable<(string Path, string Version)> Candidates()
    {
        var baseDir = AppContext.BaseDirectory;
        var rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
        var dirs = new[] { baseDir, Path.Combine(baseDir, "runtimes", rid, "native") };
        var found = new List<(string, string)>();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(dir, "libavaunion-native-*.so"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                found.Add((file, name["libavaunion-native-".Length..]));
            }
        }

        return found.OrderByDescending(f => ParseVersion(f.Item2));
    }

    public static bool Compatible(string shim, string system)
    {
        if (ParseVersion(shim).Major < 0 || ParseVersion(system).Major < 0)
        {
            return false;
        }

        if (shim == system)
        {
            return true;
        }

        return SchemaOf(shim) is { } schema && schema == SchemaOf(system);
    }

    public static string? SchemaOf(string version)
    {
        foreach (var (release, schema) in UnionSchemas.Releases)
        {
            if (release == version)
            {
                return schema;
            }
        }

        var v = ParseVersion(version);
        if (v.Major < 0 || v.Build < 0 || v.Build >= 80)
        {
            return null;
        }

        string? found = null;
        var foundBuild = -1;
        foreach (var (release, schema) in UnionSchemas.Releases)
        {
            var r = ParseVersion(release);
            if (r.Major == v.Major && r.Minor == v.Minor && r.Build < 80 && r.Build > foundBuild)
            {
                found = schema;
                foundBuild = r.Build;
            }
        }

        return found;
    }

    private static (int Major, int Minor, int Build) ParseVersion(string value)
    {
        var parts = value.Split('.');
        int Part(int i) => i < parts.Length && int.TryParse(parts[i], out var v) ? v : -1;
        return (Part(0), Part(1), Part(2));
    }
}
