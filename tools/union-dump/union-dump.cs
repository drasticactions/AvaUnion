#:project ../../src/AvaUnion.Theme/AvaUnion.Theme.csproj
#:property ImplicitUsings=enable
#:property PublishAot=false

using System.Text.RegularExpressions;
using AvaUnion.Engine;
using AvaUnion.Engine.Managed;

var engine = new ManagedEngine();
var style = engine.DefaultStyle;
var chains = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dark": engine.SetColorScheme("BreezeDark"); break;
        case "--style": style = engine.GetStyle(args[++i]) ?? throw new ArgumentException(args[i]); break;
        default: chains.Add(args[i]); break;
    }
}

foreach (var text in chains)
{
    var elements = new List<ElementHandle>();
    foreach (var part in text.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    {
        var e = engine.CreateElement();
        var m = Regex.Match(part, @"^([a-z0-9-]*)");
        e.SetType(m.Groups[1].Value);
        var hints = new List<string>();
        var states = UnionStates.None;
        foreach (Match t in Regex.Matches(part, @"([.:#])([a-z0-9-]+)|\[([a-z-]+)=([^\]]+)\]"))
        {
            if (t.Groups[3].Success)
            {
                e.SetAttribute(t.Groups[3].Value, t.Groups[4].Value);
            }
            else if (t.Groups[1].Value == ".")
            {
                hints.Add(t.Groups[2].Value);
            }
            else if (t.Groups[1].Value == ":")
            {
                states |= UnionStateNames.Parse(t.Groups[2].Value);
            }
            else
            {
                e.SetId(t.Groups[2].Value);
            }
        }

        e.SetHints(hints);
        e.SetStates(states);
        elements.Add(e);
    }

    var r = engine.Query(style, elements.ToArray());
    Console.WriteLine($"== {text}  (matched: {r.IsMatch})");
    for (var i = 0; i < UnionLeafInfo.Count; i++)
    {
        var v = r.GetLeaf(i);
        if (v is null || UnionLeafInfo.Names[i].Contains("alignment.order"))
        {
            continue;
        }

        Console.WriteLine($"   {UnionLeafInfo.Names[i],-34} {v}");
    }
}
