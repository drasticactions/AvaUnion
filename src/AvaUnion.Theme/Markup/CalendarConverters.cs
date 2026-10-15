using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaUnion.Markup;

internal static class CalendarConverters
{
    public static readonly IValueConverter DayName = new FuncValueConverter<string?, string?>(shortest =>
    {
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        var index = Array.IndexOf(format.ShortestDayNames, shortest);
        return index >= 0 && Array.LastIndexOf(format.ShortestDayNames, shortest) == index
            ? format.AbbreviatedDayNames[index]
            : shortest;
    });
}
