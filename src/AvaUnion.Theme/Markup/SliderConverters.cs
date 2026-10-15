using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaUnion.Markup;

internal static class SliderConverters
{
    public static readonly IMultiValueConverter HorizontalFillReach = new FuncMultiValueConverter<object?, Thickness>(values =>
    {
        var (half, reversed) = Read(values);
        return reversed ? new Thickness(-half, 0, 0, 0) : new Thickness(0, 0, -half, 0);
    });

    public static readonly IMultiValueConverter VerticalFillReach = new FuncMultiValueConverter<object?, Thickness>(values =>
    {
        var (half, reversed) = Read(values);
        return reversed ? new Thickness(0, 0, 0, -half) : new Thickness(0, -half, 0, 0);
    });

    private static (double Half, bool Reversed) Read(IEnumerable<object?> values)
    {
        double length = 0;
        var reversed = false;
        foreach (var value in values)
        {
            if (value is double d)
                length = d;
            else if (value is bool b)
                reversed = b;
        }
        return (length / 2, reversed);
    }
}
