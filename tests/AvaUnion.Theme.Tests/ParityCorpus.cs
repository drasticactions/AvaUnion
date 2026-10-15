using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using AvaUnion.Engine;
using Avalonia.Media;

namespace AvaUnion.Theme.Tests;

internal sealed class ParityCorpus
{
    private ParityCorpus()
    {
    }

    public List<(string Id, JsonElement Chain)> Chains { get; } = [];

    public List<(string Style, string Scheme, (bool Matched, int Set)[] Results)> Runs { get; } = [];

    public List<JsonElement> Sets { get; } = [];

    public const string NativeExpected = "parity/expected-native.json";

    public const string AppExpected = "parity/expected-app.json";

    private static readonly Dictionary<string, ParityCorpus> s_instances = new();

    public static ParityCorpus Load(string expectedFile = NativeExpected)
    {
        lock (s_instances)
        {
            if (!s_instances.TryGetValue(expectedFile, out var corpus))
            {
                s_instances[expectedFile] = corpus = Read(expectedFile);
            }

            return corpus;
        }
    }

    private static ParityCorpus Read(string expectedFile)
    {
        var corpus = new ParityCorpus();
        var chains = JsonDocument.Parse(File.ReadAllText(TestData.Path("parity/chains.json")));
        foreach (var entry in chains.RootElement.EnumerateArray())
        {
            corpus.Chains.Add((entry.GetProperty("id").GetString()!, entry.GetProperty("chain")));
        }

        var expected = JsonDocument.Parse(File.ReadAllText(TestData.Path(expectedFile)));
        foreach (var set in expected.RootElement.GetProperty("sets").EnumerateArray())
        {
            corpus.Sets.Add(set);
        }

        foreach (var run in expected.RootElement.GetProperty("runs").EnumerateArray())
        {
            var results = new List<(bool, int)>();
            foreach (var r in run.GetProperty("results").EnumerateArray())
            {
                results.Add((r[0].GetInt32() == 1, r[1].GetInt32()));
            }

            corpus.Runs.Add((run.GetProperty("style").GetString()!, run.GetProperty("scheme").GetString()!, results.ToArray()));
        }

        return corpus;
    }

    public static ElementHandle[] BuildChain(IUnionEngine engine, JsonElement chain)
    {
        var result = new List<ElementHandle>();
        foreach (var part in chain.EnumerateArray())
        {
            var e = engine.CreateElement();
            e.SetType(part.GetProperty("type").GetString());
            if (part.TryGetProperty("id", out var id))
            {
                e.SetId(id.GetString());
            }

            var states = UnionStates.None;
            if (part.TryGetProperty("states", out var s))
            {
                foreach (var state in s.EnumerateArray())
                {
                    states |= UnionStateNames.Parse(state.GetString()!);
                }
            }

            e.SetStates(states);
            var hints = new List<string>();
            if (part.TryGetProperty("hints", out var h))
            {
                foreach (var hint in h.EnumerateArray())
                {
                    hints.Add(hint.GetString()!);
                }
            }

            e.SetHints(hints);
            if (part.TryGetProperty("attributes", out var a))
            {
                foreach (var attr in a.EnumerateObject())
                {
                    if (attr.Value.ValueKind == JsonValueKind.Number)
                    {
                        e.SetAttribute(attr.Name, attr.Value.GetDouble());
                    }
                    else
                    {
                        e.SetAttribute(attr.Name, attr.Value.GetString()!);
                    }
                }
            }

            result.Add(e);
        }

        return [.. result];
    }

    public static List<string> Compare(ResolvedStyle actual, JsonElement expected, bool normalizePaths = false)
    {
        var diffs = new List<string>();
        for (var i = 0; i < UnionLeafInfo.Count; i++)
        {
            var name = UnionLeafInfo.Names[i];
            var value = actual.GetLeaf(i);
            var has = expected.TryGetProperty(name, out var e);
            if (value is null && !has)
            {
                continue;
            }

            if (value is null || !has)
            {
                diffs.Add($"{name}: expected {(has ? e.ToString() : "unset")}, got {Format(value)}");
                continue;
            }

            if (!Equal(value, e, normalizePaths))
            {
                diffs.Add($"{name}: expected {e}, got {Format(value)}");
            }
        }

        return diffs;
    }

    private static bool Equal(object value, JsonElement e, bool normalizePaths)
    {
        switch (value)
        {
            case double d:
                return System.Math.Abs(d - e.GetDouble()) <= 1e-4;
            case int i:
                return e.GetInt32() == i;
            case bool b:
                return e.GetBoolean() == b;
            case System.Enum en:
                return e.GetInt32() == System.Convert.ToInt32(en, CultureInfo.InvariantCulture);
            case Color c:
                return string.Equals(Hex(c), e.GetString(), System.StringComparison.OrdinalIgnoreCase);
            case string s:
            {
                var expected = e.GetString() ?? string.Empty;
                if (normalizePaths && s.Length > 0 && expected.Length > 0 && s.StartsWith('/'))
                {
                    var full = System.IO.Path.GetFullPath(s);
                    return full.EndsWith("/" + expected, System.StringComparison.Ordinal) || full == expected;
                }

                return s == expected;
            }

            case UnionFont f:
                return Same(f.PointSize, e, "pointSize") && Same(f.PixelSize, e, "pixelSize") && Same(f.Weight, e, "weight")
                    && (f.Italic is null) == !e.TryGetProperty("italic", out _)
                    && (f.Italic is null || e.GetProperty("italic").GetBoolean() == f.Italic);
            default:
                return false;
        }
    }

    private static bool Same(double? value, JsonElement e, string name)
    {
        var has = e.TryGetProperty(name, out var v);
        if (value is null || !has)
        {
            return value is null && !has;
        }

        return System.Math.Abs(value.Value - v.GetDouble()) <= 1e-3;
    }

    public static string Hex(Color c) => "#" + c.A.ToString("x2", CultureInfo.InvariantCulture) + c.R.ToString("x2", CultureInfo.InvariantCulture)
        + c.G.ToString("x2", CultureInfo.InvariantCulture) + c.B.ToString("x2", CultureInfo.InvariantCulture);

    private static string Format(object? value) => value switch
    {
        null => "unset",
        Color c => Hex(c),
        UnionFont f => f.ToString(),
        _ => System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "?",
    };

    public static string Describe(JsonElement chain)
    {
        var sb = new StringBuilder();
        foreach (var part in chain.EnumerateArray())
        {
            if (sb.Length > 0)
            {
                sb.Append(" > ");
            }

            sb.Append(part.ToString());
        }

        return sb.ToString();
    }
}
