using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AvaUnion.Theme.Tests;

internal static class TestData
{
    public static string Path(string relative) => System.IO.Path.Combine(AppContext.BaseDirectory, "Data", relative);

    public static string UnionVersion { get; } = typeof(TestData).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == "AvaUnionUnionVersion").Value!;

    public static string RepoRoot
    {
        get
        {
            var dir = AppContext.BaseDirectory;
            while (dir is not null && !File.Exists(System.IO.Path.Combine(dir, "AvaUnion.slnx")))
            {
                dir = System.IO.Path.GetDirectoryName(dir);
            }

            return dir ?? throw new InvalidOperationException("repository root not found");
        }
    }
}
