using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AvaUnion;

/// <summary>
/// A style package that an application ships for its own use. The native
/// engine loads it from <see cref="Directory"/>. The managed engine loads it
/// from the snapshot that <see cref="OpenSnapshot"/> opens.
/// </summary>
public sealed class UnionStylePackage
{
    private const string ResourcePrefix = "AvaUnion.Styles.";
    private const string ResourceSuffix = ".union.bin";

    /// <summary>The folder in the output folder that holds the package folders.</summary>
    public const string OutputFolder = "union-styles";

    private UnionStylePackage(string id, string? directory, Func<Stream>? openSnapshot)
    {
        Id = id;
        Directory = directory;
        OpenSnapshot = openSnapshot;
    }

    /// <summary>The style id. This is the name of the package folder.</summary>
    public string Id { get; }

    /// <summary>The package folder, or null. The folder holds <c>metadata.json</c> and <c>contents</c>.</summary>
    public string? Directory { get; }

    /// <summary>Opens the snapshot of the package, or null when there is no snapshot.</summary>
    public Func<Stream>? OpenSnapshot { get; }

    /// <summary>Creates a package from a package folder and, optionally, its snapshot.</summary>
    public static UnionStylePackage FromDirectory(string directory, Func<Stream>? openSnapshot = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        return new UnionStylePackage(Path.GetFileName(full), full, openSnapshot);
    }

    /// <summary>Creates a package that has only a snapshot. The native engine cannot use it.</summary>
    public static UnionStylePackage FromSnapshot(string id, Func<Stream> openSnapshot)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(openSnapshot);
        return new UnionStylePackage(id, null, openSnapshot);
    }

    /// <summary>
    /// Finds the packages that the <c>UnionStylePackage</c> MSBuild item adds
    /// to an assembly and to the output folder. Each embedded resource named
    /// <c>AvaUnion.Styles.&lt;id&gt;.union.bin</c> gives a snapshot. Each
    /// folder <c>union-styles/&lt;id&gt;</c> in
    /// <see cref="AppContext.BaseDirectory"/> gives a package folder.
    /// </summary>
    public static IReadOnlyList<UnionStylePackage> Discover(Assembly assembly) =>
        Discover(assembly, OperatingSystem.IsBrowser() ? null : AppContext.BaseDirectory);

    internal static IReadOnlyList<UnionStylePackage> Discover(Assembly? assembly, string? baseDirectory)
    {
        var ids = new SortedSet<string>(StringComparer.Ordinal);
        var resources = new Dictionary<string, string>(StringComparer.Ordinal);
        if (assembly is not null)
        {
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.Length > ResourcePrefix.Length + ResourceSuffix.Length
                    && name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                    && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                {
                    var id = name[ResourcePrefix.Length..^ResourceSuffix.Length];
                    resources[id] = name;
                    ids.Add(id);
                }
            }
        }

        var folders = new Dictionary<string, string>(StringComparer.Ordinal);
        var root = baseDirectory is null ? null : Path.Combine(baseDirectory, OutputFolder);
        if (root is not null && System.IO.Directory.Exists(root))
        {
            foreach (var folder in System.IO.Directory.EnumerateDirectories(root))
            {
                if (File.Exists(Path.Combine(folder, "metadata.json")))
                {
                    var id = Path.GetFileName(folder);
                    folders[id] = folder;
                    ids.Add(id);
                }
            }
        }

        var result = new List<UnionStylePackage>(ids.Count);
        foreach (var id in ids)
        {
            Func<Stream>? open = resources.TryGetValue(id, out var resource)
                ? () => assembly!.GetManifestResourceStream(resource) ?? throw new FileNotFoundException($"resource {resource} not found")
                : null;
            result.Add(new UnionStylePackage(id, folders.GetValueOrDefault(id), open));
        }

        return result;
    }

    /// <inheritdoc/>
    public override string ToString() => Id;
}
