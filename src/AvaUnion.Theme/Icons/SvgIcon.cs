using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal sealed partial class SvgIcon
{
    private readonly List<Shape> _shapes;

    private SvgIcon(List<Shape> shapes, Rect box)
    {
        _shapes = shapes;
        Box = box;
    }

    public Rect Box { get; }

    public static SvgIcon? Read(Stream stream)
    {
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
            return Read(reader);
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

    public IImage ToImage(Color? tint)
    {
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Geometry = new RectangleGeometry(Box), Brush = Brushes.Transparent });
        foreach (var shape in _shapes)
        {
            var color = shape.Color ?? tint ?? Colors.Black;
            var alpha = (byte)Math.Clamp(Math.Round(color.A * shape.Opacity), 0, 255);
            group.Children.Add(new GeometryDrawing { Geometry = shape.Geometry, Brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)) });
        }

        return new DrawingImage(group);
    }

    private static SvgIcon? Read(XmlReader reader)
    {
        var classes = new Dictionary<string, Color>();
        var gradients = new Dictionary<string, Color>();
        var shapes = new List<Shape>();
        var transforms = new Stack<Matrix>();
        var opacities = new Stack<double>();
        var fills = new Stack<string?>();
        transforms.Push(Matrix.Identity);
        opacities.Push(1.0);
        fills.Push(null);
        Rect? viewBox = null;
        double width = 0, height = 0;
        var hidden = 0;
        string? gradient = null;
        var inStyle = false;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                switch (reader.LocalName)
                {
                    case "defs" or "clipPath" or "mask" or "symbol" or "pattern":
                        hidden--;
                        break;
                    case "g":
                        transforms.Pop();
                        opacities.Pop();
                        fills.Pop();
                        break;
                    case "linearGradient" or "radialGradient":
                        gradient = null;
                        break;
                    case "style":
                        inStyle = false;
                        break;
                }

                continue;
            }

            if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA && inStyle)
            {
                foreach (Match match in StyleRule().Matches(reader.Value))
                {
                    if (TryColor(match.Groups[2].Value, out var color))
                    {
                        classes[match.Groups[1].Value] = color;
                    }
                }

                continue;
            }

            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            var empty = reader.IsEmptyElement;
            switch (reader.LocalName)
            {
                case "svg":
                    width = D(reader.GetAttribute("width"));
                    height = D(reader.GetAttribute("height"));
                    if (reader.GetAttribute("viewBox") is { } vb)
                    {
                        var p = vb.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length == 4)
                        {
                            viewBox = new Rect(D(p[0]), D(p[1]), D(p[2]), D(p[3]));
                        }
                    }

                    break;
                case "style":
                    inStyle = !empty;
                    break;
                case "defs" or "clipPath" or "mask" or "symbol" or "pattern":
                    if (!empty)
                    {
                        hidden++;
                    }

                    break;
                case "linearGradient" or "radialGradient":
                    gradient = empty ? null : reader.GetAttribute("id");
                    break;
                case "stop" when gradient is not null && !gradients.ContainsKey(gradient):
                    if (TryColor(Property(reader, "stop-color") ?? string.Empty, out var stop))
                    {
                        gradients[gradient] = stop;
                    }

                    break;
                case "g":
                    if (!empty)
                    {
                        transforms.Push(Transform(reader.GetAttribute("transform")) * transforms.Peek());
                        opacities.Push(opacities.Peek() * Opacity(reader, "opacity"));
                        fills.Push(Property(reader, "fill") ?? fills.Peek());
                    }

                    break;
                case "path" or "rect" or "circle" or "ellipse" when hidden == 0:
                    var geometry = Geometry(reader);
                    if (geometry is null)
                    {
                        break;
                    }

                    var fill = Property(reader, "fill") ?? fills.Peek();
                    if (fill == "none" || reader.GetAttribute("display") == "none")
                    {
                        break;
                    }

                    var matrix = Transform(reader.GetAttribute("transform")) * transforms.Peek();
                    if (!matrix.IsIdentity)
                    {
                        geometry.Transform = new MatrixTransform(matrix);
                    }

                    var opacity = opacities.Peek() * Opacity(reader, "opacity") * Opacity(reader, "fill-opacity");
                    shapes.Add(new Shape(geometry, ColorOf(fill, reader.GetAttribute("class"), classes, gradients), opacity));
                    break;
            }
        }

        if (shapes.Count == 0)
        {
            return null;
        }

        var box = viewBox ?? new Rect(0, 0, width > 0 ? width : 16, height > 0 ? height : 16);
        if (box.X != 0 || box.Y != 0)
        {
            var shift = Matrix.CreateTranslation(-box.X, -box.Y);
            foreach (var shape in shapes)
            {
                shape.Geometry.Transform = new MatrixTransform((shape.Geometry.Transform?.Value ?? Matrix.Identity) * shift);
            }
        }

        return new SvgIcon(shapes, new Rect(0, 0, box.Width, box.Height));
    }

    private static Geometry? Geometry(XmlReader reader)
    {
        switch (reader.LocalName)
        {
            case "path":
                return reader.GetAttribute("d") is { Length: > 0 } d ? StreamGeometry.Parse(d) : null;
            case "rect":
                var rect = new Rect(D(reader.GetAttribute("x")), D(reader.GetAttribute("y")), D(reader.GetAttribute("width")), D(reader.GetAttribute("height")));
                var rx = D(reader.GetAttribute("rx"));
                var ry = reader.GetAttribute("ry") is { } ryText ? D(ryText) : rx;
                return new RectangleGeometry(rect, rx, ry == 0 ? rx : ry);
            case "circle":
                var r = D(reader.GetAttribute("r"));
                return new EllipseGeometry(new Rect(D(reader.GetAttribute("cx")) - r, D(reader.GetAttribute("cy")) - r, 2 * r, 2 * r));
            default:
                var erx = D(reader.GetAttribute("rx"));
                var ery = D(reader.GetAttribute("ry"));
                return new EllipseGeometry(new Rect(D(reader.GetAttribute("cx")) - erx, D(reader.GetAttribute("cy")) - ery, 2 * erx, 2 * ery));
        }
    }

    private static Color? ColorOf(string? fill, string? classAttribute, Dictionary<string, Color> classes, Dictionary<string, Color> gradients)
    {
        if (fill is null)
        {
            return Colors.Black;
        }

        if (fill is "currentColor" or "currentcolor")
        {
            if (classAttribute is not null)
            {
                foreach (var name in classAttribute.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (name != "ColorScheme-Text" && classes.TryGetValue(name, out var schemed))
                    {
                        return schemed;
                    }
                }
            }

            return null;
        }

        if (fill.StartsWith("url(#", StringComparison.Ordinal))
        {
            return gradients.TryGetValue(fill[5..^1], out var stop) ? stop : Colors.Black;
        }

        return TryColor(fill, out var color) ? color : Colors.Black;
    }

    private static string? Property(XmlReader reader, string name)
    {
        if (reader.GetAttribute("style") is { } style)
        {
            foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = part.IndexOf(':');
                if (colon > 0 && part[..colon].Trim() == name)
                {
                    return part[(colon + 1)..].Trim();
                }
            }
        }

        return reader.GetAttribute(name)?.Trim();
    }

    private static double Opacity(XmlReader reader, string name) =>
        Property(reader, name) is { } value && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity)
            ? Math.Clamp(opacity, 0, 1)
            : 1.0;

    private static bool TryColor(string text, out Color color)
    {
        text = text.Trim();
        if (text.Length is 4 or 7 && text[0] == '#' && Color.TryParse(text, out color))
        {
            return true;
        }

        if (text.Length > 0 && text[0] != '#' && text != "none" && Color.TryParse(text, out color))
        {
            return true;
        }

        color = default;
        return false;
    }

    private static Matrix Transform(string? text)
    {
        var result = Matrix.Identity;
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (Match match in TransformCall().Matches(text))
        {
            var args = match.Groups[2].Value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            var v = Array.ConvertAll(args, D);
            var m = match.Groups[1].Value switch
            {
                "matrix" when v.Length == 6 => new Matrix(v[0], v[1], v[2], v[3], v[4], v[5]),
                "translate" when v.Length >= 1 => Matrix.CreateTranslation(v[0], v.Length > 1 ? v[1] : 0),
                "scale" when v.Length >= 1 => Matrix.CreateScale(v[0], v.Length > 1 ? v[1] : v[0]),
                "rotate" when v.Length == 1 => Matrix.CreateRotation(v[0] * Math.PI / 180),
                "rotate" when v.Length == 3 => Matrix.CreateTranslation(-v[1], -v[2]) * Matrix.CreateRotation(v[0] * Math.PI / 180) * Matrix.CreateTranslation(v[1], v[2]),
                _ => Matrix.Identity,
            };
            result = m * result;
        }

        return result;
    }

    private static double D(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0.0;

    [GeneratedRegex(@"\.([A-Za-z0-9_-]+)\s*\{[^}]*?color\s*:\s*([^;}\s]+)")]
    private static partial Regex StyleRule();

    [GeneratedRegex(@"(matrix|translate|scale|rotate)\s*\(([^)]*)\)")]
    private static partial Regex TransformCall();

    private sealed record Shape(Geometry Geometry, Color? Color, double Opacity);
}
