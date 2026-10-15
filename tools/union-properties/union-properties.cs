#:package YamlDotNet@16.3.0
#:property ImplicitUsings=enable

using System.Security.Cryptography;
using System.Text;
using YamlDotNet.RepresentationModel;

var check = args.Contains("--check");
var root = FindRoot();
var ymlPath = Path.Combine(root, "external/union/tools/propertygenerator/properties.yml");
var ymlText = File.ReadAllText(ymlPath);
var ymlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ymlText.Replace("\r\n", "\n")))).ToLowerInvariant();
var tag = ReadTag(Path.Combine(root, "external/union/CMakeLists.txt"));

var yaml = new YamlStream();
yaml.Load(new StringReader(ymlText));
var top = (YamlMappingNode)yaml.Documents[0].RootNode;
var children = (YamlMappingNode)top["children"];

var typeDefs = new Dictionary<string, YamlMappingNode>();
foreach (var (k, v) in children.Children)
{
    var m = (YamlMappingNode)v;
    if (Str(m, "type") == "type")
    {
        typeDefs[((YamlScalarNode)k).Value!] = m;
    }
}

var leaves = new List<Leaf>();
var rootGroup = new Group("ResolvedStyle", "StylePropertyGroup", null, []);
var groupTypes = new Dictionary<string, Group>();
foreach (var (k, v) in children.Children)
{
    var name = ((YamlScalarNode)k).Value!;
    var m = (YamlMappingNode)v;
    if (typeDefs.ContainsKey(name))
    {
        continue;
    }

    rootGroup.Members.Add(BuildMember(name, m, [name]));
}

Member BuildMember(string name, YamlMappingNode node, string[] path)
{
    var type = Str(node, "type")!;
    if (type == "group")
    {
        var g = new Group(Pascal(name) + "Group", CppGroupType(name), null, []);
        foreach (var (k, v) in ((YamlMappingNode)node["children"]).Children)
        {
            var cn = ((YamlScalarNode)k).Value!;
            g.Members.Add(BuildMember(cn, (YamlMappingNode)v, [.. path, cn]));
        }

        groupTypes[g.CsType] = g;
        return new Member(name, g, null);
    }

    if (typeDefs.TryGetValue(type, out var def))
    {
        var g = new Group(Pascal(type) + "Group", CppGroupType(type), type, []);
        foreach (var (k, v) in ((YamlMappingNode)def["children"]).Children)
        {
            var cn = ((YamlScalarNode)k).Value!;
            g.Members.Add(BuildMember(cn, (YamlMappingNode)v, [.. path, cn]));
        }

        groupTypes.TryAdd(g.CsType, g);
        return new Member(name, g, null);
    }

    var leaf = new Leaf(leaves.Count, path, type, KindOf(type));
    leaves.Add(leaf);
    return new Member(name, null, leaf);
}

if (leaves.Count > 128)
{
    throw new InvalidOperationException($"{leaves.Count} leaves do not fit the 128-bit presence mask");
}

var header = $"union-properties: properties.yml sha256={ymlHash} tag={tag} leaves={leaves.Count}";

var outputs = new Dictionary<string, string>
{
    ["src/AvaUnion.Theme/Engine/ResolvedStyle.g.cs"] = EmitResolvedStyle(),
    ["src/AvaUnion.Theme/Engine/UnionLeafInfo.g.cs"] = EmitLeafInfo(),
    ["src/AvaUnion.Theme/Engine/Native/AvuResolved.g.cs"] = EmitAvuResolvedCs(),
    ["native/src/avu_resolved.g.h"] = EmitAvuResolvedH(),
    ["native/src/Leaves.g.h"] = EmitLeavesH(),
    ["native/src/Resolved.g.cpp"] = EmitResolvedCpp(),
    ["tools/union-properties/leaves.g.json"] = EmitLeavesJson(),
    ["src/AvaUnion.Theme/Markup/StylePaths.g.cs"] = EmitStylePaths(),
};

