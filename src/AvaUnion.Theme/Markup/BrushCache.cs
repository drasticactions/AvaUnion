using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaUnion.Markup;

/// <summary>Immutable solid brushes, one for each ARGB value.</summary>
public static class BrushCache
{
    private static readonly Dictionary<uint, ImmutableSolidColorBrush> s_brushes = [];

    /// <summary>The brush for a color.</summary>
    public static ImmutableSolidColorBrush Get(Color color)
    {
        var key = color.ToUInt32();
        if (!s_brushes.TryGetValue(key, out var brush))
        {
            brush = new ImmutableSolidColorBrush(color);
            s_brushes[key] = brush;
        }

        return brush;
    }
}
