using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace AvaUnion.DataGrid;

internal static class GridLineConverters
{
    public static readonly IValueConverter ShowsHorizontal = new FuncValueConverter<DataGridGridLinesVisibility, bool>(
        static v => v is DataGridGridLinesVisibility.Horizontal or DataGridGridLinesVisibility.All);
}
