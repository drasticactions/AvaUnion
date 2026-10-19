using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Metadata;
using AvaUnion.Engine;
using AvaUnion.Markup;
using Xunit;

namespace AvaUnion.Theme.Tests;

public partial class RepositoryTests
{
    private static string Root => TestData.RepoRoot;

    private static IEnumerable<string> SourceFiles(params string[] patterns) =>
        new[] { "src", "tests", "samples", "tools" }
            .Select(d => Path.Combine(Root, d))
            .Where(Directory.Exists)
            .SelectMany(d => patterns.SelectMany(p => Directory.EnumerateFiles(d, p, SearchOption.AllDirectories)))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [GeneratedRegex(@"^(?:public |internal |file )?(?:sealed |static |abstract |partial |readonly |unsafe |ref )*(?:class|record struct|record|struct|enum|interface) (\w+)", RegexOptions.Multiline)]
    private static partial Regex TopLevelType();

    [Fact]
    public void One_Top_Level_Type_Per_File()
    {
        var offenders = new List<string>();
        foreach (var file in SourceFiles("*.cs").Where(f => Path.GetRelativePath(Root, f).StartsWith("src", StringComparison.Ordinal)))
        {
            var types = TopLevelType().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value).Distinct().ToList();
            var name = Path.GetFileName(file).Split('.')[0];
            if (types.Count > 1 || (types.Count == 1 && types[0] != name))
            {
                offenders.Add($"{Path.GetRelativePath(Root, file)}: {string.Join(", ", types)}");
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Theory]
    [InlineData(typeof(UnionTheme))]
    [InlineData(typeof(Controls.UnionControlsTheme))]
    [InlineData(typeof(DataGrid.UnionDataGridTheme))]
    [InlineData(typeof(BreezeIcons.BreezeIconSet))]
    public void Every_Public_Namespace_Is_Mapped(Type type)
    {
        var assembly = type.Assembly;
        var mapped = assembly.GetCustomAttributes<XmlnsDefinitionAttribute>()
            .Where(a => a.XmlNamespace == "https://github.com/avaunion")
            .Select(a => a.ClrNamespace)
            .ToHashSet();
        var namespaces = assembly.GetExportedTypes().Select(t => t.Namespace!).Where(n => n != "CompiledAvaloniaXaml").Distinct();
        var unmapped = namespaces.Where(n => !mapped.Contains(n)).ToList();
        Assert.True(unmapped.Count == 0, string.Join(", ", unmapped));
        Assert.Contains(assembly.GetCustomAttributes<XmlnsPrefixAttribute>(), a => a.Prefix == "union");
    }

    private static readonly string[] s_forbidden =
        [string.Concat("Ava", "Motif"), string.Concat("Ava", "Wpf"), string.Concat("Ava", "Win"), string.Concat("Ava", "Enyo"),
         string.Concat("Ava", "Platinum"), string.Concat("Ava", "Adwaita")];

    [Fact]
    public void Sibling_Projects_Are_Never_Named()
    {
        var offenders = new List<string>();
        var files = SourceFiles("*.cs", "*.axaml", "*.csproj", "*.props", "*.targets", "*.sh", "*.yml", "*.md", "*.json", "*.cpp", "*.h", "*.txt", "*.cmake")
            .Concat(Directory.EnumerateFiles(Root, "*.md"))
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, ".github"), "*", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "native"), "*", SearchOption.AllDirectories).Where(f => !f.Contains("/build") && !f.Contains("/artifacts")))
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "docs"), "*", SearchOption.AllDirectories));
        foreach (var file in files.Distinct())
        {
            var text = File.ReadAllText(file);
            foreach (var name in s_forbidden)
            {
                if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{Path.GetRelativePath(Root, file)}: {name}");
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }

    [Fact]
    public void Every_Control_Theme_File_Is_Merged()
    {
        var merged = File.ReadAllText(Path.Combine(Root, "src/AvaUnion.Theme/obj/ControlThemes/_Controls.axaml"));
        var missing = Directory.EnumerateFiles(Path.Combine(Root, "src/AvaUnion.Theme/Themes/Controls"), "*.axaml")
            .Select(Path.GetFileName)
            .Where(n => !merged.Contains("/Themes/Controls/" + n, StringComparison.Ordinal))
            .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Generated_Code_Matches_The_Pinned_Union_Tag()
    {
        var cmake = File.ReadAllText(Path.Combine(Root, "external/union/CMakeLists.txt"));
        var tag = "v" + Regex.Match(cmake, @"set\(PROJECT_VERSION ""([0-9.]+)""\)").Groups[1].Value;
        Assert.Contains($"tag={tag} ", UnionLeafInfo.Source + " ");
        var header = File.ReadAllText(Path.Combine(Root, "native/src/avu_resolved.g.h"));
        Assert.Contains(UnionLeafInfo.Source, header);
        Assert.Equal(UnionLeafInfo.Source, Engine.Managed.Snapshot.LoadEmbedded().Source);
    }

    [Fact]
    public void App_Style_Snapshots_Have_The_Current_Schema()
    {
        foreach (var file in new[] { "tests/Data/styles/avaunion-test.union.bin", "samples/AvaUnion.Gallery/Styles/avaunion-demo.union.bin" })
        {
            var snapshot = Engine.Managed.Snapshot.Load(File.ReadAllBytes(Path.Combine(Root, file)));
            Assert.Equal(UnionLeafInfo.Schema, snapshot.Schema);
        }

        var demo = UnionStyleMetadata.Parse("avaunion-demo", File.ReadAllText(Path.Combine(Root, "samples/AvaUnion.Gallery/Styles/avaunion-demo/metadata.json")), UnionStyleOrigin.Application);
        Assert.Equal(TestData.UnionVersion, demo.Version);
    }

    [Fact]
    public void Package_Version_Follows_The_Pinned_Union()
    {
        var props = File.ReadAllText(Path.Combine(Root, "Directory.Build.props"));
        var cmake = File.ReadAllText(Path.Combine(Root, "external/union/CMakeLists.txt"));
        var union = Regex.Match(cmake, @"set\(PROJECT_VERSION ""([0-9.]+)""\)").Groups[1].Value;
        Assert.Equal(union, TestData.UnionVersion);
        Assert.Equal(union, Engine.Managed.Snapshot.LoadEmbedded().UnionVersion);
        Assert.Matches(@"<Version>\$\(AvaUnionUnionVersion\)\.\$\(AvaUnionRevision\)</Version>", props);
    }

    [GeneratedRegex(@"\{union:Style ([A-Za-z.]+)")]
    private static partial Regex StyleBinding();

    [Fact]
    public void Every_Style_Binding_Path_Exists()
    {
        var bad = SourceFiles("*.axaml")
            .SelectMany(f => StyleBinding().Matches(File.ReadAllText(f)).Select(m => (File: f, Path: m.Groups[1].Value)))
            .Where(x => StylePaths.Get(x.Path) is null)
            .Select(x => $"{Path.GetRelativePath(Root, x.File)}: {x.Path}")
            .ToList();
        Assert.Empty(bad);
    }

    [Fact]
    public void Comments_Do_Not_Mention_The_Spec()
    {
        var offenders = SourceFiles("*.cs", "*.axaml")
            .Where(f => !f.EndsWith("RepositoryTests.cs", StringComparison.Ordinal))
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, l, i)))
            .Where(x => Regex.IsMatch(x.l, @"(//|<!--|\*).*\b(spec|interview|milestone)\b", RegexOptions.IgnoreCase))
            .Select(x => $"{Path.GetRelativePath(Root, x.f)}:{x.i + 1}")
            .ToList();
        Assert.Empty(offenders);
    }
}
