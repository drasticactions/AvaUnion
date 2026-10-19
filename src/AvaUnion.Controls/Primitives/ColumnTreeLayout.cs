using System.Collections.Generic;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal static class ColumnTreeLayout
{
    public static void Apply(Grid grid, IReadOnlyList<ColumnTreeViewColumn> columns)
    {
        grid.ColumnDefinitions.Clear();
        for (var i = 0; i < columns.Count; i++)
        {
            var width = columns[i].Width;
            var definition = new ColumnDefinition(width);
            if (!width.IsStar)
            {
                definition.SharedSizeGroup = "UnionTreeColumn" + i;
            }

            grid.ColumnDefinitions.Add(definition);
        }
    }
}
