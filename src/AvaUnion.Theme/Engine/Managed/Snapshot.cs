using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>
/// A snapshot of Union styles. It holds style rules and style images. The
/// Breeze snapshot also holds the Breeze Light and Breeze Dark color schemes
/// and the fallback fonts.
/// </summary>
public sealed class Snapshot
{
    /// <summary>The format version that this reader can read.</summary>
    public const int FormatVersion = 1;

    /// <summary>The file magic.</summary>
    public static ReadOnlySpan<byte> Magic => "AVUSNAP1"u8;

    private Snapshot()
    {
    }

    /// <summary>The Union version of the export.</summary>
    public string UnionVersion { get; private set; } = string.Empty;

    /// <summary>The generator source line.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>The SHA-256 of the Union property schema that the snapshot was made with.</summary>
    public string Schema { get; private set; } = string.Empty;

    /// <summary>The text of each style's <c>metadata.json</c> by style id. It is empty when the package has none.</summary>
    public IReadOnlyDictionary<string, string> StyleMetadata { get; private set; } = new Dictionary<string, string>();

    /// <summary>Styles by id.</summary>
    public IReadOnlyDictionary<string, ManagedStyle> Styles { get; private set; } = new Dictionary<string, ManagedStyle>();

    /// <summary>Color schemes by name.</summary>
    public IReadOnlyDictionary<string, SchemeTable> Schemes { get; private set; } = new Dictionary<string, SchemeTable>();

    /// <summary>Style images by package-relative path.</summary>
    public IReadOnlyDictionary<string, UnionImageGeometry> Images { get; private set; } = new Dictionary<string, UnionImageGeometry>();

    /// <summary>Fallback fonts as (family, point size), the preferred font first.</summary>
    public IReadOnlyList<(string Family, double PointSize)> Fonts { get; private set; } = [];

    /// <summary>The snapshot embedded in this assembly.</summary>
    public static Snapshot LoadEmbedded()
    {
        using var stream = typeof(Snapshot).Assembly.GetManifestResourceStream("AvaUnion.Snapshot.breeze.union.bin")
            ?? throw new InvalidOperationException("The Breeze snapshot is not embedded");
        return Load(stream);
    }

