using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AvaUnion.BreezeIcons;

internal sealed class BreezeArchive
{
    private const string Resource = "breeze-icons.bin";

    private readonly long[] _chunkStarts;
    private readonly int[] _chunkLengths;
    private readonly (int Chunk, int Offset, int Length)[] _blobs;
    private int _cachedChunk = -1;
    private byte[] _cachedData = [];

    private BreezeArchive(long[] chunkStarts, int[] chunkLengths, (int, int, int)[] blobs, Dictionary<string, SortedDictionary<int, (int Blob, bool Pad)>> icons)
    {
        _chunkStarts = chunkStarts;
        _chunkLengths = chunkLengths;
        _blobs = blobs;
        Icons = icons;
    }

    public Dictionary<string, SortedDictionary<int, (int Blob, bool Pad)>> Icons { get; }

    public static BreezeArchive Load()
    {
        using var stream = Open();
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        if (!reader.ReadBytes(4).AsSpan().SequenceEqual("BZI1"u8))
        {
            throw new InvalidDataException("not a Breeze icon archive");
        }

        var indexLength = reader.ReadInt32();
        using var index = new BinaryReader(new MemoryStream(Decompress(reader.ReadBytes(indexLength))), Encoding.UTF8);
        var dataStart = 8L + indexLength;
        var chunkLengths = new int[index.ReadInt32()];
        var chunkStarts = new long[chunkLengths.Length];
        for (var i = 0; i < chunkLengths.Length; i++)
        {
            chunkLengths[i] = index.ReadInt32();
            chunkStarts[i] = dataStart;
            dataStart += chunkLengths[i];
        }

        var blobs = new (int, int, int)[index.ReadInt32()];
        for (var i = 0; i < blobs.Length; i++)
        {
            blobs[i] = (index.ReadInt32(), index.ReadInt32(), index.ReadInt32());
        }

        var icons = new Dictionary<string, SortedDictionary<int, (int Blob, bool Pad)>>(StringComparer.Ordinal);
        var count = index.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var name = index.ReadString();
            var size = index.ReadInt32();
            var blob = index.ReadInt32();
            var pad = index.ReadBoolean();
            if (!icons.TryGetValue(name, out var sizes))
            {
                icons[name] = sizes = [];
            }

            sizes[size] = (blob, pad);
        }

        return new BreezeArchive(chunkStarts, chunkLengths, blobs, icons);
    }

    public Stream Read(int blob)
    {
        var (chunk, offset, length) = _blobs[blob];
        if (chunk != _cachedChunk)
        {
            using var stream = Open();
            stream.Position = _chunkStarts[chunk];
            var compressed = new byte[_chunkLengths[chunk]];
            stream.ReadExactly(compressed);
            _cachedData = Decompress(compressed);
            _cachedChunk = chunk;
        }

        return new MemoryStream(_cachedData, offset, length, writable: false);
    }

    private static Stream Open() =>
        typeof(BreezeArchive).Assembly.GetManifestResourceStream(Resource) ?? throw new InvalidDataException("the Breeze icon archive is missing");

    private static byte[] Decompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(new MemoryStream(data), CompressionMode.Decompress))
        {
            brotli.CopyTo(output);
        }

        return output.ToArray();
    }
}
