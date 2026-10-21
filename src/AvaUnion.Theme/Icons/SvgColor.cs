using Avalonia.Media;

namespace AvaUnion.Icons;

internal readonly record struct SvgColor(Color Color, bool Tinted, SvgColorMap? Map = null)
{
    public Color Resolve(Color? tint)
    {
        var color = Tinted && tint is { } t ? t : Color;
        return Map is null ? color : Map(color);
    }

    public SvgColor With(SvgColorMap? map)
    {
        if (map is null)
        {
            return this;
        }

        var inner = Map;
        return this with { Map = inner is null ? map : c => map(inner(c)) };
    }
}