    /// <summary>Reads and parses a snapshot.</summary>
    /// <exception cref="InvalidDataException">The data is not a snapshot, or it was made with another property schema.</exception>
    public static Snapshot Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Load(memory.ToArray());
    }

    /// <summary>Parses a snapshot.</summary>
    /// <exception cref="InvalidDataException">The data is not a snapshot, or it was made with another property schema.</exception>
    public static Snapshot Load(byte[] data)
    {
        try
        {
            return Parse(data);
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException)
        {
            throw new InvalidDataException("The snapshot is truncated", e);
        }
    }

    private static Snapshot Parse(byte[] data)
    {
        if (data.Length < 8)
        {
            throw new InvalidDataException("Not a Union snapshot");
        }

        var r = new Reader(data);
        if (!r.Bytes(8).SequenceEqual(Magic))
        {
            throw new InvalidDataException("Not a Union snapshot");
        }

        var format = r.U32();
        if (format != FormatVersion)
        {
            throw new InvalidDataException($"Snapshot format {format} is not supported");
        }

        var leafCount = (int)r.U32();
        if (leafCount != UnionLeafInfo.Count)
        {
            throw new InvalidDataException($"Snapshot has {leafCount} leaves; this build has {UnionLeafInfo.Count}");
        }

        var snapshot = new Snapshot
        {
            UnionVersion = r.InlineString(),
            Source = r.InlineString(),
            Schema = r.InlineString(),
        };
        if (snapshot.Schema != UnionLeafInfo.Schema)
        {
            throw new InvalidDataException($"Snapshot has property schema {snapshot.Schema}; this build has {UnionLeafInfo.Schema}");
        }

        var stringCount = (int)r.U32();
        var strings = new string[stringCount];
        for (var i = 0; i < stringCount; i++)
        {
            strings[i] = r.InlineString();
        }

        r.Strings = strings;

        var styles = new Dictionary<string, ManagedStyle>(StringComparer.Ordinal);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        var styleCount = r.U32();
        for (var s = 0; s < styleCount; s++)
        {
            var id = r.Str();
            metadata[id] = r.Str();
            var ruleCount = (int)r.U32();
            var rules = new ManagedRule[ruleCount];
            for (var i = 0; i < ruleCount; i++)
            {
                rules[i] = ReadRule(ref r);
            }

            styles[id] = new ManagedStyle(id, rules);
        }

        var schemes = new Dictionary<string, SchemeTable>(StringComparer.OrdinalIgnoreCase);
        var schemeCount = r.U32();
        for (var s = 0; s < schemeCount; s++)
        {
            var name = r.Str();
            var count = (int)r.U32();
            var colors = new Color[count];
            for (var i = 0; i < count; i++)
            {
                colors[i] = Color.FromUInt32(r.U32());
            }

            schemes[name] = new SchemeTable(name, colors);
        }

        var images = new Dictionary<string, UnionImageGeometry>(StringComparer.Ordinal);
        var imageCount = r.U32();
        for (var i = 0; i < imageCount; i++)
        {
            var path = r.Str();
            var box = new Rect(r.F32(), r.F32(), r.F32(), r.F32());
            var shapeCount = r.U16();
            var shapes = new UnionImageShape[shapeCount];
            for (var k = 0; k < shapeCount; k++)
            {
                var kind = r.U8();
                var flags = r.U8();
                var strokeWidth = r.F32();
                var cap = r.U8();
                var join = r.U8();
                string? pathData = null;
                double x = 0, y = 0, w = 0, h = 0;
                switch (kind)
                {
                    case 0:
                        pathData = r.Str();
                        break;
                    case 1:
                        x = r.F32();
                        y = r.F32();
                        w = r.F32();
                        break;
                    default:
                        x = r.F32();
                        y = r.F32();
                        w = r.F32();
                        h = r.F32();
                        break;
                }

                shapes[k] = new UnionImageShape(kind, (flags & 1) != 0, (flags & 2) != 0, strokeWidth, cap, join, pathData, x, y, w, h);
            }

            images[path] = new UnionImageGeometry(box, shapes);
        }

        var fonts = new List<(string, double)>();
        var fontCount = r.U32();
        for (var i = 0; i < fontCount; i++)
        {
            fonts.Add((r.Str(), r.F32()));
        }

        snapshot.Styles = styles;
        snapshot.StyleMetadata = metadata;
        snapshot.Schemes = schemes;
        snapshot.Images = images;
        snapshot.Fonts = fonts;
        return snapshot;
    }

    private static ManagedRule ReadRule(ref Reader r)
    {
        var selectorCount = r.U16();
        var selectors = new Selector[selectorCount];
        for (var i = 0; i < selectorCount; i++)
        {
            var kind = (SelectorKind)r.U8();
            selectors[i] = kind switch
            {
                SelectorKind.Type or SelectorKind.Id or SelectorKind.Hint or SelectorKind.AttributeExists => new Selector(kind, r.Str()),
                SelectorKind.State => new Selector(kind, state: (UnionStates)r.U32()),
                SelectorKind.AttributeEquals => ReadAttributeEquals(ref r),
                SelectorKind.AttributeSubstringMatch => new Selector(kind, r.Str(), attributeValue: r.Str()),
                _ => new Selector(kind),
            };
        }

        var weight = r.I32();
        var leafCount = r.U16();
        var leaves = new (int, object)[leafCount];
        for (var i = 0; i < leafCount; i++)
        {
            var leaf = r.U8();
            leaves[i] = (leaf, ReadValue(ref r, leaf));
        }

        return new ManagedRule(selectors, weight, leaves);
    }

    private static Selector ReadAttributeEquals(ref Reader r)
    {
        var name = r.Str();
        object value = r.U8() == 1 ? r.F64() : r.Str();
        return new Selector(SelectorKind.AttributeEquals, name, attributeValue: value);
    }

    private static object ReadValue(ref Reader r, int leaf)
    {
        switch (UnionLeafInfo.Kinds[leaf])
        {
            case UnionLeafKind.Real:
                return r.F64();
            case UnionLeafKind.Int:
                return r.I32();
            case UnionLeafKind.Bool:
                return r.U8() != 0;
            case UnionLeafKind.Enum:
            case UnionLeafKind.Flags:
                return BoxEnum(UnionLeafInfo.EnumTypes[leaf]!, r.I32());
            case UnionLeafKind.Color:
                return ReadColor(ref r);
            case UnionLeafKind.String:
            case UnionLeafKind.Url:
            case UnionLeafKind.Path:
                return r.Str();
            case UnionLeafKind.Font:
            {
                var mask = r.U8();
                string? family = (mask & 1) != 0 ? r.Str() : null;
                double? point = (mask & 2) != 0 ? r.F64() : null;
                double? pixel = (mask & 4) != 0 ? r.F64() : null;
                int? weight = (mask & 8) != 0 ? r.I32() : null;
                bool? italic = (mask & 16) != 0 ? r.U8() != 0 : null;
                return new UnionFont(family, point, pixel, weight, italic);
            }

            default:
                throw new InvalidDataException($"Unknown leaf kind for leaf {leaf}");
        }
    }

    /// <summary>Boxes an enum value as the managed enum type of a leaf.</summary>
    public static object BoxEnum(string enumType, int value) => enumType switch
    {
        nameof(UnionAlignmentContainer) => (UnionAlignmentContainer)value,
        nameof(UnionAlignment) => (UnionAlignment)value,
        nameof(UnionLineStyle) => (UnionLineStyle)value,
        nameof(UnionTextWrapMode) => (UnionTextWrapMode)value,
        nameof(UnionTextElide) => (UnionTextElide)value,
        nameof(UnionImageFlags) => (UnionImageFlags)value,
        _ => throw new InvalidDataException($"Unknown enum {enumType}"),
    };

    private static UnionColor ReadColor(ref Reader r)
    {
        var type = r.U8();
        switch (type)
        {
            case 0:
                return UnionColor.Empty;
            case 1:
                return UnionColor.Rgba(r.U8(), r.U8(), r.U8(), r.U8());
            case 2:
            {
                var source = r.Str();
                var count = r.U8();
                var args = new string[count];
                for (var i = 0; i < count; i++)
                {
                    args[i] = r.Str();
                }

                return UnionColor.Custom(source, args);
            }

            case 3:
                return UnionColor.Add(ReadColor(ref r), ReadColor(ref r));
            case 4:
                return UnionColor.Subtract(ReadColor(ref r), ReadColor(ref r));
            case 5:
                return UnionColor.Multiply(ReadColor(ref r), ReadColor(ref r));
            case 6:
            {
                var color = ReadColor(ref r);
                var mask = r.U8();
                var values = r.Bytes(4);
                return UnionColor.Set(
                    color,
                    (mask & 1) != 0 ? values[0] : null,
                    (mask & 2) != 0 ? values[1] : null,
                    (mask & 4) != 0 ? values[2] : null,
                    (mask & 8) != 0 ? values[3] : null);
            }

            case 7:
                return UnionColor.Mix(ReadColor(ref r), ReadColor(ref r), r.F64());
            default:
                throw new InvalidDataException($"Unknown color type {type}");
        }
    }

    private ref struct Reader(byte[] data)
    {
        private readonly byte[] _data = data;
        private int _pos;

        public string[] Strings = [];

        public ReadOnlySpan<byte> Bytes(int count)
        {
            var span = _data.AsSpan(_pos, count);
            _pos += count;
            return span;
        }

        public byte U8() => _data[_pos++];

        public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Bytes(2));

        public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));

        public int I32() => BinaryPrimitives.ReadInt32LittleEndian(Bytes(4));

        public float F32() => BinaryPrimitives.ReadSingleLittleEndian(Bytes(4));

        public double F64() => BinaryPrimitives.ReadDoubleLittleEndian(Bytes(8));

        public string InlineString() => Encoding.UTF8.GetString(Bytes((int)U32()));

        public string Str() => Strings[(int)U32()];
    }
}
