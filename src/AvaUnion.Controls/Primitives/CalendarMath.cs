using System;
using System.Globalization;

namespace AvaUnion.Controls.Primitives;

internal static class CalendarMath
{
    public static DateOnly FirstVisibleDate(int year, int month, DayOfWeek firstDayOfWeek)
    {
        var first = new DateOnly(year, Math.Clamp(month, 1, 12), 1);
        var back = ((int)first.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        return first.AddDays(-back);
    }

    public static int WeekNumber(DateOnly date) => ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));

    public static DayOfWeek CultureFirstDay => CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
}
