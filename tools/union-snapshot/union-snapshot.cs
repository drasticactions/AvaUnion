#:property ImplicitUsings=enable

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var check = args.Contains("--check");
var exportPath = Option("--export");
var outPath = Option("--out");
if ((exportPath is null) != (outPath is null))
{
    Console.Error.WriteLine("usage: union-snapshot.cs [--check] | --export <style.json> --out <file.union.bin> [--check]");
    return 2;
}

var root = FindRoot();
var dataDir = Path.Combine(root, "tests/Data/snapshot");
var output = outPath is null ? Path.Combine(root, "src/AvaUnion.Theme/Snapshot/breeze.union.bin") : Path.GetFullPath(outPath);
var outputName = outPath ?? "src/AvaUnion.Theme/Snapshot/breeze.union.bin";

using var leavesDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tools/union-properties/leaves.g.json")));
var source = leavesDoc.RootElement.GetProperty("source").GetString()!;
var schema = Regex.Match(source, "sha256=([0-9a-f]+)").Groups[1].Value;
var leafIndex = new Dictionary<string, (int Index, string Kind)>();
foreach (var leaf in leavesDoc.RootElement.GetProperty("leaves").EnumerateArray())
{
    leafIndex[leaf.GetProperty("name").GetString()!] = (leaf.GetProperty("index").GetInt32(), leaf.GetProperty("kind").GetString()!);
}

var strings = new List<string> { string.Empty };
var stringIds = new Dictionary<string, int>(StringComparer.Ordinal) { [string.Empty] = 0 };
int Str(string value)
{
    if (!stringIds.TryGetValue(value, out var id))
    {
        id = strings.Count;
        strings.Add(value);
        stringIds[value] = id;
    }

    return id;
}

var body = new Buffer();
string? unionVersion = null;

var exports = new List<(string Id, JsonDocument Doc)>();
if (exportPath is not null)
{
    var doc = JsonDocument.Parse(File.ReadAllText(exportPath));
    var style = doc.RootElement;
    var id = style.GetProperty("style").GetString()!;
    var problems = new List<string>();
    var exportSchema = style.TryGetProperty("schema", out var s) ? s.GetString() : null;
    if (exportSchema != schema)
    {
        problems.Add($"exported with property schema {exportSchema ?? "(none)"}, this checkout has {schema}; use the AvaUnion ref that matches the Union the export was made with");
    }

    if (style.GetProperty("hasErrors").GetBoolean())
    {
        problems.Add("the CSS has errors; union-export logs them");
    }

    if (style.TryGetProperty("outside", out var outside))
    {
        foreach (var entry in outside.EnumerateArray())
        {
            problems.Add($"refers to {entry.GetString()}, which is outside the package; a package must be self-contained");
        }
    }

    if (style.TryGetProperty("sharedVariables", out var shared))
    {
        foreach (var entry in shared.EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString();
            var others = string.Join(", ", entry.GetProperty("styles").EnumerateArray().Select(e => e.GetString()));
            problems.Add($"defines {name}, which {others} also defines. Union uses the first definition of a variable in a process. Prefix the variable with the style id, for example --{id}-{name!.TrimStart('-')}.");
        }
    }

    if (problems.Count > 0)
    {
        foreach (var problem in problems)
        {
            Console.Error.WriteLine($"{exportPath}: style {id} {problem}");
        }

        return 1;
    }

    exports.Add((id, doc));
}
else
{
    foreach (var id in new[] { "breeze", "breeze-rtl", "breeze-mobile" })
    {
        exports.Add((id, JsonDocument.Parse(File.ReadAllText(Path.Combine(dataDir, id + ".json")))));
    }
}

