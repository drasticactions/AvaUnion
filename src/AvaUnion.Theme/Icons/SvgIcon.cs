using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal sealed class SvgIcon
{
    private readonly SvgNode _root;
    private readonly double _margin;

    private SvgIcon(SvgNode root, Rect box, double margin = 0)
    {
        _root = root;
        Box = box;
        _margin = margin;
    }

    public Rect Box { get; }

    public static SvgIcon? Read(Stream stream)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = null,
                MaxCharactersFromEntities = 1 << 20,
                MaxCharactersInDocument = 64 << 20,
            };
            using var reader = XmlReader.Create(stream, settings);
            var (root, box) = SvgReader.Read(XDocument.Load(reader).Root!);
            return root is null ? null : new SvgIcon(root, box);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static SvgIcon? Read(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);
            return Read(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public SvgIcon Padded(double margin) =>
        new(_root, new Rect(0, 0, Box.Width + 2 * margin, Box.Height + 2 * margin), _margin + margin);

    public IImage ToImage(Color? tint)
    {
        var content = new DrawingGroup { ClipGeometry = new RectangleGeometry(Box) };
        if (_root.Build(tint) is { } drawing)
        {
            content.Children.Add(_margin == 0 ? drawing : new DrawingGroup { Transform = new TranslateTransform(_margin, _margin), Children = { drawing } });
        }

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Geometry = new RectangleGeometry(Box), Brush = Brushes.Transparent });
        group.Children.Add(content);
        return new DrawingImage(group) { Viewbox = Box };
    }
}
