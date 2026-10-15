using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class NativeParityTests
{
    public static IEnumerable<object[]> Runs() => ManagedParityTests.Runs();

    [Theory]
    [MemberData(nameof(Runs))]
    public void Native_Engine_Matches_Export(string style, string scheme)
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var corpus = ParityCorpus.Load();
        var run = corpus.Runs.Single(r => r.Style == style && r.Scheme == scheme);
        var failures = NativeHost.Run(engine =>
        {
            engine.SetColorScheme($"/usr/share/color-schemes/{scheme}.colors");
            var handle = engine.GetStyle(style)!;
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

            engine.SetColorScheme(null);
            return failures;
        });

        Assert.True(failures.Count == 0, $"{failures.Count} chains differ:\n" + string.Join("\n", failures.Take(25)));
    }
}
