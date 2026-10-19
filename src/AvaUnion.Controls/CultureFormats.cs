using System.Globalization;
using System.Linq;

namespace AvaUnion.Controls;

internal static class CultureFormats
{
    public static string ShortDate(CultureInfo culture)
    {
        var patterns = culture.DateTimeFormat.GetAllDateTimePatterns('d');
        var first = culture.DateTimeFormat.ShortDatePattern;
        var twoDigitYear = first.Replace("yyyy", "yy");
        return twoDigitYear != first && patterns.Contains(twoDigitYear) ? twoDigitYear : first;
    }

    public static string ShortTime(CultureInfo culture) => culture.DateTimeFormat.ShortTimePattern;
}