body.U32((uint)exports.Count);
var images = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
foreach (var (id, doc) in exports)
{
    var style = doc.RootElement;
    unionVersion ??= style.GetProperty("union").GetString();
    if (style.GetProperty("hasErrors").GetBoolean())
    {
        throw new InvalidOperationException($"{id} was exported with parse errors");
    }

    if (style.TryGetProperty("schema", out var styleSchema) && styleSchema.GetString() != schema)
    {
        throw new InvalidOperationException($"{id} was exported with property schema {styleSchema.GetString()}, expected {schema}");
    }

    foreach (var image in style.GetProperty("images").EnumerateObject())
    {
        images[image.Name] = image.Value;
    }

    var rules = style.GetProperty("rules");
    body.U32(Str(id));
    body.U32(Str(style.TryGetProperty("metadata", out var metadata) ? CompactMetadata(metadata) : string.Empty));
    body.U32((uint)rules.GetArrayLength());
    foreach (var rule in rules.EnumerateArray())
    {
        var selectors = rule.GetProperty("selectors");
        body.U16((ushort)selectors.GetArrayLength());
        foreach (var selector in selectors.EnumerateArray())
        {
            var kind = selector.GetProperty("kind").GetString();
            switch (kind)
            {
                case "Empty": body.U8(0); break;
                case "Type": body.U8(1); body.U32(Str(selector.GetProperty("value").GetString()!)); break;
                case "Id": body.U8(2); body.U32(Str(selector.GetProperty("value").GetString()!)); break;
                case "State": body.U8(3); body.U32((uint)selector.GetProperty("state").GetInt32()); break;
                case "Hint": body.U8(4); body.U32(Str(selector.GetProperty("value").GetString()!)); break;
                case "AttributeExists": body.U8(5); body.U32(Str(selector.GetProperty("value").GetString()!)); break;
                case "AttributeEquals":
                    body.U8(6);
                    body.U32(Str(selector.GetProperty("name").GetString()!));
                    if (selector.GetProperty("valueType").GetString() == "number")
                    {
                        body.U8(1);
                        body.F64(selector.GetProperty("value").GetDouble());
                    }
                    else
                    {
                        body.U8(0);
                        body.U32(Str(selector.GetProperty("value").GetString()!));
                    }

                    break;
                case "AttributeSubstringMatch":
                    body.U8(7);
                    body.U32(Str(selector.GetProperty("name").GetString()!));
                    body.U32(Str(selector.GetProperty("value").GetString()!));
                    break;
                case "AnyElement": body.U8(8); break;
                case "ChildCombinator": body.U8(9); break;
                case "DescendantCombinator": body.U8(10); break;
                default: throw new InvalidOperationException($"unknown selector kind {kind}");
            }
        }

        body.I32(rule.GetProperty("weight").GetInt32());
        var properties = rule.GetProperty("properties").EnumerateObject().ToList();
        body.U16((ushort)properties.Count);
        foreach (var property in properties)
        {
            if (!leafIndex.TryGetValue(property.Name, out var leaf))
            {
                throw new InvalidOperationException($"unknown leaf {property.Name}; regenerate with union-properties");
            }

            body.U8((byte)leaf.Index);
            WriteValue(body, leaf.Kind, property.Value);
        }
    }
}

