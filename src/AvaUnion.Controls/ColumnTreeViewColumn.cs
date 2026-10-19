using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace AvaUnion.Controls;

/// <summary>A column of a <see cref="ColumnTreeView"/>.</summary>
public class ColumnTreeViewColumn : AvaloniaObject
{
    /// <summary>Defines the <see cref="Header"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<ColumnTreeViewColumn, object?>(nameof(Header));

    /// <summary>Defines the <see cref="CellTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<ColumnTreeViewColumn, IDataTemplate?>(nameof(CellTemplate));

    /// <summary>Defines the <see cref="Width"/> property.</summary>
    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<ColumnTreeViewColumn, GridLength>(nameof(Width), GridLength.Auto);

    /// <summary>The content of the column header.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The binding of the cell text to the item of the row. It is used when <see cref="CellTemplate"/> is null.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ColumnTreeView.ItemsSource), AncestorType = typeof(ColumnTreeView))]
    public BindingBase? Binding { get; set; }

    /// <summary>The template of the cells. It gets the item of the row as its data.</summary>
    public IDataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// The width of the column. Auto, the default, fits the widest cell of the
    /// rows that show. Star sizes share the space that is left.
    /// </summary>
    public GridLength Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }
}
