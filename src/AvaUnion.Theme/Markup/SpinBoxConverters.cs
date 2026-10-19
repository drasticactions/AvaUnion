using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace AvaUnion.Markup;

internal static class SpinBoxConverters
{
    public static readonly IMultiValueConverter LimitTextWidth = new FuncMultiValueConverter<object?, double>(values =>
        values.OfType<NumericUpDown>().FirstOrDefault() is { } spin ? Math.Ceiling(Elements.AutoHints.LimitTextWidth(spin)) : 0.0);
}
