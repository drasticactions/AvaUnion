using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal sealed class SvgGradientPaint : SvgPaint
{
    public required bool Radial { get; init; }

    public required Point Start { get; init; }

    public required Point End { get; init; }

    public double Radius { get; init; }

    public required Matrix Transform { get; init; }

    public required GradientSpreadMethod Spread { get; init; }

    public required IReadOnlyList<(double Offset, SvgColor Color, double Opacity)> Stops { get; init; }

    public override IBrush Brush(Color? tint, double opacity)
    {
        var stops = new GradientStops();
        foreach (var (offset, color, stopOpacity) in Stops)
        {
            var c = color.Resolve(tint);
            stops.Add(new GradientStop(Color.FromArgb((byte)Math.Clamp(Math.Round(c.A * stopOpacity), 0, 255), c.R, c.G, c.B), offset));
        }

        GradientBrush brush = Radial
            ? new RadialGradientBrush
            {
                Center = new RelativePoint(End, RelativeUnit.Absolute),
                GradientOrigin = new RelativePoint(Start, RelativeUnit.Absolute),
                RadiusX = new RelativeScalar(Radius, RelativeUnit.Absolute),
                RadiusY = new RelativeScalar(Radius, RelativeUnit.Absolute),
            }
            : new LinearGradientBrush
            {
                StartPoint = new RelativePoint(Start, RelativeUnit.Absolute),
                EndPoint = new RelativePoint(End, RelativeUnit.Absolute),
            };
        brush.GradientStops = stops;
        brush.SpreadMethod = Spread;
        brush.Opacity = opacity;
        if (!Transform.IsIdentity)
        {
            brush.Transform = new MatrixTransform(Transform);
            brush.TransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
        }

        return brush;
    }
}
