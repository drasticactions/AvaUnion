using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AvaUnion.Engine;
using AvaUnion.Engine.Managed;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class AppStyleTests
{
    private const string Id = "avaunion-test";

    private static string PackageDir => TestData.Path("styles/avaunion-test");

    private static string SnapshotFile => TestData.Path("styles/avaunion-test.union.bin");

    private static UnionStylePackage SnapshotPackage(string id = Id) => UnionStylePackage.FromSnapshot(id, () => File.OpenRead(SnapshotFile));

    private static ManagedEngine Managed(params UnionStylePackage[] packages)
    {
        var engine = new ManagedEngine();
        engine.AddStylePackages(packages);
        return engine;
    }

    private static string CopyPackage(string id, Action<string>? edit = null)
    {
        var root = Directory.CreateTempSubdirectory("avaunion-app-style-").FullName;
        var target = Path.Combine(root, id);
        foreach (var file in Directory.EnumerateFiles(PackageDir, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(PackageDir, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }

        edit?.Invoke(target);
        return target;
    }

    [Fact]
    public void Managed_Loads_App_Snapshot()
    {
        var engine = Managed(SnapshotPackage());
        var handle = engine.GetStyle(Id);
        Assert.NotNull(handle);
        using var export = JsonDocument.Parse(File.ReadAllText(TestData.Path("styles/avaunion-test.json")));
        Assert.Equal(export.RootElement.GetProperty("rules").GetArrayLength(), ((ManagedStyle)handle.Payload).Rules.Count);
        Assert.Equal(UnionStyleOrigin.Application, handle.Metadata.Origin);
        Assert.Equal("1.0", handle.Metadata.Version);
        Assert.Equal("LGPL-2.1-only", handle.Metadata.License);
        Assert.NotNull(engine.GetImageGeometry("avaunion-test/contents/images/mark.svg"));
        Assert.Empty(engine.AppStyleLogLines);
    }

    public static IEnumerable<object[]> AppRuns() =>
        ParityCorpus.Load(ParityCorpus.AppExpected).Runs.Select(r => new object[] { r.Scheme });

    [Theory]
    [MemberData(nameof(AppRuns))]
    public void Managed_Parity_With_Union(string scheme)
    {
        var corpus = ParityCorpus.Load(ParityCorpus.AppExpected);
        var run = corpus.Runs.Single(r => r.Style == Id && r.Scheme == scheme);
        var engine = Managed(SnapshotPackage());
        engine.SetColorScheme(scheme);
        var handle = engine.GetStyle(Id)!;
        var failures = new List<string>();
        for (var i = 0; i < corpus.Chains.Count; i++)
        {
            var (id, chain) = corpus.Chains[i];
            var resolved = engine.Query(handle, ParityCorpus.BuildChain(engine, chain));
            var (matched, set) = run.Results[i];
            if (resolved.IsMatch != matched)
            {
                failures.Add($"{id}: matched {resolved.IsMatch}, Union {matched}");
                continue;
            }

            var diffs = ParityCorpus.Compare(resolved, corpus.Sets[set]);
            if (diffs.Count > 0)
            {
                failures.Add($"{id} ({ParityCorpus.Describe(chain)}):\n    " + string.Join("\n    ", diffs));
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} of {corpus.Chains.Count} chains differ:\n" + string.Join("\n", failures.Take(25)));
    }

    [Fact]
    public void Native_Loads_App_Package()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var (path, hasErrors, origin, name) = NativeHost.Run(e =>
        {
            e.AddStylePackages([UnionStylePackage.FromDirectory(PackageDir)]);
            try
            {
                var handle = e.GetStyle(Id)!;
                var info = e.GetStyleInfo(handle);
                return (info.Path, info.HasErrors, handle.Metadata.Origin, handle.Metadata.Id);
            }
            finally
            {
                e.RemoveStylePackages();
            }
        });

        Assert.Equal(Path.GetFullPath(PackageDir), path);
        Assert.False(hasErrors);
        Assert.Equal(UnionStyleOrigin.Application, origin);
        Assert.Equal(Id, name);
    }

    [Theory]
    [MemberData(nameof(AppRuns))]
    public void Native_Parity_With_Export(string scheme)
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var corpus = ParityCorpus.Load(ParityCorpus.AppExpected);
        var run = corpus.Runs.Single(r => r.Style == Id && r.Scheme == scheme);
        var failures = NativeHost.Run(engine =>
        {
            engine.AddStylePackages([UnionStylePackage.FromDirectory(PackageDir)]);
            engine.SetColorScheme($"/usr/share/color-schemes/{scheme}.colors");
            try
            {
                var handle = engine.GetStyle(Id)!;
                var failures = new List<string>();
                for (var i = 0; i < corpus.Chains.Count; i++)
                {
                    var (id, chain) = corpus.Chains[i];
                    var elements = ParityCorpus.BuildChain(engine, chain);
                    var resolved = engine.Query(handle, elements);
                    foreach (var e in elements)
                    {
                        e.Dispose();
                    }

                    var (matched, set) = run.Results[i];
                    if (resolved.IsMatch != matched)
                    {
                        failures.Add($"{id}: matched {resolved.IsMatch}, export {matched}");
                        continue;
                    }

                    var diffs = ParityCorpus.Compare(resolved, corpus.Sets[set], normalizePaths: true);
                    if (diffs.Count > 0)
                    {
                        failures.Add($"{id}: " + string.Join("; ", diffs));
                    }
                }

                return failures;
            }
            finally
            {
                engine.SetColorScheme(null);
                engine.RemoveStylePackages();
            }
        });

        Assert.True(failures.Count == 0, $"{failures.Count} chains differ:\n" + string.Join("\n", failures.Take(25)));
    }

    [Fact]
    public void App_Package_Shadows_Installed_Id_Managed()
    {
        var engine = Managed(SnapshotPackage("breeze"));
        var handle = engine.GetStyle("breeze")!;
        Assert.Equal(UnionStyleOrigin.Application, handle.Metadata.Origin);
        Assert.Same(handle.Payload, engine.DefaultStyle.Payload);
        Assert.Equal(UnionStyleOrigin.Application, engine.DefaultStyle.Metadata.Origin);
        Assert.Equal("breeze", ((ManagedStyle)handle.Payload).Id);
    }

    [Fact]
    public void App_Package_Shadows_Installed_Id_Native()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var package = UnionStylePackage.FromDirectory(CopyPackage("breeze"));
        var result = NativeHost.Run(e =>
        {
            var before = e.DefaultStyle.Payload;
            var defaultId = e.DefaultStyle.Id;
            e.AddStylePackages([package]);
            try
            {
                var handle = e.GetStyle("breeze")!;
                return (defaultId,
                    Origin: handle.Metadata.Origin,
                    Follows: ReferenceEquals(handle.Payload, e.DefaultStyle.Payload) || handle.Payload.Equals(e.DefaultStyle.Payload),
                    Path: e.GetStyleInfo(handle).Path,
                    Description: e.Description,
                    Before: before);
            }
            finally
            {
                e.RemoveStylePackages();
            }
        });

        var restored = NativeHost.Run(e => e.DefaultStyle.Payload.Equals(result.Before) && e.GetStyle("breeze")!.Metadata.Origin == UnionStyleOrigin.Installed);
        Assert.Equal(UnionStyleOrigin.Application, result.Origin);
        Assert.Equal(package.Directory, result.Path);
        Assert.True(restored);
        if (result.defaultId == "breeze")
        {
            Assert.True(result.Follows);
            Assert.Contains("style breeze (app)", result.Description, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Metadata_Is_Localized()
    {
        var json = File.ReadAllText(Path.Combine(PackageDir, "metadata.json"));
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            Assert.Equal("AvaUnion-Test", UnionStyleMetadata.Parse(Id, json, UnionStyleOrigin.Application).Name);
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var metadata = UnionStyleMetadata.Parse(Id, json, UnionStyleOrigin.Application);
            Assert.Equal("AvaUnion Test", metadata.Name);
            Assert.Equal(["AvaUnion"], metadata.Authors);
            Assert.False(metadata.IsHidden);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }

        Assert.Equal(Id, UnionStyleMetadata.Parse(Id, "not json", UnionStyleOrigin.Installed).Id);
    }

    [Fact]
    public void Styles_Lists_App_First_Without_Hidden_Managed()
    {
        var styles = Managed(SnapshotPackage()).Styles;
        Assert.Equal(Id, styles[0].Id);
        Assert.Equal(UnionStyleOrigin.Application, styles[0].Origin);
        Assert.Equal(styles.Count, styles.Select(s => s.Id).Distinct().Count());
        Assert.Contains(styles, s => s.Id == "breeze");
        Assert.DoesNotContain(styles, s => s.Id == "breeze-rtl");
    }

    [Fact]
    public void Styles_Lists_App_First_Without_Hidden_Native()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var hidden = CopyPackage("avaunion-hidden", dir =>
        {
            var file = Path.Combine(dir, "metadata.json");
            File.WriteAllText(file, File.ReadAllText(file).Replace("\"version\"", "\"hidden\": true,\n    \"version\""));
        });
        var styles = NativeHost.Run(e =>
        {
            e.AddStylePackages([UnionStylePackage.FromDirectory(PackageDir), UnionStylePackage.FromDirectory(hidden), UnionStylePackage.FromDirectory(CopyPackage("breeze"))]);
            try
            {
                return e.Styles;
            }
            finally
            {
                e.RemoveStylePackages();
            }
        });

        Assert.Equal(Id, styles[0].Id);
        Assert.Equal("breeze", styles[1].Id);
        Assert.Equal(UnionStyleOrigin.Application, styles[1].Origin);
        Assert.Equal(styles.Count, styles.Select(s => s.Id).Distinct().Count());
        Assert.DoesNotContain(styles, s => s.Id == "avaunion-hidden");
        Assert.DoesNotContain(styles, s => s.Id == "breeze-rtl");
        Assert.All(styles.Skip(2), s => Assert.Equal(UnionStyleOrigin.Installed, s.Origin));
    }

    [Fact]
    public void Snapshot_With_Other_Schema_Falls_Back()
    {
        var data = File.ReadAllBytes(SnapshotFile);
        var schema = Encoding.ASCII.GetBytes(UnionLeafInfo.Schema);
        var at = data.AsSpan().LastIndexOf(schema);
        Assert.True(at > 0);
        data[at] = (byte)(data[at] == (byte)'0' ? '1' : '0');
        var engine = Managed(UnionStylePackage.FromSnapshot(Id, () => new MemoryStream(data)));
        Assert.Null(engine.GetStyle(Id));
        var line = Assert.Single(engine.AppStyleLogLines);
        Assert.StartsWith($"AvaUnion: style {Id} unavailable: ", line, StringComparison.Ordinal);
        Assert.Contains("schema", line, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_Part_Logs_Once_Managed()
    {
        var engine = Managed(UnionStylePackage.FromDirectory(PackageDir), UnionStylePackage.FromDirectory(PackageDir));
        Assert.Null(engine.GetStyle(Id));
        Assert.Equal([$"AvaUnion: style {Id} unavailable: no snapshot for the managed engine"], engine.AppStyleLogLines);
    }

    [Fact]
    public void Missing_Part_Logs_Once_Native()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        const string id = "avaunion-snapshot-only";
        var (first, second, lines) = NativeHost.Run(e =>
        {
            var before = e.AppStyleLogLines.Count;
            e.AddStylePackages([SnapshotPackage(id)]);
            try
            {
                var first = e.GetStyle(id);
                var second = e.GetStyle(id);
                return (first, second, e.AppStyleLogLines.Skip(before).ToList());
            }
            finally
            {
                e.RemoveStylePackages();
            }
        });

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal([$"AvaUnion: style {id} unavailable: no package folder for the native engine"], lines);
    }

    [Fact]
    public void Export_Rejects_Outside_Paths()
    {
        var root = Directory.CreateTempSubdirectory("avaunion-export-").FullName;
        var export = Path.Combine(root, "outside.json");
        using (var source = JsonDocument.Parse(File.ReadAllText(TestData.Path("styles/avaunion-test.json"))))
        using (var stream = File.Create(export))
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in source.RootElement.EnumerateObject())
            {
                if (property.Name == "outside")
                {
                    writer.WriteStartArray("outside");
                    writer.WriteStringValue("breeze/contents/css/style.css");
                    writer.WriteEndArray();
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        var (code, error) = RunSnapshotTool("--export", export, "--out", Path.Combine(root, "out.union.bin"));
        Assert.True(code != 0, error);
        Assert.Contains("breeze/contents/css/style.css", error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "out.union.bin")));

        var (good, goodError) = RunSnapshotTool("--export", TestData.Path("styles/avaunion-test.json"), "--out", SnapshotFile, "--check");
        Assert.True(good == 0, goodError);
    }

    [Fact]
    public void Export_Rejects_Shared_Variables()
    {
        var root = Directory.CreateTempSubdirectory("avaunion-export-").FullName;
        var export = Path.Combine(root, "shared.json");
        using (var source = JsonDocument.Parse(File.ReadAllText(TestData.Path("styles/avaunion-test.json"))))
        using (var stream = File.Create(export))
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in source.RootElement.EnumerateObject())
            {
                if (property.Name == "sharedVariables")
                {
                    writer.WriteStartArray("sharedVariables");
                    writer.WriteStartObject();
                    writer.WriteString("name", "--highlight-color");
                    writer.WriteStartArray("styles");
                    writer.WriteStringValue("breeze");
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        var (code, error) = RunSnapshotTool("--export", export, "--out", Path.Combine(root, "out.union.bin"));
        Assert.True(code != 0, error);
        Assert.Contains("--highlight-color, which breeze also defines", error, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "out.union.bin")));
    }

    [Fact]
    public unsafe void Native_Export_Lists_Variables_Other_Styles_Define()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var package = CopyPackage("avaunion-shared", dir => File.WriteAllText(
            Path.Combine(dir, "contents", "css", "colors.css"),
            ":root { --highlight-color: #ff0000; --avaunion-shared-own: #00ff00; --accent: #3daee9; --frame-color: #808080; --text-color: #000000; --window-color: #ffffff; --view-color: #ffffff; --button-color: #eeeeee; }"));
        var output = Path.Combine(Path.GetDirectoryName(package)!, "export.json");
        var status = NativeHost.Run(e =>
        {
            e.AddStylePackages([UnionStylePackage.FromDirectory(package)]);
            try
            {
                var handle = e.GetStyle("avaunion-shared")!;
                fixed (char* p = output)
                {
                    return Engine.Native.NativeMethods.ExportStyle((IntPtr)handle.Payload, p, output.Length);
                }
            }
            finally
            {
                e.RemoveStylePackages();
            }
        });

        Assert.Equal(0, status);
        using var export = JsonDocument.Parse(File.ReadAllText(output));
        var shared = export.RootElement.GetProperty("sharedVariables").EnumerateArray().ToList();
        var entry = Assert.Single(shared, s => s.GetProperty("name").GetString() == "--highlight-color");
        Assert.Contains("breeze", entry.GetProperty("styles").EnumerateArray().Select(s => s.GetString()));
        Assert.DoesNotContain(shared, s => s.GetProperty("name").GetString() == "--avaunion-shared-own");
    }

    private static (int Code, string Error) RunSnapshotTool(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = TestData.RepoRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        foreach (var argument in new[] { "run", "tools/union-snapshot/union-snapshot.cs", "--" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, error.Result + output.Result);
    }

    [Fact]
    public void Discover_Finds_Resource_And_Folder()
    {
        var packages = UnionStylePackage.Discover(typeof(AppStyleTests).Assembly);
        var package = Assert.Single(packages, p => p.Id == Id);
        Assert.NotNull(package.OpenSnapshot);
        Assert.NotNull(package.Directory);
        Assert.True(File.Exists(Path.Combine(package.Directory, "contents", "css", "style.css")));
        using (var stream = package.OpenSnapshot())
        {
            Assert.Contains(Id, Snapshot.Load(stream).Styles.Keys);
        }

        var root = Directory.CreateTempSubdirectory("avaunion-discover-").FullName;
        Directory.CreateDirectory(Path.Combine(root, UnionStylePackage.OutputFolder, "folder-only"));
        File.WriteAllText(Path.Combine(root, UnionStylePackage.OutputFolder, "folder-only", "metadata.json"), "{}");
        Directory.CreateDirectory(Path.Combine(root, UnionStylePackage.OutputFolder, "no-metadata"));
        var found = UnionStylePackage.Discover(null, root);
        var folderOnly = Assert.Single(found);
        Assert.Equal("folder-only", folderOnly.Id);
        Assert.Null(folderOnly.OpenSnapshot);
    }

    [Fact]
    public void Explicit_Packages_Come_Before_Discovered_Ones()
    {
        var options = new AvaUnionOptions { DiscoverStylePackages = true };
        options.StylePackages.Add(SnapshotPackage("first"));
        options.StylePackages.Add(SnapshotPackage("first"));
        var list = EngineSelector.StylePackages(options);
        Assert.Equal("first", list[0].Id);
        Assert.Equal(list.Count, list.Select(p => p.Id).Distinct().Count());
    }
}
