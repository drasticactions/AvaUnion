#:property ImplicitUsings=enable

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

var source = Option("--source");
var version = Option("--version") ?? "unknown";
var check = args.Contains("--check");
if (source is null)
{
    Console.Error.WriteLine("usage: breeze-icons.cs --source <breeze-icons checkout> [--version <text>] [--check]");
    return 2;
}

var root = FindRoot();
var project = Path.Combine(root, "src/AvaUnion.BreezeIcons");
var output = Path.Combine(project, "Icons/breeze-icons.bin");
const int ChunkSize = 64 * 1024;

var entries = new SortedDictionary<(string Name, int Size), string>();
foreach (var file in Directory.EnumerateFiles(Path.Combine(source, "icons"), "*.svg", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
{
    var size = Path.GetFileName(Path.GetDirectoryName(file)!);
    if (int.TryParse(size, out var pixels))
    {
        entries.TryAdd((Path.GetFileNameWithoutExtension(file), pixels), file);
    }
}

var missing = File.ReadAllLines(Path.Combine(project, "icons.txt"))
    .Where(l => l.Length > 0)
    .Where(name => !entries.Keys.Any(k => k.Name == name))
    .ToList();
foreach (var name in missing)
{
    Console.Error.WriteLine($"missing: {name}");
}

var blobs = new List<byte[]>();
var blobIds = new Dictionary<string, int>(StringComparer.Ordinal);
var entryBlobs = new List<(string Name, int Size, int Blob, bool Pad)>();
foreach (var path in entries.OrderBy(e => e.Value, StringComparer.Ordinal))
{
    var data = File.ReadAllBytes(path.Value);
    var hash = Convert.ToHexString(SHA256.HashData(data));
    if (!blobIds.TryGetValue(hash, out var id))
    {
        blobIds[hash] = id = blobs.Count;
        blobs.Add(data);
    }

    entryBlobs.Add((path.Key.Name, path.Key.Size, id, false));
    if (path.Key.Size == 22 && !entries.ContainsKey((path.Key.Name, 24)) && Is22(path.Value))
    {
        entryBlobs.Add((path.Key.Name, 24, id, true));
    }
}

var chunks = new List<byte[]>();
var places = new (int Chunk, int Offset)[blobs.Count];
var current = new MemoryStream();
for (var i = 0; i < blobs.Count; i++)
{
    if (current.Length > 0 && current.Length + blobs[i].Length > ChunkSize)
    {
        chunks.Add(Compress(current.ToArray()));
        current = new MemoryStream();
    }

    places[i] = (chunks.Count, (int)current.Length);
    current.Write(blobs[i]);
}

if (current.Length > 0)
{
    chunks.Add(Compress(current.ToArray()));
}

var index = new MemoryStream();
using (var writer = new BinaryWriter(index, Encoding.UTF8, leaveOpen: true))
{
    writer.Write(chunks.Count);
    foreach (var chunk in chunks)
    {
        writer.Write(chunk.Length);
    }

    writer.Write(blobs.Count);
    for (var i = 0; i < blobs.Count; i++)
    {
        writer.Write(places[i].Chunk);
        writer.Write(places[i].Offset);
        writer.Write(blobs[i].Length);
    }

    writer.Write(entryBlobs.Count);
    foreach (var (name, size, blob, pad) in entryBlobs.OrderBy(e => e.Name, StringComparer.Ordinal).ThenBy(e => e.Size))
    {
        writer.Write(name);
        writer.Write(size);
        writer.Write(blob);
        writer.Write(pad);
    }
}

var archive = new MemoryStream();
using (var writer = new BinaryWriter(archive, Encoding.UTF8, leaveOpen: true))
{
    var compressedIndex = Compress(index.ToArray());
    writer.Write("BZI1"u8);
    writer.Write(compressedIndex.Length);
    writer.Write(compressedIndex);
    foreach (var chunk in chunks)
    {
        writer.Write(chunk);
    }
}

var bytes = archive.ToArray();
var versionText = version + "\n";
var license = File.ReadAllBytes(Path.Combine(source, "COPYING-ICONS"));
Console.WriteLine($"{entryBlobs.Count} icons, {entries.Keys.Select(k => k.Name).Distinct().Count()} names, {blobs.Count} unique files, {blobs.Sum(b => b.Length)} bytes, archive {bytes.Length} bytes");
if (check)
{
    var same = File.Exists(output) && File.ReadAllBytes(output).AsSpan().SequenceEqual(bytes)
        && File.ReadAllText(Path.Combine(project, "Icons/VERSION")) == versionText;
    Console.WriteLine(same ? "up to date" : "out of date");
    return same && missing.Count == 0 ? 0 : 1;
}

Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllBytes(output, bytes);
File.WriteAllText(Path.Combine(project, "Icons/VERSION"), versionText);
File.WriteAllBytes(Path.Combine(project, "Icons/COPYING-ICONS"), license);
return missing.Count == 0 ? 0 : 1;

static bool Is22(string file)
{
    using var reader = XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
    if (!reader.ReadToFollowing("svg", "http://www.w3.org/2000/svg"))
    {
        return false;
    }

    static double? Number(string? text) =>
        text is null ? null : double.TryParse(text.Replace("px", string.Empty, StringComparison.Ordinal).Trim(), System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

    var viewBox = reader.GetAttribute("viewBox");
    var width = Number(reader.GetAttribute("width"));
    var height = Number(reader.GetAttribute("height"));
    var viewBoxOk = viewBox is null || viewBox.Replace("px", string.Empty, StringComparison.Ordinal)
        .Split([',', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
        .Select(p => double.TryParse(p, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : double.NaN)
        .SequenceEqual([0.0, 0.0, 22.0, 22.0]);
    var sizeOk = (width is null && height is null) || (width == 22 && height == 22);
    return viewBoxOk && sizeOk && !(viewBox is null && (width is null || height is null));
}

static byte[] Compress(byte[] data)
{
    using var encoder = new BrotliEncoder(11, 22);
    var buffer = new byte[BrotliEncoder.GetMaxCompressedLength(data.Length)];
    if (encoder.Compress(data, buffer, out _, out var written, isFinalBlock: true) != System.Buffers.OperationStatus.Done)
    {
        throw new InvalidOperationException("compression failed");
    }

    return buffer[..written];
}

string? Option(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
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
