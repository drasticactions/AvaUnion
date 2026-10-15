using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Converters;

namespace AvaUnion.Markup;

internal static class TabStripConverters
{
    private const double Tolerance = 0.5;

    public static readonly IMultiValueConverter Overflows = Converter((offset, extent, viewport) => extent - viewport > Tolerance);

    public static readonly IMultiValueConverter CanScrollBack = Converter((offset, extent, viewport) => offset > Tolerance);

    public static readonly IMultiValueConverter CanScrollForward = Converter((offset, extent, viewport) => offset < extent - viewport - Tolerance);

    private static IMultiValueConverter Converter(Func<double, double, double, bool> test) =>
        new FuncMultiValueConverter<object?, bool>(values =>
        {
            var numbers = values.OfType<double>().ToList();
            return numbers.Count == 3 && test(numbers[0], numbers[1], numbers[2]);
        });
}
