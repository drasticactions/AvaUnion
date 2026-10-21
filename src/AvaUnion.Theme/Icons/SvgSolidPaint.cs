using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaUnion.Icons;

internal sealed class SvgSolidPaint(SvgColor color) : SvgPaint
{
    public SvgColor Color { get; } = color;

    public override IBrush Brush(Color? tint, double opacity)
    {
        var c = Color.Resolve(tint);
        return new ImmutableSolidColorBrush(Avalonia.Media.Color.FromArgb((byte)Math.Clamp(Math.Round(c.A * opacity), 0, 255), c.R, c.G, c.B));
    }
}
