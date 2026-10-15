#:property ImplicitUsings=enable

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var check = args.Contains("--check");
var root = FindRoot();
var output = Path.Combine(root, "tests/Data/parity/chains.json");

var chains = new List<(string Id, List<Part> Chain)>();
var seen = new HashSet<string>(StringComparer.Ordinal);
var types = new SortedSet<string>(StringComparer.Ordinal);
var hintPairs = new SortedSet<(string Type, string Hint)>();
var attributePairs = new SortedSet<(string Type, string Name, string Value)>();

string[] stateNames = ["hovered", "active-focus", "visual-focus", "pressed", "checked", "disabled", "highlighted"];
string[][] stateCombos =
[
    [],
    ["hovered"], ["active-focus"], ["visual-focus", "active-focus"], ["pressed"], ["checked"], ["disabled"], ["highlighted"],
    ["hovered", "pressed"], ["checked", "hovered"], ["disabled", "checked"], ["visual-focus", "active-focus", "checked"],
];

foreach (var styleId in new[] { "breeze", "breeze-rtl", "breeze-mobile" })
{
    using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tests/Data/snapshot", styleId + ".json")));
    var index = 0;
    foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
    {
        var compounds = new List<(Part Part, string? CombinatorBefore)>();
        var current = new Part();
        string? pending = null;
        foreach (var s in rule.GetProperty("selectors").EnumerateArray())
        {
            var kind = s.GetProperty("kind").GetString();
            switch (kind)
            {
                case "ChildCombinator":
                case "DescendantCombinator":
                    compounds.Add((current, pending));
                    current = new Part();
                    pending = kind;
                    break;
                case "Type":
                    current.Type = s.GetProperty("value").GetString()!.ToLowerInvariant();
                    break;
                case "Id":
                    current.Id = s.GetProperty("value").GetString();
                    break;
                case "Hint":
                    current.Hints.Add(s.GetProperty("value").GetString()!);
                    break;
                case "State":
                    current.States.AddRange(StateNames(s.GetProperty("state").GetInt32()));
                    break;
                case "AttributeExists":
                    current.Attributes[s.GetProperty("value").GetString()!] = "x";
                    break;
                case "AttributeEquals":
                case "AttributeSubstringMatch":
                {
                    var v = s.GetProperty("value");
                    current.Attributes[s.GetProperty("name").GetString()!] = v.ValueKind == JsonValueKind.Number ? v.GetDouble() : v.GetString()!;
                    break;
                }
            }
        }

        compounds.Add((current, pending));

        foreach (var (part, _) in compounds)
        {
            if (part.Type is { Length: > 0 } t)
            {
                types.Add(t);
                foreach (var h in part.Hints)
                {
                    hintPairs.Add((t, h));
                }

                foreach (var (k, v) in part.Attributes)
                {
                    attributePairs.Add((t, k, v.ToString()!));
                }
            }
        }

        var chain = compounds.Select(c => c.Part.Clone()).ToList();
        Add($"{styleId}/rule{index}", chain);

        if (compounds.Any(c => c.CombinatorBefore == "DescendantCombinator"))
        {
            var deep = new List<Part>();
            foreach (var (part, combinator) in compounds)
            {
                if (combinator == "DescendantCombinator")
                {
                    deep.Add(new Part { Type = "item" });
                }

                deep.Add(part.Clone());
            }

            Add($"{styleId}/rule{index}/deep", deep);
        }

        if (compounds[0].Part.Type != "applicationwindow")
        {
            Add($"{styleId}/rule{index}/window", [new Part { Type = "applicationwindow" }, .. chain.Select(p => p.Clone())]);
        }

        index++;
    }
}

foreach (var type in types)
{
    Add($"type/{type}", [new Part { Type = type }]);
    foreach (var combo in stateCombos)
    {
        var name = combo.Length == 0 ? "none" : string.Join('+', combo);
        Add($"window/{type}/{name}", [new Part { Type = "applicationwindow" }, new Part { Type = type, States = [.. combo] }]);
    }

    Add($"inactive/{type}", [new Part { Type = "applicationwindow", Hints = ["inactive"] }, new Part { Type = type }]);
    Add($"inactive/{type}/checked", [new Part { Type = "applicationwindow", Hints = ["inactive"] }, new Part { Type = type, States = ["checked"] }]);
}

