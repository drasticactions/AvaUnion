using System.Collections.Generic;
using System.Linq;
using AvaUnion.Engine.Managed;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ManagedParityTests
{
    public static IEnumerable<object[]> Runs()
    {
        var corpus = ParityCorpus.Load();
        for (var i = 0; i < corpus.Runs.Count; i++)
        {
            yield return [corpus.Runs[i].Style, corpus.Runs[i].Scheme];
        }
    }

    [Theory]
    [MemberData(nameof(Runs))]
    public void Managed_Engine_Matches_Union(string style, string scheme)
    {
        var corpus = ParityCorpus.Load();
        var run = corpus.Runs.Single(r => r.Style == style && r.Scheme == scheme);
        var engine = new ManagedEngine();
        engine.SetColorScheme(scheme);
        var handle = engine.GetStyle(style)!;
        var failures = new List<string>();
        for (var i = 0; i < corpus.Chains.Count; i++)
        {
            var (id, chain) = corpus.Chains[i];
            var elements = ParityCorpus.BuildChain(engine, chain);
            var resolved = engine.Query(handle, elements);
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
}
