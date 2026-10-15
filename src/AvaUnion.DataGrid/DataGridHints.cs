using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using AvaUnion.Elements;

namespace AvaUnion.DataGrid;

internal static class DataGridHints
{
    private static int s_registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref s_registered, 1) == 1)
        {
            return;
        }

        UnionHintProviders.MapPseudoClass<DataGridRow>(":selected", "selected");
        UnionHintProviders.MapPseudoClass<DataGridRow>(":editing", "editing");
        UnionHintProviders.MapPseudoClass<DataGridRowHeader>(":selected", "selected");
        UnionHintProviders.MapPseudoClass<DataGridRowHeader>(":editing", "editing");
        UnionHintProviders.MapPseudoClass<DataGridRowHeader>(":current", "current");
        UnionHintProviders.MapPseudoClass<DataGridCell>(":selected", "selected");
        UnionHintProviders.MapPseudoClass<DataGridCell>(":current", "current");
        UnionHintProviders.MapPseudoClass<DataGridCell>(":edited", "editing");
        UnionHintProviders.MapPseudoClass<DataGridColumnHeader>(":sortascending", "sort-ascending");
        UnionHintProviders.MapPseudoClass<DataGridColumnHeader>(":sortdescending", "sort-descending");
        UnionHintProviders.MapPseudoClass<DataGridRowGroupHeader>(":current", "current");

        UnionHintProviders.Register<DataGridRow>(
            static (element, _, hints, _) =>
            {
                var row = (DataGridRow)element;
                var grid = row.FindAncestorOfType<Avalonia.Controls.DataGrid>();
                if (grid is not null && !grid.Classes.Contains("no-alternating-colors") && row.Index % 2 == 1 && !hints.Contains("alternating-colors"))
                {
                    hints.Add("alternating-colors");
                }
            },
            DataGridRow.IndexProperty);
    }
}
