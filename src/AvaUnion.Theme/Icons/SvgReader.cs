using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal sealed partial class SvgReader
{
    private const string TextClass = "ColorScheme-Text";
    private static readonly XNamespace s_svg = "http://www.w3.org/2000/svg";
    private static readonly XNamespace s_xlink = "http://www.w3.org/1999/xlink";

    private readonly Dictionary<string, List<XElement>> _ids = new(StringComparer.Ordinal);
    private readonly List<Rule> _rules = [];
    private readonly Dictionary<XElement, Style> _domStyles = [];
    private readonly HashSet<XElement> _active = [];
    private Rect _viewport;

    private SvgReader(XElement root)
    {
        foreach (var element in root.DescendantsAndSelf())
        {
            if (element.Attribute("id")?.Value is { Length: > 0 } id)
            {
                if (!_ids.TryGetValue(id, out var list))
                {
                    _ids[id] = list = [];
                }

                list.Add(element);
            }

            if (element.Name.LocalName == "style" && IsSvg(element))
            {
                ParseCss(element.Value);
            }
        }
    }

    public static (SvgNode? Root, Rect Box) Read(XElement root)
    {
        if (root.Name.LocalName != "svg")
        {
            return (null, default);
        }

        var reader = new SvgReader(root);
        var viewBox = SvgParser.Numbers(root.Attribute("viewBox")?.Value) is { Count: 4 } vb && vb[2] > 0 && vb[3] > 0 ? new Rect(vb[0], vb[1], vb[2], vb[3]) : (Rect?)null;
        var width = Size(root.Attribute("width")?.Value);
        var height = Size(root.Attribute("height")?.Value);
        if (viewBox is { } v)
        {
            width ??= height is { } h ? h * v.Width / v.Height : v.Width;
            height ??= width.Value * v.Height / v.Width;
        }

        var box = new Rect(0, 0, width ?? 16, height ?? 16);
        reader._viewport = viewBox ?? box;
        var transform = viewBox is { } view ? ViewBoxTransform(view, box.Size, root.Attribute("preserveAspectRatio")?.Value) : Matrix.Identity;
        var node = reader.Render(root, Style.Initial, null, transform);
        return (node, box);
    }

    private static double? Size(string? text) =>
        text is null || text.TrimEnd().EndsWith('%') ? null : SvgParser.Length(text) is > 0 and var value ? value : null;

    private static Matrix ViewBoxTransform(Rect view, Size size, string? aspect)
    {
        var sx = size.Width / view.Width;
        var sy = size.Height / view.Height;
        var parts = (aspect ?? "xMidYMid meet").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var align = parts.Length > 0 ? parts[0] : "xMidYMid";
        double tx = 0, ty = 0;
        if (align != "none")
        {
            var scale = parts.Length > 1 && parts[1] == "slice" ? Math.Max(sx, sy) : Math.Min(sx, sy);
            sx = sy = scale;
            var free = new Size(size.Width - view.Width * scale, size.Height - view.Height * scale);
            tx = align.Contains("xMid", StringComparison.Ordinal) ? free.Width / 2 : align.Contains("xMax", StringComparison.Ordinal) ? free.Width : 0;
            ty = align.Contains("YMid", StringComparison.Ordinal) ? free.Height / 2 : align.Contains("YMax", StringComparison.Ordinal) ? free.Height : 0;
        }

        return Matrix.CreateTranslation(-view.X, -view.Y) * Matrix.CreateScale(sx, sy) * Matrix.CreateTranslation(tx, ty);
    }

    private static bool IsSvg(XElement element) => element.Name.Namespace == s_svg || element.Name.Namespace == XNamespace.None;

    private SvgNode? Render(XElement element, Style parent, SvgColorMap? map, Matrix? extra = null)
    {
        if (!IsSvg(element) || !_active.Add(element))
        {
            return null;
        }

        try
        {
            return RenderCore(element, parent, map, extra);
        }
        finally
        {
            _active.Remove(element);
        }
    }

    private SvgNode? RenderCore(XElement element, Style parent, SvgColorMap? map, Matrix? extra)
    {
        var name = element.Name.LocalName;
        if (name is "defs" or "clipPath" or "mask" or "symbol" or "pattern" or "linearGradient" or "radialGradient" or "filter"
            or "style" or "metadata" or "title" or "desc" or "foreignObject" or "marker" or "image" or "flowRoot" or "script")
        {
            return null;
        }

        var style = Cascade(element, parent);
        if (style.Hidden)
        {
            return null;
        }

        var transform = SvgParser.Transform(element.Attribute("transform")?.Value);
        if (extra is { } e)
        {
            transform = e;
        }

        var filter = Filter(style.Filter);
        if (filter.Skip)
        {
            return null;
        }

        var contentMap = Compose(map, filter.Map);
        var children = new List<SvgNode>();
        switch (name)
        {
            case "svg" or "g" or "a":
                if (name == "svg" && extra is null && element.Parent is not null)
                {
                    transform = NestedViewport(element) * transform;
                }

                foreach (var child in element.Elements())
                {
                    if (Render(child, style, contentMap) is { } node)
                    {
                        children.Add(node);
                    }
                }

                break;
            case "switch":
                foreach (var child in element.Elements())
                {
                    if (child.Attribute("requiredExtensions") is not null || child.Name.LocalName == "foreignObject" || !IsSvg(child))
                    {
                        continue;
                    }

                    if (Render(child, style, contentMap) is { } node)
                    {
                        children.Add(node);
                    }

                    break;
                }

                break;
            case "use":
                if (Reference(element) is { } target)
                {
                    var offset = Matrix.CreateTranslation(SvgParser.Length(element.Attribute("x")?.Value), SvgParser.Length(element.Attribute("y")?.Value));
                    SvgNode? node;
                    if (target.Name.LocalName is "symbol" or "svg")
                    {
                        node = RenderSymbol(target, element, style, contentMap);
                    }
                    else
                    {
                        node = Render(target, style, contentMap);
                    }

                    if (node is not null)
                    {
                        var wrapper = new SvgGroup { Transform = offset };
                        wrapper.Children.Add(node);
                        children.Add(wrapper);
                    }
                }

                break;
            case "text":
                if (style.Visible)
                {
                    children.AddRange(Text(element, style, contentMap));
                }

                break;
            default:
                if (style.Visible && Shape(element, style, contentMap) is { } shape)
                {
                    if (style.Opacity < 1 && filter.Effect is null && style.Clip is null && style.Mask is null && (shape.Fill is null || shape.Stroke is null))
                    {
                        shape.FillOpacity *= style.Opacity;
                        shape.StrokeOpacity *= style.Opacity;
                        style = style with { Opacity = 1 };
                    }

                    children.Add(shape);
                }

                break;
        }

        if (children.Count == 0)
        {
            return null;
        }

        var bounds = filter.Effect is not null || style.Mask is not null || style.Clip is not null ? Bounds(children) : default;
        Geometry? clip = null;
        if (style.Clip is not null && Target(style.Clip, "clipPath") is { } clipElement)
        {
            clip = ClipGeometry(clipElement, bounds);
            if (clip is null)
            {
                return null;
            }
        }

        SvgNode? mask = null;
        Rect maskBounds = default;
        if (style.Mask is not null && Target(style.Mask, "mask") is { } maskElement)
        {
            (mask, maskBounds, var clipped) = Mask(maskElement, bounds, map);
            if (mask is null)
            {
                return null;
            }

            if (clipped)
            {
                var region = new RectangleGeometry(maskBounds);
                clip = clip is null ? region : new CombinedGeometry(GeometryCombineMode.Intersect, clip, region);
            }
        }

        var group = new SvgGroup
        {
            Transform = transform,
            Opacity = style.Opacity,
            Clip = clip,
            Mask = mask,
            MaskBounds = maskBounds,
            Effect = filter.Effect,
            EffectClip = filter.Effect is not null && filter.Element is not null ? new RectangleGeometry(Region(filter.Element, "filterUnits", bounds)) : null,
        };
        group.Children.AddRange(children);
        return group;
    }

    private Matrix NestedViewport(XElement element)
    {
        var x = SvgParser.Length(element.Attribute("x")?.Value);
        var y = SvgParser.Length(element.Attribute("y")?.Value);
        var translate = Matrix.CreateTranslation(x, y);
        if (SvgParser.Numbers(element.Attribute("viewBox")?.Value) is { Count: 4 } vb && vb[2] > 0 && vb[3] > 0)
        {
            var w = SvgParser.Length(element.Attribute("width")?.Value, _viewport.Width, _viewport.Width);
            var h = SvgParser.Length(element.Attribute("height")?.Value, _viewport.Height, _viewport.Height);
            return ViewBoxTransform(new Rect(vb[0], vb[1], vb[2], vb[3]), new Size(w, h), element.Attribute("preserveAspectRatio")?.Value) * translate;
        }

        return translate;
    }

    private SvgNode? RenderSymbol(XElement symbol, XElement use, Style style, SvgColorMap? map)
    {
        var matrix = Matrix.Identity;
        if (SvgParser.Numbers(symbol.Attribute("viewBox")?.Value) is { Count: 4 } vb && vb[2] > 0 && vb[3] > 0)
        {
            var w = SvgParser.Length(use.Attribute("width")?.Value ?? symbol.Attribute("width")?.Value, _viewport.Width, _viewport.Width);
            var h = SvgParser.Length(use.Attribute("height")?.Value ?? symbol.Attribute("height")?.Value, _viewport.Height, _viewport.Height);
            matrix = ViewBoxTransform(new Rect(vb[0], vb[1], vb[2], vb[3]), new Size(w, h), symbol.Attribute("preserveAspectRatio")?.Value);
        }

        var symbolStyle = Cascade(symbol, style);
        var group = new SvgGroup { Transform = matrix * SvgParser.Transform(symbol.Attribute("transform")?.Value) };
        foreach (var child in symbol.Elements())
        {
            if (Render(child, symbolStyle, map) is { } node)
            {
                group.Children.Add(node);
            }
        }

        return group.Children.Count == 0 ? null : group;
    }

    private SvgShape? Shape(XElement element, Style style, SvgColorMap? map)
    {
        var geometry = Geometry(element, style.FillRule);
        if (geometry is null)
        {
            return null;
        }

        var bounds = geometry.Bounds;
        var fill = Paint(style.Fill, style.FillColor, bounds, map);
        var stroke = style.StrokeWidth > 0 ? Paint(style.Stroke, style.StrokeColor, bounds, map) : null;
        if (fill is null && stroke is null)
        {
            return null;
        }

        IReadOnlyList<double>? dashes = null;
        if (style.Dashes is { Count: > 0 } d && d.All(x => x >= 0) && d.Sum() > 0)
        {
            dashes = d.Count % 2 == 1 ? [.. d, .. d] : d;
        }

        return new SvgShape
        {
            Geometry = geometry,
            Fill = fill,
            FillOpacity = style.FillOpacity,
            Stroke = stroke,
            StrokeOpacity = style.StrokeOpacity,
            StrokeWidth = style.StrokeWidth,
            LineCap = style.LineCap,
            LineJoin = style.LineJoin,
            MiterLimit = style.MiterLimit,
            Dashes = dashes,
            DashOffset = style.DashOffset,
            StrokeFirst = style.StrokeFirst,
        };
    }

    private Geometry? Geometry(XElement element, FillRule rule)
    {
        double A(string name, double percentOf = 0) => SvgParser.Length(element.Attribute(name)?.Value, percentOf);

        switch (element.Name.LocalName)
        {
            case "path":
                return SvgParser.Path(element.Attribute("d")?.Value, rule);
            case "rect":
                var w = A("width", _viewport.Width);
                var h = A("height", _viewport.Height);
                if (w <= 0 || h <= 0)
                {
                    return null;
                }

                var rxText = element.Attribute("rx")?.Value;
                var ryText = element.Attribute("ry")?.Value;
                var rx = rxText is null ? -1 : A("rx", _viewport.Width);
                var ry = ryText is null ? -1 : A("ry", _viewport.Height);
                if (rx < 0)
                {
                    rx = Math.Max(ry, 0);
                }

                if (ry < 0)
                {
                    ry = rx;
                }

                return SvgParser.Rectangle(new Rect(A("x", _viewport.Width), A("y", _viewport.Height), w, h), Math.Min(rx, w / 2), Math.Min(ry, h / 2));
            case "circle":
                var r = A("r", Math.Sqrt((_viewport.Width * _viewport.Width + _viewport.Height * _viewport.Height) / 2));
                return r <= 0 ? null : SvgParser.Ellipse(new Point(A("cx", _viewport.Width), A("cy", _viewport.Height)), r, r);
            case "ellipse":
                var erx = element.Attribute("rx") is null ? -1 : A("rx", _viewport.Width);
                var ery = element.Attribute("ry") is null ? -1 : A("ry", _viewport.Height);
                erx = erx < 0 ? ery : erx;
                ery = ery < 0 ? erx : ery;
                return erx <= 0 || ery <= 0 ? null : SvgParser.Ellipse(new Point(A("cx", _viewport.Width), A("cy", _viewport.Height)), erx, ery);
            case "line":
                var line = new StreamGeometry();
                using (var context = line.Open())
                {
                    context.BeginFigure(new Point(A("x1", _viewport.Width), A("y1", _viewport.Height)), false);
                    context.LineTo(new Point(A("x2", _viewport.Width), A("y2", _viewport.Height)));
                    context.EndFigure(false);
                }

                return line;
            case "polyline":
                return SvgParser.Poly(element.Attribute("points")?.Value, false, rule);
            case "polygon":
                return SvgParser.Poly(element.Attribute("points")?.Value, true, rule);
            default:
                return null;
        }
    }

    private IEnumerable<SvgNode> Text(XElement text, Style style, SvgColorMap? map)
    {
        var nodes = new List<SvgNode>();
        var pen = new Point(FirstLength(text.Attribute("x")?.Value), FirstLength(text.Attribute("y")?.Value));
        var line = new List<(Geometry Geometry, Style Style)>();
        var lineStart = pen.X;
        var anchor = style.TextAnchor;

        void Flush()
        {
            if (line.Count == 0)
            {
                return;
            }

            var shift = anchor switch
            {
                "middle" => -(pen.X - lineStart) / 2,
                "end" => -(pen.X - lineStart),
                _ => 0,
            };
            foreach (var (geometry, runStyle) in line)
            {
                if (shift != 0)
                {
                    geometry.Transform = new MatrixTransform(Matrix.CreateTranslation(shift, 0));
                }

                var bounds = geometry.Bounds;
                var fill = Paint(runStyle.Fill, runStyle.FillColor, bounds, map);
                var stroke = runStyle.StrokeWidth > 0 ? Paint(runStyle.Stroke, runStyle.StrokeColor, bounds, map) : null;
                if (fill is not null || stroke is not null)
                {
                    nodes.Add(new SvgShape
                    {
                        Geometry = geometry,
                        Fill = fill,
                        FillOpacity = runStyle.FillOpacity,
                        Stroke = stroke,
                        StrokeOpacity = runStyle.StrokeOpacity,
                        StrokeWidth = runStyle.StrokeWidth,
                        LineCap = runStyle.LineCap,
                        LineJoin = runStyle.LineJoin,
                        MiterLimit = runStyle.MiterLimit,
                    });
                }
            }

            line.Clear();
        }

        void Walk(XElement element, Style elementStyle)
        {
            foreach (var part in element.Nodes())
            {
                if (part is XText run)
                {
                    var content = WhitespaceRun().Replace(run.Value, " ");
                    if (part.PreviousNode is null && part.Parent == text)
                    {
                        content = content.TrimStart();
                    }

                    if (part.NextNode is null && part.Parent == text)
                    {
                        content = content.TrimEnd();
                    }

                    if (content.Length == 0 || !elementStyle.Visible)
                    {
                        continue;
                    }

                    try
                    {
                        var typeface = new Typeface(new FontFamily(elementStyle.FontFamily), elementStyle.FontStyle, elementStyle.FontWeight);
                        var formatted = new FormattedText(content, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, elementStyle.FontSize, Brushes.Black);
                        if (formatted.BuildGeometry(new Point(pen.X, pen.Y - formatted.Baseline)) is { } geometry)
                        {
                            if (geometry is GeometryGroup glyphs)
                            {
                                glyphs.FillRule = FillRule.NonZero;
                            }

                            line.Add((geometry, elementStyle));
                        }

                        pen = new Point(pen.X + formatted.WidthIncludingTrailingWhitespace, pen.Y);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
                else if (part is XElement child && child.Name.LocalName is "tspan" && IsSvg(child))
                {
                    var childStyle = Cascade(child, elementStyle);
                    if (childStyle.Hidden)
                    {
                        continue;
                    }

                    if (child.Attribute("x") is not null || child.Attribute("y") is not null)
                    {
                        Flush();
                        pen = new Point(
                            child.Attribute("x") is { } x ? FirstLength(x.Value) : pen.X,
                            child.Attribute("y") is { } y ? FirstLength(y.Value) : pen.Y);
                        lineStart = pen.X;
                        anchor = childStyle.TextAnchor;
                    }

                    Walk(child, childStyle);
                }
            }
        }

        Walk(text, style);
        Flush();
        return nodes;
    }

    private static double FirstLength(string? text) =>
        SvgParser.Numbers(text) is { Count: > 0 } list ? list[0] : 0;

    private SvgPaint? Paint(string? value, SvgColor current, Rect bounds, SvgColorMap? map)
    {
        if (value is null)
        {
            return null;
        }

        value = value.Trim();
        if (value.StartsWith("url(", StringComparison.Ordinal))
        {
            var close = value.IndexOf(')');
            if (close < 0)
            {
                return null;
            }

            var id = value[4..close].Trim().Trim('\'', '"');
            if (id.StartsWith('#') && Lookup(id[1..], "linearGradient", "radialGradient") is { } server)
            {
                return Gradient(server, bounds, map, out var none) ?? (none ? null : Paint(value[(close + 1)..], current, bounds, map));
            }

            var fallback = value[(close + 1)..].Trim();
            return fallback.Length > 0 ? Paint(fallback, current, bounds, map) : null;
        }

        if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
        {
            return new SvgSolidPaint(current.With(map));
        }

        return SvgParser.TryColor(value, out var color) ? new SvgSolidPaint(new SvgColor(color, false).With(map)) : null;
    }

    private SvgPaint? Gradient(XElement server, Rect bounds, SvgColorMap? map, out bool none)
    {
        none = true;
        var chain = new List<XElement>();
        for (var current = server; current is not null && !chain.Contains(current) && chain.Count < 16; current = Reference(current, "linearGradient", "radialGradient"))
        {
            if (current.Name.LocalName is "linearGradient" or "radialGradient")
            {
                chain.Add(current);
            }
        }

        string? Attr(string name) => chain.Select(e => e.Attribute(name)?.Value).FirstOrDefault(v => v is not null);

        var stopElements = chain.Select(e => e.Elements().Where(c => c.Name.LocalName == "stop").ToList()).FirstOrDefault(l => l.Count > 0);
        if (stopElements is null)
        {
            return null;
        }

        var stops = new List<(double, SvgColor, double)>();
        var last = 0.0;
        foreach (var stop in stopElements)
        {
            var stopStyle = DomStyle(stop);
            var offset = Math.Max(last, Math.Clamp(SvgParser.Fraction(stop.Attribute("offset")?.Value, 0), 0, 1));
            last = offset;
            var color = stopStyle.StopColor is { } sc && sc.Trim().Equals("currentColor", StringComparison.OrdinalIgnoreCase)
                ? stopStyle.Color
                : new SvgColor(SvgParser.TryColor(stopStyle.StopColor, out var c) ? c : Colors.Black, false);
            stops.Add((offset, color.With(map), stopStyle.StopOpacity));
        }

        if (stops.Count == 1)
        {
            none = false;
            var (_, only, opacity) = stops[0];
            return new SvgSolidPaint(only.With(c => WithOpacity(c, opacity)));
        }

        var userSpace = Attr("gradientUnits") == "userSpaceOnUse";
        if (!userSpace && (bounds.Width <= 0 || bounds.Height <= 0))
        {
            return null;
        }

        var unit = userSpace ? Matrix.Identity : new Matrix(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y);
        var transform = SvgParser.Transform(Attr("gradientTransform")) * unit;
        var spread = server.Attribute("spreadMethod")?.Value switch
        {
            "reflect" => GradientSpreadMethod.Reflect,
            "repeat" => GradientSpreadMethod.Repeat,
            _ => GradientSpreadMethod.Pad,
        };

        double Coord(string name, string fallback, double percentOf) =>
            userSpace ? SvgParser.Length(Attr(name) ?? fallback, percentOf) : SvgParser.Fraction(Attr(name) ?? fallback, 0);

        none = false;
        if (server.Name.LocalName == "linearGradient")
        {
            var start = new Point(Coord("x1", "0%", _viewport.Width), Coord("y1", "0%", _viewport.Height));
            var end = new Point(Coord("x2", "100%", _viewport.Width), Coord("y2", "0%", _viewport.Height));
            if (start == end)
            {
                var (_, lastColor, lastOpacity) = stops[^1];
                return new SvgSolidPaint(lastColor.With(c => WithOpacity(c, lastOpacity)));
            }

            return new SvgGradientPaint { Radial = false, Start = start, End = end, Transform = transform, Spread = spread, Stops = stops };
        }

        var diagonal = Math.Sqrt((_viewport.Width * _viewport.Width + _viewport.Height * _viewport.Height) / 2);
        var center = new Point(Coord("cx", "50%", _viewport.Width), Coord("cy", "50%", _viewport.Height));
        var radius = Coord("r", "50%", diagonal);
        var fx = Attr("fx") is null ? center.X : Coord("fx", "50%", _viewport.Width);
        var fy = Attr("fy") is null ? center.Y : Coord("fy", "50%", _viewport.Height);
        if (radius <= 0)
        {
            var (_, lastColor, lastOpacity) = stops[^1];
            return new SvgSolidPaint(lastColor.With(c => WithOpacity(c, lastOpacity)));
        }

        var focus = new Point(fx, fy);
        var distance = Math.Sqrt((focus.X - center.X) * (focus.X - center.X) + (focus.Y - center.Y) * (focus.Y - center.Y));
        if (distance > radius * 0.999)
        {
            focus = center + (focus - center) * (radius * 0.999 / distance);
        }

        return new SvgGradientPaint { Radial = true, Start = focus, End = center, Radius = radius, Transform = transform, Spread = spread, Stops = stops };
    }

    private static Color WithOpacity(Color color, double opacity) =>
        Color.FromArgb((byte)Math.Clamp(Math.Round(color.A * opacity), 0, 255), color.R, color.G, color.B);

    private Geometry? ClipGeometry(XElement clip, Rect bounds)
    {
        if (!_active.Add(clip))
        {
            return null;
        }

        try
        {
            var units = clip.Attribute("clipPathUnits")?.Value == "objectBoundingBox" ? new Matrix(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y) : Matrix.Identity;
            var outer = SvgParser.Transform(clip.Attribute("transform")?.Value) * units;
            var clipStyle = DomStyle(clip);
            Geometry? result = null;
            foreach (var child in clip.Elements())
            {
                var element = child;
                var matrix = SvgParser.Transform(child.Attribute("transform")?.Value);
                var childStyle = Cascade(child, clipStyle);
                if (child.Name.LocalName == "use" && Reference(child) is { } target)
                {
                    matrix = SvgParser.Transform(target.Attribute("transform")?.Value)
                        * Matrix.CreateTranslation(SvgParser.Length(child.Attribute("x")?.Value), SvgParser.Length(child.Attribute("y")?.Value)) * matrix;
                    element = target;
                    childStyle = Cascade(target, childStyle);
                }

                if (childStyle.Hidden || !childStyle.Visible || Geometry(element, childStyle.ClipRule) is not { } geometry)
                {
                    continue;
                }

                var total = matrix * outer;
                if (!total.IsIdentity)
                {
                    geometry.Transform = new MatrixTransform(total);
                }

                result = result is null ? geometry : new CombinedGeometry(GeometryCombineMode.Union, result, geometry);
            }

            return result ?? new RectangleGeometry(default);
        }
        finally
        {
            _active.Remove(clip);
        }
    }

    private (SvgNode? Node, Rect Bounds, bool Clipped) Mask(XElement mask, Rect bounds, SvgColorMap? map)
    {
        if (!_active.Add(mask))
        {
            return (null, default, false);
        }

        try
        {
            var unbounded = mask.Attribute("maskUnits")?.Value == "userSpaceOnUse"
                && mask.Attribute("x") is null && mask.Attribute("y") is null && mask.Attribute("width") is null && mask.Attribute("height") is null;
            var region = unbounded ? bounds : Region(mask, "maskUnits", bounds);
            var maskStyle = DomStyle(mask);
            var luminance = Compose(map, Luminance);
            var group = new SvgGroup
            {
                Transform = mask.Attribute("maskContentUnits")?.Value == "objectBoundingBox" ? new Matrix(bounds.Width, 0, 0, bounds.Height, bounds.X, bounds.Y) : Matrix.Identity,
            };
            foreach (var child in mask.Elements())
            {
                if (Render(child, maskStyle, luminance) is { } node)
                {
                    group.Children.Add(node);
                }
            }

            return (group.Children.Count == 0 || region.Width <= 0 || region.Height <= 0 ? null : group, region, !unbounded);
        }
        finally
        {
            _active.Remove(mask);
        }
    }

    private static Color Luminance(Color color)
    {
        var l = (0.2125 * color.R + 0.7154 * color.G + 0.0721 * color.B) / 255;
        return Color.FromArgb((byte)Math.Clamp(Math.Round(color.A * l), 0, 255), 255, 255, 255);
    }

    private Rect Region(XElement element, string unitsName, Rect bounds)
    {
        if (element.Attribute(unitsName)?.Value == "userSpaceOnUse")
        {
            double U(string name, double fallback, double size) => SvgParser.Length(element.Attribute(name)?.Value, size, fallback * size);

            return new Rect(
                U("x", -0.1, _viewport.Width) + (element.Attribute("x") is null ? _viewport.X : 0),
                U("y", -0.1, _viewport.Height) + (element.Attribute("y") is null ? _viewport.Y : 0),
                Math.Max(0, U("width", 1.2, _viewport.Width)),
                Math.Max(0, U("height", 1.2, _viewport.Height)));
        }

        double F(string name, double fallback) => SvgParser.Fraction(element.Attribute(name)?.Value, fallback);

        return new Rect(
            bounds.X + F("x", -0.1) * bounds.Width,
            bounds.Y + F("y", -0.1) * bounds.Height,
            Math.Max(0, F("width", 1.2) * bounds.Width),
            Math.Max(0, F("height", 1.2) * bounds.Height));
    }

    private (IEffect? Effect, SvgColorMap? Map, XElement? Element, bool Skip) Filter(string? reference)
    {
        if (reference is null)
        {
            return default;
        }

        if (Target(reference, "filter") is not { } filter)
        {
            return (null, null, null, true);
        }

        var primitives = filter.Elements().Where(e => e.Name.LocalName.StartsWith("fe", StringComparison.Ordinal)).ToList();
        if (primitives.Count == 0)
        {
            return (null, null, null, true);
        }

        if (primitives.All(p => p.Name.LocalName == "feColorMatrix"))
        {
            SvgColorMap? map = null;
            foreach (var primitive in primitives)
            {
                map = Compose(map, ColorMatrix(primitive));
            }

            return (null, map, null, false);
        }

        if (primitives.Count == 1 && primitives[0].Name.LocalName == "feGaussianBlur")
        {
            var sigma = Sigma(primitives[0]);
            return sigma <= 0 ? default : (new BlurEffect { Radius = BlurRadius(sigma) }, null, filter, false);
        }

        var names = string.Join(",", primitives.Select(p => p.Name.LocalName));
        if (names == "feFlood,feComposite,feGaussianBlur,feOffset,feComposite"
            && primitives[1].Attribute("operator")?.Value == "in" && primitives[4].Attribute("operator")?.Value == "over"
            && primitives[4].Attribute("in")?.Value == "SourceGraphic")
        {
            var flood = primitives[0];
            var floodStyle = DomStyle(flood);
            var color = SvgParser.TryColor(flood.Attribute("flood-color")?.Value ?? floodStyle.Get("flood-color"), out var c) ? c : Colors.Black;
            var floodOpacity = SvgParser.Fraction(flood.Attribute("flood-opacity")?.Value ?? floodStyle.Get("flood-opacity"), 1);
            return (new DropShadowEffect
            {
                Color = color,
                Opacity = floodOpacity * color.A / 255,
                BlurRadius = BlurRadius(Sigma(primitives[2])),
                OffsetX = SvgParser.Length(primitives[3].Attribute("dx")?.Value),
                OffsetY = SvgParser.Length(primitives[3].Attribute("dy")?.Value),
            }, null, filter, false);
        }

        return default;
    }

    private static double Sigma(XElement blur) =>
        SvgParser.Numbers(blur.Attribute("stdDeviation")?.Value) is { Count: > 0 } s ? s.Average() : 0;

    private static double BlurRadius(double sigma) => Math.Max(0, (sigma - 0.5) / 0.288675);

    private static SvgColorMap? ColorMatrix(XElement primitive)
    {
        var values = SvgParser.Numbers(primitive.Attribute("values")?.Value);
        double[] m;
        switch (primitive.Attribute("type")?.Value ?? "matrix")
        {
            case "matrix" when values.Count == 20:
                m = [.. values];
                break;
            case "saturate":
                var s = values.Count > 0 ? values[0] : 1;
                m =
                [
                    0.213 + 0.787 * s, 0.715 - 0.715 * s, 0.072 - 0.072 * s, 0, 0,
                    0.213 - 0.213 * s, 0.715 + 0.285 * s, 0.072 - 0.072 * s, 0, 0,
                    0.213 - 0.213 * s, 0.715 - 0.715 * s, 0.072 + 0.928 * s, 0, 0,
                    0, 0, 0, 1, 0,
                ];
                break;
            case "hueRotate":
                var a = (values.Count > 0 ? values[0] : 0) * Math.PI / 180;
                var cos = Math.Cos(a);
                var sin = Math.Sin(a);
                m =
                [
                    0.213 + cos * 0.787 - sin * 0.213, 0.715 - cos * 0.715 - sin * 0.715, 0.072 - cos * 0.072 + sin * 0.928, 0, 0,
                    0.213 - cos * 0.213 + sin * 0.143, 0.715 + cos * 0.285 + sin * 0.140, 0.072 - cos * 0.072 - sin * 0.283, 0, 0,
                    0.213 - cos * 0.213 - sin * 0.787, 0.715 - cos * 0.715 + sin * 0.715, 0.072 + cos * 0.928 + sin * 0.072, 0, 0,
                    0, 0, 0, 1, 0,
                ];
                break;
            case "luminanceToAlpha":
                m = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.2125, 0.7154, 0.0721, 0, 0];
                break;
            default:
                return null;
        }

        return color =>
        {
            Span<double> v = [color.R / 255.0, color.G / 255.0, color.B / 255.0, color.A / 255.0];
            Span<byte> o = stackalloc byte[4];
            for (var row = 0; row < 4; row++)
            {
                var sum = m[row * 5 + 4];
                for (var col = 0; col < 4; col++)
                {
                    sum += m[row * 5 + col] * v[col];
                }

                o[row] = (byte)Math.Clamp(Math.Round(sum * 255), 0, 255);
            }

            return Color.FromArgb(o[3], o[0], o[1], o[2]);
        };
    }

    private static SvgColorMap? Compose(SvgColorMap? first, SvgColorMap? second) =>
        first is null ? second : second is null ? first : c => second(first(c));

    private static Rect Bounds(IEnumerable<SvgNode> nodes)
    {
        Rect? result = null;
        foreach (var node in nodes)
        {
            if (Bounds(node, Matrix.Identity) is { } b)
            {
                result = result is { } r ? r.Union(b) : b;
            }
        }

        return result ?? default;
    }

    private static Rect? Bounds(SvgNode node, Matrix matrix)
    {
        switch (node)
        {
            case SvgShape shape:
                return shape.Geometry.Bounds.TransformToAABB(matrix);
            case SvgGroup group:
                Rect? result = null;
                var inner = group.Transform * matrix;
                foreach (var child in group.Children)
                {
                    if (Bounds(child, inner) is { } b)
                    {
                        result = result is { } r ? r.Union(b) : b;
                    }
                }

                return result;
            default:
                return null;
        }
    }

    private XElement? Lookup(string id, params string[] kinds)
    {
        if (!_ids.TryGetValue(id, out var list))
        {
            return null;
        }

        return kinds.Length == 0 ? list[0] : list.Find(e => kinds.Contains(e.Name.LocalName));
    }

    private XElement? Reference(XElement element, params string[] kinds)
    {
        var href = element.Attribute(s_xlink + "href")?.Value ?? element.Attribute("href")?.Value;
        return href is { Length: > 1 } && href[0] == '#' ? Lookup(href[1..], kinds) : null;
    }

    private XElement? Target(string reference, string kind)
    {
        var value = reference.Trim();
        if (!value.StartsWith("url(", StringComparison.Ordinal))
        {
            return null;
        }

        var close = value.IndexOf(')');
        var id = close < 0 ? null : value[4..close].Trim().Trim('\'', '"');
        return id is { Length: > 1 } && id[0] == '#' ? Lookup(id[1..], kind) : null;
    }

    private Style DomStyle(XElement element)
    {
        if (_domStyles.TryGetValue(element, out var style))
        {
            return style;
        }

        var parent = element.Parent is { } p ? DomStyle(p) : Style.Initial;
        style = Cascade(element, parent);
        _domStyles[element] = style;
        return style;
    }

    private Style Cascade(XElement element, Style parent)
    {
        var style = parent with
        {
            Opacity = 1,
            Hidden = false,
            Clip = null,
            Mask = null,
            Filter = null,
            StopColor = "black",
            StopOpacity = 1,
            Extra = null,
        };

        var classes = element.Attribute("class")?.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? [];
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (var attribute in element.Attributes())
        {
            if (attribute.Name.Namespace == XNamespace.None && attribute.Name.LocalName is not ("style" or "class" or "transform" or "id"))
            {
                style = Apply(style, parent, attribute.Name.LocalName, attribute.Value);
                declared.Add(attribute.Name.LocalName);
            }
        }

        var id = element.Attribute("id")?.Value;
        foreach (var rule in _rules.Where(r => r.Matches(element.Name.LocalName, id, classes)).OrderBy(r => r.Specificity).ThenBy(r => r.Order))
        {
            foreach (var (name, value) in rule.Declarations)
            {
                style = Apply(style, parent, name, value);
                declared.Add(name);
                if (name == "color" && rule.Classes.Length > 0)
                {
                    style = style with { Color = style.Color with { Tinted = rule.Classes.Contains(TextClass) } };
                }
            }
        }

        if (element.Attribute("style")?.Value is { } inline)
        {
            foreach (var (name, value) in Declarations(inline))
            {
                style = Apply(style, parent, name, value);
                declared.Add(name);
            }
        }

        if (classes.Contains(TextClass))
        {
            style = style with { Color = style.Color with { Tinted = true } };
        }

        if (declared.Contains("fill"))
        {
            style = style with { FillColor = style.Color };
        }

        if (declared.Contains("stroke"))
        {
            style = style with { StrokeColor = style.Color };
        }

        return style;
    }

    private static Style Apply(Style style, Style parent, string name, string value)
    {
        value = value.Trim();
        if (value.EndsWith("!important", StringComparison.Ordinal))
        {
            value = value[..^"!important".Length].Trim();
        }

        if (value == "inherit")
        {
            return name switch
            {
                "opacity" => style with { Opacity = parent.Opacity },
                "stop-color" => style with { StopColor = parent.StopColor },
                "stop-opacity" => style with { StopOpacity = parent.StopOpacity },
                _ => style,
            };
        }

        double Number(double fallback) => double.TryParse(value.TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
            ? Math.Clamp(value.EndsWith('%') ? n / 100 : n, 0, 1)
            : fallback;

        return name switch
        {
            "fill" => ValidPaint(value) ? style with { Fill = value } : style,
            "stroke" => ValidPaint(value) ? style with { Stroke = value } : style,
            "fill-opacity" => style with { FillOpacity = Number(style.FillOpacity) },
            "stroke-opacity" => style with { StrokeOpacity = Number(style.StrokeOpacity) },
            "opacity" => style with { Opacity = Number(1) },
            "fill-rule" => style with { FillRule = value == "evenodd" ? FillRule.EvenOdd : FillRule.NonZero },
            "clip-rule" => style with { ClipRule = value == "evenodd" ? FillRule.EvenOdd : FillRule.NonZero },
            "stroke-width" => style with { StrokeWidth = Math.Max(0, SvgParser.Length(value, 0, style.StrokeWidth, style.FontSize)) },
            "stroke-linecap" => style with { LineCap = value switch { "round" => PenLineCap.Round, "square" => PenLineCap.Square, _ => PenLineCap.Flat } },
            "stroke-linejoin" => style with { LineJoin = value switch { "round" => PenLineJoin.Round, "bevel" => PenLineJoin.Bevel, _ => PenLineJoin.Miter } },
            "stroke-miterlimit" => style with { MiterLimit = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ml) && ml >= 1 ? ml : style.MiterLimit },
            "stroke-dasharray" => style with { Dashes = value == "none" ? null : SvgParser.Numbers(value) },
            "stroke-dashoffset" => style with { DashOffset = SvgParser.Length(value) },
            "color" => SvgParser.TryColor(value, out var color) ? style with { Color = style.Color with { Color = color } } : style,
            "display" => style with { Hidden = value == "none" },
            "visibility" => style with { Visible = value is not ("hidden" or "collapse") },
            "clip-path" => style with { Clip = value == "none" ? null : value },
            "mask" => style with { Mask = value == "none" ? null : value },
            "filter" => style with { Filter = value == "none" ? null : value },
            "stop-color" => style with { StopColor = value },
            "stop-opacity" => style with { StopOpacity = Number(1) },
            "paint-order" => style with { StrokeFirst = PaintOrderStrokeFirst(value) },
            "font-family" => style with { FontFamily = value.Replace("'", string.Empty, StringComparison.Ordinal).Replace("\"", string.Empty, StringComparison.Ordinal) },
            "font-size" => style with { FontSize = SvgParser.Length(value, parent.FontSize, style.FontSize, parent.FontSize) },
            "font-weight" => style with { FontWeight = value switch { "bold" => FontWeight.Bold, "normal" => FontWeight.Normal, _ => int.TryParse(value, out var w) ? (FontWeight)w : style.FontWeight } },
            "font-style" => style with { FontStyle = value is "italic" or "oblique" ? FontStyle.Italic : FontStyle.Normal },
            "text-anchor" => style with { TextAnchor = value },
            "flood-color" or "flood-opacity" => style with { Extra = (style.Extra ?? []).Append((name, value)).ToArray() },
            _ => style,
        };
    }

    private static bool ValidPaint(string value) =>
        value.StartsWith("url(", StringComparison.Ordinal) || value is "none" || value.Equals("currentColor", StringComparison.OrdinalIgnoreCase) || SvgParser.TryColor(value, out _);

    private static bool PaintOrderStrokeFirst(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var stroke = Array.IndexOf(parts, "stroke");
        var fill = Array.IndexOf(parts, "fill");
        return stroke >= 0 && (fill < 0 || stroke < fill);
    }

    private static IEnumerable<(string Name, string Value)> Declarations(string text)
    {
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = part.IndexOf(':');
            if (colon > 0)
            {
                yield return (part[..colon].Trim(), part[(colon + 1)..].Trim());
            }
        }
    }

    private void ParseCss(string css)
    {
        css = CssComment().Replace(css, string.Empty);
        foreach (Match match in CssRule().Matches(css))
        {
            var declarations = Declarations(match.Groups[2].Value).ToArray();
            foreach (var selector in match.Groups[1].Value.Split(','))
            {
                if (Rule.Parse(selector.Trim(), declarations, _rules.Count) is { } rule)
                {
                    _rules.Add(rule);
                }
            }
        }
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex CssComment();

    [GeneratedRegex(@"([^{}@]+)\{([^}]*)\}")]
    private static partial Regex CssRule();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    private sealed record Rule(string? Tag, string? Id, string[] Classes, (string Name, string Value)[] Declarations, int Specificity, int Order)
    {
        public static Rule? Parse(string selector, (string, string)[] declarations, int order)
        {
            if (selector.Length == 0 || selector.Any(c => char.IsWhiteSpace(c) || c is '>' or '+' or '~' or '[' or ':'))
            {
                return null;
            }

            string? tag = null, id = null;
            var classes = new List<string>();
            foreach (Match part in SelectorPart().Matches(selector))
            {
                var text = part.Value;
                switch (text[0])
                {
                    case '.':
                        classes.Add(text[1..]);
                        break;
                    case '#':
                        id = text[1..];
                        break;
                    case '*':
                        break;
                    default:
                        tag = text;
                        break;
                }
            }

            return new Rule(tag, id, [.. classes], declarations, (id is null ? 0 : 100) + classes.Count * 10 + (tag is null ? 0 : 1), order);
        }

        public bool Matches(string tag, string? id, string[] classes) =>
            (Tag is null || Tag == tag) && (Id is null || Id == id) && Classes.All(classes.Contains);
    }

    [GeneratedRegex(@"[.#]?[A-Za-z0-9_\-]+|\*")]
    private static partial Regex SelectorPart();

    private sealed record Style
    {
        public static Style Initial { get; } = new();

        public string? Fill { get; init; } = "black";

        public string? Stroke { get; init; } = "none";

        public double FillOpacity { get; init; } = 1;

        public double StrokeOpacity { get; init; } = 1;

        public double Opacity { get; init; } = 1;

        public FillRule FillRule { get; init; } = FillRule.NonZero;

        public FillRule ClipRule { get; init; } = FillRule.NonZero;

        public double StrokeWidth { get; init; } = 1;

        public PenLineCap LineCap { get; init; } = PenLineCap.Flat;

        public PenLineJoin LineJoin { get; init; } = PenLineJoin.Miter;

        public double MiterLimit { get; init; } = 4;

        public IReadOnlyList<double>? Dashes { get; init; }

        public double DashOffset { get; init; }

        public SvgColor Color { get; init; } = new(Colors.Black, true);

        public SvgColor FillColor { get; init; } = new(Colors.Black, true);

        public SvgColor StrokeColor { get; init; } = new(Colors.Black, true);

        public bool Hidden { get; init; }

        public bool Visible { get; init; } = true;

        public string? Clip { get; init; }

        public string? Mask { get; init; }

        public string? Filter { get; init; }

        public string? StopColor { get; init; } = "black";

        public double StopOpacity { get; init; } = 1;

        public bool StrokeFirst { get; init; }

        public string FontFamily { get; init; } = "sans-serif";

        public double FontSize { get; init; } = 16;

        public FontWeight FontWeight { get; init; } = FontWeight.Normal;

        public FontStyle FontStyle { get; init; } = FontStyle.Normal;

        public string TextAnchor { get; init; } = "start";

        public (string Name, string Value)[]? Extra { get; init; }

        public string? Get(string name) => Extra?.LastOrDefault(e => e.Name == name).Value;
    }
}