foreach (var (type, hint) in hintPairs)
{
    Add($"hint/{type}.{hint}", [new Part { Type = "applicationwindow" }, new Part { Type = type, Hints = [hint] }]);
    Add($"hint/{type}.{hint}/hovered", [new Part { Type = "applicationwindow" }, new Part { Type = type, Hints = [hint], States = ["hovered"] }]);
    Add($"hint/{type}.{hint}/upper", [new Part { Type = "applicationwindow" }, new Part { Type = type, Hints = [hint.ToUpperInvariant()] }]);
}

foreach (var (type, name, value) in attributePairs)
{
    Add($"attr/{type}[{name}={value}]", [new Part { Type = "applicationwindow" }, new Part { Type = type, Attributes = new() { [name] = value } }]);
    Add($"attr/{type}[{name}={value}]/case", [new Part { Type = "applicationwindow" }, new Part { Type = type, Attributes = new() { [name.ToUpperInvariant()] = value.ToUpperInvariant() } }]);
}

Add("attrtype/number", [new Part { Type = "applicationwindow" }, new Part { Type = "button", Attributes = new() { ["display"] = 1.0 } }]);
Add("attrtype/number-colorset", [new Part { Type = "applicationwindow" }, new Part { Type = "scrollview", Attributes = new() { ["colorset"] = 2.0 } }]);
Add("attrtype/string-number", [new Part { Type = "applicationwindow" }, new Part { Type = "button", Attributes = new() { ["display"] = "1" } }]);

foreach (var hint in new[] { "combobox", "menu" })
{
    Add($"popup/{hint}", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = [hint] }]);
    Add($"popup/{hint}/itemdelegate", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = [hint] }, new Part { Type = "itemdelegate", Hints = ["inside-list", "hover-enabled"] }]);
    Add($"popup/{hint}/itemdelegate/hovered", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = [hint] }, new Part { Type = "itemdelegate", Hints = ["inside-list", "hover-enabled"], States = ["hovered"] }]);
}

Add("popup/menu/menuitem", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = ["menu"] }, new Part { Type = "menu" }, new Part { Type = "menuitem" }]);
Add("popup/menu/menuitem/highlighted", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = ["menu"] }, new Part { Type = "menu" }, new Part { Type = "menuitem", States = ["highlighted"] }]);
Add("popup/menu/menuitem/indicator", [new Part { Type = "applicationwindow" }, new Part { Type = "popup", Hints = ["menu"] }, new Part { Type = "menu" }, new Part { Type = "menuitem", Hints = ["exclusive"], States = ["checked"] }, new Part { Type = "indicator" }]);
Add("popup/tooltip", [new Part { Type = "applicationwindow" }, new Part { Type = "popup" }, new Part { Type = "tooltip" }]);

string[][] nested =
[
    ["button", "indicator"], ["checkbox", "indicator"], ["radiobutton", "indicator"], ["switch", "indicator"], ["switch", "indicator", "handle"],
    ["spinbox", "indicator"], ["combobox", "indicator"], ["slider", "handle"], ["slider", "fill"], ["slider", "tickmark"],
    ["progressbar", "track"], ["progressbar", "fill"], ["scrollbar", "handle"], ["scrollview", "scrollbar", "handle"],
    ["menuitem", "indicator"], ["menuitem", "arrow"], ["groupbox", "header"], ["splitview", "handle"], ["tabbar", "tabbutton"],
    ["treeviewdelegate", "indicator"], ["treeviewdelegate", "indentation"], ["toolbar", "toolbutton"], ["toolbar", "button"],
    ["monthgrid", "label"], ["dayofweekrow", "label"], ["pageindicator", "pageindicatordelegate"], ["tumbler", "text"],
    ["dialog", "dialogbuttonbox", "button"], ["applicationheader", "toolbutton"], ["titlebar", "closebutton"],
];
foreach (var path in nested)
{
    var basePath = new List<Part> { new() { Type = "applicationwindow" } };
    basePath.AddRange(path.Select(t => new Part { Type = t }));
    Add("nested/" + string.Join('>', path), basePath);
    foreach (var state in new[] { "hovered", "pressed", "checked", "disabled" })
    {
        var copy = basePath.Select(p => p.Clone()).ToList();
        copy[^2].States.Add(state);
        Add($"nested/{string.Join('>', path)}/parent-{state}", copy);
        copy = basePath.Select(p => p.Clone()).ToList();
        copy[^1].States.Add(state);
        Add($"nested/{string.Join('>', path)}/{state}", copy);
    }
}

