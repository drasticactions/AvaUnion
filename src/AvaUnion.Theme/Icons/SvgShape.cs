using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaUnion.Icons;

internal sealed class SvgShape : SvgNode
{
    public required Geometry Geometry { get; init; }

    public SvgPaint? Fill { get; init; }

    public double FillOpacity { get; set; } = 1;

    public SvgPaint? Stroke { get; init; }

    public double StrokeOpacity { get; set; } = 1;

    public double StrokeWidth { get; init; } = 1;

    public PenLineCap LineCap { get; init; }

    public PenLineJoin LineJoin { get; init; }

    public double MiterLimit { get; init; } = 4;

    public IReadOnlyList<double>? Dashes { get; init; }

    public double DashOffset { get; init; }

    public bool StrokeFirst { get; init; }

    public override Drawing? Build(Color? tint)
    {
        var fill = Fill?.Brush(tint, FillOpacity);
        IPen? pen = null;
        if (Stroke?.Brush(tint, StrokeOpacity) is { } strokeBrush)
        {
            var dash = Dashes is null ? null : new ImmutableDashStyle(Array.ConvertAll([.. Dashes], d => d / StrokeWidth), DashOffset / StrokeWidth);
            pen = new Pen(strokeBrush, StrokeWidth, dash, LineCap, LineJoin, MiterLimit);
        }

        if (fill is null && pen is null)
        {
            return null;
        }

        if (fill is not null && pen is not null && StrokeFirst)
        {
            var group = new DrawingGroup();
            group.Children.Add(new GeometryDrawing { Geometry = Geometry, Pen = pen });
            group.Children.Add(new GeometryDrawing { Geometry = Geometry, Brush = fill });
            return group;
        }

        return new GeometryDrawing { Geometry = Geometry, Brush = fill, Pen = pen };
    }
}
