using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class CellGrid : Panel
{
    public static readonly StyledProperty<int> RowsProperty = AvaloniaProperty.Register<CellGrid, int>(nameof(Rows), 1);
    public static readonly StyledProperty<int> ColumnsProperty = AvaloniaProperty.Register<CellGrid, int>(nameof(Columns), 1);
    public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<CellGrid, double>(nameof(Spacing));

    static CellGrid() => AffectsMeasure<CellGrid>(RowsProperty, ColumnsProperty, SpacingProperty);

    public int Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var cell = new Size(0, 0);
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            cell = new Size(Math.Max(cell.Width, child.DesiredSize.Width), Math.Max(cell.Height, child.DesiredSize.Height));
        }

        return new Size((cell.Width * Columns) + (Spacing * (Columns - 1)), (cell.Height * Rows) + (Spacing * (Rows - 1)));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = (finalSize.Width - (Spacing * (Columns - 1))) / Columns;
        var height = (finalSize.Height - (Spacing * (Rows - 1))) / Rows;
        for (var i = 0; i < Children.Count; i++)
        {
            var row = i / Columns;
            var column = i % Columns;
            Children[i].Arrange(new Rect(column * (width + Spacing), row * (height + Spacing), width, height));
        }

        return finalSize;
    }
}