var json = new JsonArray();
foreach (var (id, chain) in chains)
{
    var parts = new JsonArray();
    foreach (var p in chain)
    {
        var o = new JsonObject { ["type"] = p.Type ?? string.Empty };
        if (p.Id is not null)
        {
            o["id"] = p.Id;
        }

        if (p.States.Count > 0)
        {
            o["states"] = new JsonArray([.. p.States.Distinct().Select(s => (JsonNode)s!)]);
        }

        if (p.Hints.Count > 0)
        {
            o["hints"] = new JsonArray([.. p.Hints.Select(s => (JsonNode)s!)]);
        }

        if (p.Attributes.Count > 0)
        {
            var attrs = new JsonObject();
            foreach (var (k, v) in p.Attributes)
            {
                attrs[k] = v is double d ? JsonValue.Create(d) : JsonValue.Create((string)v);
            }

            o["attributes"] = attrs;
        }

        parts.Add(o);
    }

    json.Add(new JsonObject { ["id"] = id, ["chain"] = parts });
}

var text = json.ToJsonString(new JsonSerializerOptions { WriteIndented = false }).Replace("},{\"id\"", "},\n{\"id\"") + "\n";
var existing = File.Exists(output) ? File.ReadAllText(output) : null;
if (existing == text)
{
    Console.WriteLine($"up to date: {chains.Count} chains");
    return 0;
}

if (check)
{
    Console.Error.WriteLine("stale: tests/Data/parity/chains.json");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, text);
Console.WriteLine($"wrote {chains.Count} chains");
return 0;

void Add(string id, List<Part> chain)
{
    var key = string.Join(" > ", chain.Select(p => p.Key()));
    if (seen.Add(key))
    {
        chains.Add((id, chain));
    }
}

IEnumerable<string> StateNames(int bits)
{
    for (var i = 0; i < stateNames.Length; i++)
    {
        if ((bits & (1 << i)) != 0)
        {
            yield return stateNames[i];
        }
    }
}

static string FindRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir is not null && !File.Exists(Path.Combine(dir, "AvaUnion.slnx")))
    {
        dir = Path.GetDirectoryName(dir);
    }

    return dir ?? throw new InvalidOperationException("run from inside the repository");
}

sealed class Part
{
    public string? Type { get; set; }

    public string? Id { get; set; }

    public List<string> States { get; set; } = [];

    public List<string> Hints { get; set; } = [];

    public SortedDictionary<string, object> Attributes { get; set; } = new(StringComparer.Ordinal);

    public Part Clone() => new()
    {
        Type = Type,
        Id = Id,
        States = [.. States],
        Hints = [.. Hints],
        Attributes = new SortedDictionary<string, object>(Attributes, StringComparer.Ordinal),
    };

    public string Key()
    {
        var sb = new StringBuilder(Type ?? "*");
        if (Id is not null)
        {
            sb.Append('#').Append(Id);
        }

        foreach (var h in Hints)
        {
            sb.Append('.').Append(h);
        }

        foreach (var s in States.Distinct().Order())
        {
            sb.Append(':').Append(s);
        }

        foreach (var (k, v) in Attributes)
        {
            sb.Append('[').Append(k).Append('=').Append(v is double d ? "#" + d : v).Append(']');
        }

        return sb.ToString();
    }
}