string[] schemeNames = exportPath is null ? ["BreezeLight", "BreezeDark"] : [];
body.U32((uint)schemeNames.Length);
foreach (var name in schemeNames)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataDir, name + ".json")));
    var colors = doc.RootElement.GetProperty("colors");
    var table = new uint[colors.GetArrayLength()];
    var groups = new[] { "active", "inactive", "disabled" };
    var sets = new[] { "view", "window", "button", "selection", "tooltip", "complementary", "header" };
    var kinds = new[] { "background", "foreground", "decoration" };
    var roles = new[]
    {
        new[] { "normal", "alternate", "active", "link", "visited", "negative", "neutral", "positive" },
        new[] { "text", "inactive", "active", "link", "visited", "negative", "neutral", "positive" },
        new[] { "focus", "hover", "frame", "framecontrast" },
    };
    foreach (var color in colors.EnumerateArray())
    {
        var parts = color.GetProperty("key").GetString()!.Split('.');
        var kind = Array.IndexOf(kinds, parts[2]);
        var index = (Array.IndexOf(groups, parts[0]) * 7 + Array.IndexOf(sets, parts[1])) * 20 + kind * 8 + Array.IndexOf(roles[kind], parts[3]);
        table[index] = uint.Parse(color.GetProperty("argb").GetString()!.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    body.U32(Str(name));
    body.U32((uint)table.Length);
    foreach (var argb in table)
    {
        body.U32(argb);
    }
}

body.U32((uint)images.Count);
foreach (var (path, image) in images)
{
    body.U32(Str(path));
    var box = image.GetProperty("viewBox").EnumerateArray().Select(v => (float)v.GetDouble()).ToArray();
    foreach (var v in box)
    {
        body.F32(v);
    }

    var shapes = image.GetProperty("shapes");
    body.U16((ushort)shapes.GetArrayLength());
    foreach (var shape in shapes.EnumerateArray())
    {
        var kind = shape.GetProperty("kind").GetString() switch
        {
            "path" => 0,
            "circle" => 1,
            _ => 2,
        };
        body.U8((byte)kind);
        body.U8((byte)((shape.GetProperty("fill").GetBoolean() ? 1 : 0) | (shape.GetProperty("stroke").GetBoolean() ? 2 : 0)));
        body.F32(shape.TryGetProperty("strokeWidth", out var sw) ? (float)sw.GetDouble() : 1f);
        body.U8(shape.GetProperty("lineCap").GetString() switch { "round" => 1, "square" => 2, _ => 0 });
        body.U8(shape.GetProperty("lineJoin").GetString() switch { "round" => 1, "bevel" => 2, _ => 0 });
        switch (kind)
        {
            case 0:
                body.U32(Str(shape.GetProperty("d").GetString()!));
                break;
            case 1:
                body.F32((float)shape.GetProperty("cx").GetDouble());
                body.F32((float)shape.GetProperty("cy").GetDouble());
                body.F32((float)shape.GetProperty("r").GetDouble());
                break;
            default:
                foreach (var k in new[] { "x", "y", "width", "height" })
                {
                    body.F32(shape.TryGetProperty(k, out var v) ? (float)v.GetDouble() : 0f);
                }

                break;
        }
    }
}

(string Family, float Points)[] fonts = exportPath is null ? [("Noto Sans", 10f), ("$Default", 10f)] : [];
body.U32((uint)fonts.Length);
foreach (var (family, points) in fonts)
{
    body.U32(Str(family));
    body.F32(points);
}

var file = new Buffer();
file.Raw("AVUSNAP1"u8.ToArray());
file.U32(1);
file.U32((uint)leafIndex.Count);
file.InlineString(unionVersion ?? "unknown");
file.InlineString(source);
file.InlineString(schema);
file.U32((uint)strings.Count);
foreach (var s in strings)
{
    file.InlineString(s);
}

file.Raw(body.ToArray());
var bytes = file.ToArray();

foreach (var (_, doc) in exports)
{
    doc.Dispose();
}

var existing = File.Exists(output) ? File.ReadAllBytes(output) : null;
if (existing is not null && existing.AsSpan().SequenceEqual(bytes))
{
    Console.WriteLine($"up to date: {bytes.Length} bytes");
    return 0;
}

if (check)
{
    Console.Error.WriteLine($"stale: {outputName}");
    return 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllBytes(output, bytes);
Console.WriteLine($"wrote {output}: {bytes.Length} bytes, {strings.Count} strings");
return 0;

void WriteValue(Buffer b, string kind, JsonElement value)
{
    switch (kind)
    {
        case "Real": b.F64(value.GetDouble()); break;
        case "Int": b.I32(value.GetInt32()); break;
        case "Bool": b.U8(value.GetBoolean() ? (byte)1 : (byte)0); break;
        case "Enum":
        case "Flags": b.I32(value.GetInt32()); break;
        case "Color": WriteColor(b, value); break;
        case "String":
        case "Url":
        case "Path": b.U32(Str(value.GetString()!)); break;
        case "Font":
        {
            byte mask = 0;
            if (value.TryGetProperty("family", out _)) mask |= 1;
            if (value.TryGetProperty("pointSize", out _)) mask |= 2;
            if (value.TryGetProperty("pixelSize", out _)) mask |= 4;
            if (value.TryGetProperty("weight", out _)) mask |= 8;
            if (value.TryGetProperty("italic", out _)) mask |= 16;
            b.U8(mask);
            if ((mask & 1) != 0) b.U32(Str(value.GetProperty("family").GetString()!));
            if ((mask & 2) != 0) b.F64(value.GetProperty("pointSize").GetDouble());
            if ((mask & 4) != 0) b.F64(value.GetProperty("pixelSize").GetDouble());
            if ((mask & 8) != 0) b.I32(value.GetProperty("weight").GetInt32());
            if ((mask & 16) != 0) b.U8(value.GetProperty("italic").GetBoolean() ? (byte)1 : (byte)0);
            break;
        }

        default: throw new InvalidOperationException($"unknown leaf kind {kind}");
    }
}

void WriteColor(Buffer b, JsonElement c)
{
    switch (c.GetProperty("type").GetString())
    {
        case "empty": b.U8(0); break;
        case "rgba":
            b.U8(1);
            foreach (var k in new[] { "r", "g", "b", "a" })
            {
                b.U8((byte)c.GetProperty(k).GetInt32());
            }

            break;
        case "custom":
        {
            b.U8(2);
            b.U32(Str(c.GetProperty("source").GetString()!));
            var args = c.GetProperty("args");
            b.U8((byte)args.GetArrayLength());
            foreach (var a in args.EnumerateArray())
            {
                b.U32(Str(a.GetString()!));
            }

            break;
        }

        case "add": b.U8(3); WriteColor(b, c.GetProperty("a")); WriteColor(b, c.GetProperty("b")); break;
        case "subtract": b.U8(4); WriteColor(b, c.GetProperty("a")); WriteColor(b, c.GetProperty("b")); break;
        case "multiply": b.U8(5); WriteColor(b, c.GetProperty("a")); WriteColor(b, c.GetProperty("b")); break;
        case "set":
        {
            b.U8(6);
            WriteColor(b, c.GetProperty("color"));
            byte mask = 0;
            var values = new byte[4];
            var channels = new[] { "r", "g", "b", "a" };
            for (var i = 0; i < 4; i++)
            {
                var v = c.GetProperty(channels[i]);
                if (v.ValueKind == JsonValueKind.Number)
                {
                    mask |= (byte)(1 << i);
                    values[i] = (byte)v.GetInt32();
                }
            }

            b.U8(mask);
            b.Raw(values);
            break;
        }

        case "mix": b.U8(7); WriteColor(b, c.GetProperty("a")); WriteColor(b, c.GetProperty("b")); b.F64(c.GetProperty("amount").GetDouble()); break;
        default: throw new InvalidOperationException($"unknown color type {c.GetProperty("type")}");
    }
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string CompactMetadata(JsonElement metadata)
{
    if (metadata.ValueKind != JsonValueKind.Object || !metadata.EnumerateObject().Any())
    {
        return string.Empty;
    }

    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
    {
        metadata.WriteTo(writer);
    }

    return Encoding.UTF8.GetString(stream.ToArray());
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

sealed class Buffer
{
    private readonly MemoryStream _stream = new();
    private readonly byte[] _scratch = new byte[8];

    public void Raw(byte[] data) => _stream.Write(data);

    public void U8(byte v) => _stream.WriteByte(v);

    public void U8(int v) => _stream.WriteByte((byte)v);

    public void U16(ushort v)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_scratch, v);
        _stream.Write(_scratch, 0, 2);
    }

    public void U32(uint v)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_scratch, v);
        _stream.Write(_scratch, 0, 4);
    }

    public void U32(int v) => U32((uint)v);

    public void I32(int v)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_scratch, v);
        _stream.Write(_scratch, 0, 4);
    }

    public void F32(float v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_scratch, v);
        _stream.Write(_scratch, 0, 4);
    }

    public void F64(double v)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(_scratch, v);
        _stream.Write(_scratch, 0, 8);
    }

    public void InlineString(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        U32((uint)bytes.Length);
        Raw(bytes);
    }

    public byte[] ToArray() => _stream.ToArray();
}