var stale = 0;
foreach (var (rel, text) in outputs)
{
    var full = Path.Combine(root, rel);
    var normalized = text.Replace("\r\n", "\n");
    var existing = File.Exists(full) ? File.ReadAllText(full) : null;
    if (existing == normalized)
    {
        continue;
    }

    if (check)
    {
        Console.Error.WriteLine($"stale: {rel}");
        stale++;
        continue;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    File.WriteAllText(full, normalized);
    Console.WriteLine($"wrote {rel}");
}

if (check && stale > 0)
{
    return 1;
}

Console.WriteLine($"{leaves.Count} leaves, {groupTypes.Count} group types");
return 0;

string EmitResolvedStyle()
{
    var sb = new StringBuilder();
    sb.Append(CsHeader());
    sb.AppendLine("using System;");
    sb.AppendLine("using Avalonia.Media;");
    sb.AppendLine("using AvaUnion.Engine.Managed;");
    sb.AppendLine("using AvaUnion.Engine.Native;");
    sb.AppendLine();
    sb.AppendLine("namespace AvaUnion.Engine;");
    sb.AppendLine();
    sb.AppendLine("public sealed partial record ResolvedStyle");
    sb.AppendLine("{");
    EmitMembers(sb, rootGroup, "    ");
    foreach (var g in groupTypes.Values.OrderBy(g => g.CsType, StringComparer.Ordinal))
    {
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>A group of style values.</summary>");
        sb.AppendLine($"    public sealed partial record {g.CsType}");
        sb.AppendLine("    {");
        sb.AppendLine($"        /// <summary>A group that has no values.</summary>");
        sb.AppendLine($"        public static readonly {g.CsType} Empty = new();");
        sb.AppendLine();
        EmitMembers(sb, g, "        ");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>True when one or more values in this group are set.</summary>");
        sb.AppendLine("        public bool HasAnyValue =>");
        var conds = g.Members.Select(m => m.Group is not null ? $"{Pascal(m.Name)}.HasAnyValue" : $"{Pascal(m.Name)} is not null").ToList();
        sb.AppendLine("            " + string.Join("\n            || ", conds) + ";");
        sb.AppendLine("    }");
    }

    sb.AppendLine();
    sb.AppendLine("    internal static ResolvedStyle FromLeaves(ReadOnlySpan<object?> v, Func<UnionColor, Color> resolve)");
    sb.AppendLine("    {");
    sb.AppendLine("        return new ResolvedStyle");
    sb.AppendLine("        {");
    EmitInit(sb, rootGroup, "            ", leaf => leaf.Kind switch
    {
        Kind.Color => $"v[{leaf.Index}] is UnionColor c{leaf.Index} ? resolve(c{leaf.Index}) : null",
        _ => $"({CsLeafType(leaf)})v[{leaf.Index}]",
    });
    sb.AppendLine("        };");
    sb.AppendLine("    }");

    sb.AppendLine();
    sb.AppendLine("    internal static ResolvedStyle FromNative(in AvuResolved r, Func<int, string?> str)");
    sb.AppendLine("    {");
    sb.AppendLine("        return new ResolvedStyle");
    sb.AppendLine("        {");
    EmitInit(sb, rootGroup, "            ", leaf =>
    {
        var has = $"r.Has({leaf.Index})";
        var f = $"r.{CsField(leaf)}";
        return leaf.Kind switch
        {
            Kind.Real => $"{has} ? (double){f} : null",
            Kind.Int => $"{has} ? {f} : null",
            Kind.Bool => $"{has} ? {f} != 0 : null",
            Kind.Enum or Kind.Flags => $"{has} ? ({CsEnum(leaf.CppType)}){f} : null",
            Kind.Color => $"{has} ? Color.FromUInt32({f}) : null",
            Kind.String or Kind.Url or Kind.Path => $"{has} ? str({f}) : null",
            Kind.Font => $"{has} ? r.ReadFont(str) : null",
            _ => throw new InvalidOperationException(),
        };
    });
    sb.AppendLine("        };");
    sb.AppendLine("    }");

    sb.AppendLine();
    sb.AppendLine("    /// <summary>Gets the value of the leaf at a <see cref=\"UnionLeafInfo\"/> index. The value is boxed. It is null when the leaf is not set.</summary>");
    sb.AppendLine("    public object? GetLeaf(int index)");
    sb.AppendLine("    {");
    sb.AppendLine("        return index switch");
    sb.AppendLine("        {");
    foreach (var leaf in leaves)
    {
        sb.AppendLine($"            {leaf.Index} => {string.Join('.', leaf.Path.Select(Pascal))},");
    }

    sb.AppendLine("            _ => throw new ArgumentOutOfRangeException(nameof(index)),");
    sb.AppendLine("        };");
    sb.AppendLine("    }");
    sb.AppendLine("}");
    return sb.ToString();
}

void EmitMembers(StringBuilder sb, Group g, string indent)
{
    var first = true;
    foreach (var m in g.Members)
    {
        if (!first)
        {
            sb.AppendLine();
        }

        first = false;
        if (m.Group is not null)
        {
            sb.AppendLine($"{indent}/// <summary>The <c>{m.Name}</c> group.</summary>");
            sb.AppendLine($"{indent}public {m.Group.CsType} {Pascal(m.Name)} {{ get; init; }} = {m.Group.CsType}.Empty;");
        }
        else
        {
            var leaf = m.Leaf!;
            sb.AppendLine($"{indent}/// <summary>The <c>{string.Join('.', leaf.Path)}</c> value. It is null when it is not set.</summary>");
            sb.AppendLine($"{indent}public {CsLeafType(leaf)} {Pascal(m.Name)} {{ get; init; }}");
        }
    }
}

void EmitInit(StringBuilder sb, Group g, string indent, Func<Leaf, string> leafExpr)
{
    foreach (var m in g.Members)
    {
        if (m.Group is not null)
        {
            sb.AppendLine($"{indent}{Pascal(m.Name)} = new {m.Group.CsType}");
            sb.AppendLine($"{indent}{{");
            EmitInit(sb, m.Group, indent + "    ", leafExpr);
            sb.AppendLine($"{indent}}},");
        }
        else
        {
            sb.AppendLine($"{indent}{Pascal(m.Name)} = {leafExpr(m.Leaf!)},");
        }
    }
}

string EmitLeafInfo()
{
    var sb = new StringBuilder();
    sb.Append(CsHeader());
    sb.AppendLine("namespace AvaUnion.Engine;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>The leaves of the style property tree, in schema order.</summary>");
    sb.AppendLine("public static class UnionLeafInfo");
    sb.AppendLine("{");
    sb.AppendLine($"    /// <summary>The number of leaves.</summary>");
    sb.AppendLine($"    public const int Count = {leaves.Count};");
    sb.AppendLine();
    sb.AppendLine($"    /// <summary>The schema hash and the Union tag of the generated code.</summary>");
    sb.AppendLine($"    public const string Source = \"{header}\";");
    sb.AppendLine();
    sb.AppendLine($"    /// <summary>The SHA-256 of the Union property schema the generated code was made from.</summary>");
    sb.AppendLine($"    public const string Schema = \"{ymlHash}\";");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>The schema name of each leaf, for example <c>layout.padding.left</c>.</summary>");
    sb.AppendLine("    public static readonly string[] Names =");
    sb.AppendLine("    [");
    foreach (var leaf in leaves)
    {
        sb.AppendLine($"        \"{string.Join('.', leaf.Path)}\",");
    }

    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>The value kind of each leaf.</summary>");
    sb.AppendLine("    public static readonly UnionLeafKind[] Kinds =");
    sb.AppendLine("    [");
    foreach (var leaf in leaves)
    {
        sb.AppendLine($"        UnionLeafKind.{leaf.Kind},");
    }

    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>The managed enum type name of each enum or flag leaf. It is null for other leaves.</summary>");
    sb.AppendLine("    public static readonly string?[] EnumTypes =");
    sb.AppendLine("    [");
    foreach (var leaf in leaves)
    {
        sb.AppendLine(leaf.Kind is Kind.Enum or Kind.Flags ? $"        \"{CsEnum(leaf.CppType)}\"," : "        null,");
    }

    sb.AppendLine("    ];");
    sb.AppendLine("}");
    return sb.ToString();
}

string EmitAvuResolvedCs()
{
    var sb = new StringBuilder();
    sb.Append(CsHeader());
    sb.AppendLine("using System.Runtime.InteropServices;");
    sb.AppendLine();
    sb.AppendLine("namespace AvaUnion.Engine.Native;");
    sb.AppendLine();
    sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
    sb.AppendLine("internal partial struct AvuResolved");
    sb.AppendLine("{");
    sb.AppendLine("    public ulong Present0;");
    sb.AppendLine("    public ulong Present1;");
    foreach (var leaf in leaves)
    {
        if (leaf.Kind == Kind.Font)
        {
            var b = CsField(leaf);
            sb.AppendLine($"    public int {b}FamilyId;");
            sb.AppendLine($"    public float {b}PointSize;");
            sb.AppendLine($"    public float {b}PixelSize;");
            sb.AppendLine($"    public int {b}Weight;");
            sb.AppendLine($"    public int {b}Italic;");
            sb.AppendLine($"    public int {b}Mask;");
            continue;
        }

        sb.AppendLine($"    public {CsAbiType(leaf)} {CsField(leaf)};");
    }

    sb.AppendLine("}");
    return sb.ToString();
}

string EmitStylePaths()
{
    var paths = new List<string>();
    void Walk(Group g, string prefix)
    {
        foreach (var m in g.Members)
        {
            var path = prefix.Length == 0 ? Pascal(m.Name) : prefix + "." + Pascal(m.Name);
            paths.Add(path);
            if (m.Group is not null)
            {
                Walk(m.Group, path);
            }
        }
    }

    Walk(rootGroup, "");
    var sb = new StringBuilder();
    sb.Append(CsHeader());
    sb.AppendLine("using System;");
    sb.AppendLine("using AvaUnion.Engine;");
    sb.AppendLine();
    sb.AppendLine("namespace AvaUnion.Markup;");
    sb.AppendLine();
    sb.AppendLine("/// <summary>Gets values from a <see cref=\"ResolvedStyle\"/> by path.</summary>");
    sb.AppendLine("public static class StylePaths");
    sb.AppendLine("{");
    sb.AppendLine("    /// <summary>All valid paths, for example <c>Text.Color</c> or <c>Layout.Padding</c>.</summary>");
    sb.AppendLine("    public static readonly string[] All =");
    sb.AppendLine("    [");
    foreach (var p in paths)
    {
        sb.AppendLine($"        \"{p}\",");
    }

    sb.AppendLine("    ];");
    sb.AppendLine();
    sb.AppendLine("    /// <summary>Gets the getter for a path. It is null when the path does not exist.</summary>");
    sb.AppendLine("    public static Func<ResolvedStyle, object?>? Get(string path) => path switch");
    sb.AppendLine("    {");
    foreach (var p in paths)
    {
        sb.AppendLine($"        \"{p}\" => static s => s.{p},");
    }

    sb.AppendLine("        _ => null,");
    sb.AppendLine("    };");
    sb.AppendLine("}");
    return sb.ToString();
}

string EmitLeavesJson()
{
    var sb = new StringBuilder();
    sb.AppendLine("{");
    sb.AppendLine($"  \"source\": \"{header}\",");
    sb.AppendLine("  \"leaves\": [");
    for (var i = 0; i < leaves.Count; i++)
    {
        var leaf = leaves[i];
        var enumType = leaf.Kind is Kind.Enum or Kind.Flags ? $"\"{CsEnum(leaf.CppType)}\"" : "null";
        var comma = i + 1 < leaves.Count ? "," : "";
        sb.AppendLine($"    {{ \"index\": {leaf.Index}, \"name\": \"{string.Join('.', leaf.Path)}\", \"kind\": \"{leaf.Kind}\", \"enum\": {enumType} }}{comma}");
    }

    sb.AppendLine("  ]");
    sb.AppendLine("}");
    return sb.ToString();
}

string EmitAvuResolvedH()
{
    var sb = new StringBuilder();
    sb.AppendLine($"/* Generated by tools/union-properties. Do not edit. */");
    sb.AppendLine($"/* {header} */");
    sb.AppendLine("#pragma once");
    sb.AppendLine();
    sb.AppendLine("#include <stdint.h>");
    sb.AppendLine();
    sb.AppendLine($"#define AVU_UNION_SCHEMA \"{ymlHash}\"");
    sb.AppendLine($"#define AVU_LEAF_COUNT {leaves.Count}");
    sb.AppendLine("#define AVU_FONT_FAMILY 1");
    sb.AppendLine("#define AVU_FONT_POINT_SIZE 2");
    sb.AppendLine("#define AVU_FONT_PIXEL_SIZE 4");
    sb.AppendLine("#define AVU_FONT_WEIGHT 8");
    sb.AppendLine("#define AVU_FONT_ITALIC 16");
    sb.AppendLine();
    foreach (var leaf in leaves)
    {
        sb.AppendLine($"#define AVU_LEAF_{string.Join('_', leaf.Path.Select(Snake)).ToUpperInvariant()} {leaf.Index}");
    }

    sb.AppendLine();
    sb.AppendLine("typedef struct avu_resolved {");
    sb.AppendLine("    uint64_t present[2];");
    foreach (var leaf in leaves)
    {
        var f = CField(leaf);
        if (leaf.Kind == Kind.Font)
        {
            sb.AppendLine($"    int32_t {f}_family_id;");
            sb.AppendLine($"    float {f}_point_size;");
            sb.AppendLine($"    float {f}_pixel_size;");
            sb.AppendLine($"    int32_t {f}_weight;");
            sb.AppendLine($"    int32_t {f}_italic;");
            sb.AppendLine($"    int32_t {f}_mask;");
            continue;
        }

        var t = leaf.Kind switch
        {
            Kind.Real => "float",
            Kind.Color => "uint32_t",
            _ => "int32_t",
        };
        sb.AppendLine($"    {t} {f};");
    }

    sb.AppendLine("} avu_resolved;");
    return sb.ToString();
}

string EmitLeavesH()
{
    var sb = new StringBuilder();
    sb.AppendLine("// Generated by tools/union-properties. Do not edit.");
    sb.AppendLine($"// {header}");
    sb.AppendLine("#pragma once");
    sb.AppendLine();
    sb.AppendLine("#include \"properties/StylePropertyGroup.h\"");
    sb.AppendLine();
    sb.AppendLine("namespace avu");
    sb.AppendLine("{");
    sb.AppendLine("template<typename Visitor>");
    sb.AppendLine("void visitLeaves(const Union::Properties::StylePropertyGroup *p, Visitor &&visitor)");
    sb.AppendLine("{");
    sb.AppendLine("    if (!p) {");
    sb.AppendLine("        return;");
    sb.AppendLine("    }");
    EmitVisit(sb, rootGroup, "p", "    ", 0);
    sb.AppendLine("}");
    sb.AppendLine("}");
    return sb.ToString();
}

void EmitVisit(StringBuilder sb, Group g, string var, string indent, int depth)
{
    foreach (var m in g.Members)
    {
        if (m.Group is not null)
        {
            var gv = $"g{depth}_{m.Name}";
            sb.AppendLine($"{indent}if (auto {gv} = {var}->{m.Name}()) {{");
            EmitVisit(sb, m.Group, gv, indent + "    ", depth + 1);
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            var leaf = m.Leaf!;
            sb.AppendLine($"{indent}if (auto v = {var}->{m.Name}(); v.has_value()) {{");
            sb.AppendLine($"{indent}    visitor({leaf.Index}, \"{string.Join('.', leaf.Path)}\", v.value());");
            sb.AppendLine($"{indent}}}");
        }
    }
}

string EmitResolvedCpp()
{
    var sb = new StringBuilder();
    sb.AppendLine("// Generated by tools/union-properties. Do not edit.");
    sb.AppendLine($"// {header}");
    sb.AppendLine();
    sb.AppendLine("#include \"Resolved.h\"");
    sb.AppendLine();
    sb.AppendLine("#include <cstring>");
    sb.AppendLine();
    sb.AppendLine("#include \"Leaves.g.h\"");
    sb.AppendLine();
    sb.AppendLine("namespace avu");
    sb.AppendLine("{");
    sb.AppendLine("void copyResolved(const Union::Properties::StylePropertyGroup *p, avu_resolved *out)");
    sb.AppendLine("{");
    sb.AppendLine("    std::memset(out, 0, sizeof(avu_resolved));");
    sb.AppendLine("    if (!p) {");
    sb.AppendLine("        return;");
    sb.AppendLine("    }");
    EmitCopy(sb, rootGroup, "p", "    ", 0);
    sb.AppendLine("}");
    sb.AppendLine("}");
    return sb.ToString();
}

void EmitCopy(StringBuilder sb, Group g, string var, string indent, int depth)
{
    foreach (var m in g.Members)
    {
        if (m.Group is not null)
        {
            var gv = $"g{depth}_{m.Name}";
            sb.AppendLine($"{indent}if (auto {gv} = {var}->{m.Name}()) {{");
            EmitCopy(sb, m.Group, gv, indent + "    ", depth + 1);
            sb.AppendLine($"{indent}}}");
            continue;
        }

        var leaf = m.Leaf!;
        var f = CField(leaf);
        var assign = leaf.Kind switch
        {
            Kind.Real => $"out->{f} = float(v.value());",
            Kind.Int => $"out->{f} = int32_t(v.value());",
            Kind.Bool => $"out->{f} = v.value() ? 1 : 0;",
            Kind.Enum or Kind.Flags => $"out->{f} = int32_t(v.value());",
            Kind.Color => $"out->{f} = colorToArgb(v.value());",
            Kind.String => $"out->{f} = internString(v.value());",
            Kind.Url => $"out->{f} = internString(v.value().toString());",
            Kind.Path => $"out->{f} = internString(QString::fromStdString(v.value().string()));",
            Kind.Font => $"copyFont(v.value(), &out->{f}_family_id, &out->{f}_point_size, &out->{f}_pixel_size, &out->{f}_weight, &out->{f}_italic, &out->{f}_mask);",
            _ => throw new InvalidOperationException(),
        };
        sb.AppendLine($"{indent}if (auto v = {var}->{m.Name}(); v.has_value()) {{");
        sb.AppendLine($"{indent}    {assign}");
        sb.AppendLine($"{indent}    out->present[{leaf.Index / 64}] |= uint64_t(1) << {leaf.Index % 64};");
        sb.AppendLine($"{indent}}}");
    }
}

string CsHeader() =>
    "// <auto-generated>\n" +
    "// Generated by tools/union-properties from Union's properties.yml. Do not edit.\n" +
    $"// {header}\n" +
    "// </auto-generated>\n\n#nullable enable\n\n";

string CsLeafType(Leaf leaf) => leaf.Kind switch
{
    Kind.Real => "double?",
    Kind.Int => "int?",
    Kind.Bool => "bool?",
    Kind.Enum or Kind.Flags => CsEnum(leaf.CppType) + "?",
    Kind.Color => "Color?",
    Kind.String or Kind.Url or Kind.Path => "string?",
    Kind.Font => "UnionFont?",
    _ => throw new InvalidOperationException(),
};

string CsAbiType(Leaf leaf) => leaf.Kind switch
{
    Kind.Real => "float",
    Kind.Color => "uint",
    _ => "int",
};

static string CsEnum(string cppType) => cppType switch
{
    "Union::Properties::AlignmentContainer" => "UnionAlignmentContainer",
    "Union::Properties::Alignment" => "UnionAlignment",
    "Union::Properties::LineStyle" => "UnionLineStyle",
    "Union::Properties::TextWrapMode" => "UnionTextWrapMode",
    "Union::Properties::TextElide" => "UnionTextElide",
    "Union::Properties::ImageFlags" => "UnionImageFlags",
    _ => throw new InvalidOperationException($"unknown enum {cppType}"),
};

static Kind KindOf(string type) => type switch
{
    "qreal" => Kind.Real,
    "int" => Kind.Int,
    "bool" => Kind.Bool,
    "Union::Properties::ImageFlags" => Kind.Flags,
    "Union::Properties::AlignmentContainer" or "Union::Properties::Alignment" or "Union::Properties::LineStyle"
        or "Union::Properties::TextWrapMode" or "Union::Properties::TextElide" => Kind.Enum,
    "Union::Color" => Kind.Color,
    "QString" => Kind.String,
    "QUrl" => Kind.Url,
    "std::filesystem::path" => Kind.Path,
    "QFont" => Kind.Font,
    _ => throw new InvalidOperationException($"unknown leaf type {type}"),
};

static string CppGroupType(string name) => Pascal(name) + "PropertyGroup";

static string Pascal(string s) => char.ToUpperInvariant(s[0]) + s[1..];

static string Snake(string s)
{
    var sb = new StringBuilder();
    foreach (var c in s)
    {
        if (char.IsUpper(c))
        {
            sb.Append('_').Append(char.ToLowerInvariant(c));
        }
        else
        {
            sb.Append(c);
        }
    }

    return sb.ToString();
}

static string CField(Leaf leaf) => string.Join('_', leaf.Path.Select(Snake));

static string CsField(Leaf leaf) => string.Concat(leaf.Path.Select(Pascal));

static string? Str(YamlMappingNode m, string key) =>
    m.Children.TryGetValue(new YamlScalarNode(key), out var v) ? ((YamlScalarNode)v).Value : null;

static string ReadTag(string cmake)
{
    foreach (var line in File.ReadAllLines(cmake))
    {
        var t = line.Trim();
        if (t.StartsWith("set(PROJECT_VERSION ", StringComparison.Ordinal))
        {
            return "v" + t.Split('"')[1];
        }
    }

    throw new InvalidOperationException("PROJECT_VERSION not found in " + cmake);
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

enum Kind { Real, Int, Bool, Enum, Flags, Color, String, Url, Path, Font }

record Leaf(int Index, string[] Path, string CppType, Kind Kind);

record Member(string Name, Group? Group, Leaf? Leaf);

record Group(string CsType, string CppType, string? TypeDef, List<Member> Members);
