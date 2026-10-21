using Avalonia.Media;

namespace AvaUnion.Icons;

internal abstract class SvgPaint
{
    public abstract IBrush? Brush(Color? tint, double opacity);
}
