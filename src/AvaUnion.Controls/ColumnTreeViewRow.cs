using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using AvaUnion.Controls.Primitives;
using AvaUnion.Elements;
using AvaUnion.Presenters;

namespace AvaUnion.Controls;

/// <summary>A row of a <see cref="ColumnTreeView"/>.</summary>
[TemplatePart("PART_Cells", typeof(Grid))]
public class ColumnTreeViewRow : TemplatedControl
{
    /// <summary>Defines the <see cref="Level"/> property.</summary>
    public static readonly DirectProperty<ColumnTreeViewRow, int> LevelProperty =
        AvaloniaProperty.RegisterDirect<ColumnTreeViewRow, int>(nameof(Level), r => r.Level);

    /// <summary>Defines the <see cref="IsExpanded"/> property.</summary>
    public static readonly DirectProperty<ColumnTreeViewRow, bool> IsExpandedProperty =
        AvaloniaProperty.RegisterDirect<ColumnTreeViewRow, bool>(nameof(IsExpanded), r => r.IsExpanded);

    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly DirectProperty<ColumnTreeViewRow, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<ColumnTreeViewRow, bool>(nameof(IsSelected), r => r.IsSelected);

    /// <summary>Defines the <see cref="Index"/> property.</summary>
    public static readonly DirectProperty<ColumnTreeViewRow, int> IndexProperty =
        AvaloniaProperty.RegisterDirect<ColumnTreeViewRow, int>(nameof(Index), r => r.Index);

    private ColumnTreeNode? _node;
    private Grid? _cells;
    private ToggleButton? _indicator;
    private int _level;
    private bool _isExpanded;
    private bool _isSelected;
    private int _index;

    static ColumnTreeViewRow()
    {
        UnionHintProviders.Register<ColumnTreeViewRow>(
            static (element, _, hints, _) =>
            {
                var row = (ColumnTreeViewRow)element;
                hints.Add("hover-enabled");
                if (row.IsExpanded)
                {
                    hints.Add("expanded");
                }

                if (row.IsSelected)
                {
                    hints.Add("selected");
                }

                if (row.Index % 2 == 1)
                {
                    hints.Add("alternating-colors");
                }
            },
            IsExpandedProperty,
            IsSelectedProperty,
            IndexProperty);
    }

    /// <summary>The item of the row.</summary>
    public object? Item => _node?.Item;

    /// <summary>True when the row has children.</summary>
    public bool HasChildren => _node?.HasChildren ?? false;

    /// <summary>The nesting level, 0 for top-level rows.</summary>
    public int Level
    {
        get => _level;
        private set => SetAndRaise(LevelProperty, ref _level, value);
    }

    /// <summary>True when the children of the row show.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        private set => SetAndRaise(IsExpandedProperty, ref _isExpanded, value);
    }

    /// <summary>True when the row is the selected row.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        private set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
    }

    /// <summary>The position of the row among the rows that show, from 0.</summary>
    public int Index
    {
        get => _index;
        private set => SetAndRaise(IndexProperty, ref _index, value);
    }

    /// <summary>Expands or collapses the row.</summary>
    public void Toggle()
    {
        if (_node is { HasChildren: true } node)
        {
            node.Owner.SetExpanded(node.Item, !node.IsExpanded);
        }
    }

    internal void Attach(ColumnTreeNode node)
    {
        Detach();
        _node = node;
        node.Changed += Update;
        Level = node.Level;
        Update();
        BuildCells();
    }

    internal void Detach()
    {
        if (_node is not null)
        {
            _node.Changed -= Update;
            _node = null;
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _cells = e.NameScope.Find<Grid>("PART_Cells");
        BuildCells();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PaddingProperty)
        {
            BuildCells();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_node is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _node.Owner.Select(_node);
        if (e.ClickCount == 2)
        {
            Toggle();
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ColumnTreeViewRowAutomationPeer(this);

    private void Update()
    {
        if (_node is null)
        {
            return;
        }

        IsExpanded = _node.IsExpanded;
        IsSelected = _node.IsSelected;
        Index = _node.Index;
        if (_indicator is not null)
        {
            _indicator.IsChecked = _node.IsExpanded;
        }
    }

    private void BuildCells()
    {
        if (_cells is null || _node is null)
        {
            return;
        }

        var columns = _node.Owner.Columns;
        ColumnTreeLayout.Apply(_cells, columns);
        _cells.Children.Clear();
        var gap = Padding.Left + Padding.Right;
        for (var i = 0; i < columns.Count; i++)
        {
            var cell = Cell(columns[i], _node.Item);
            cell.Margin = new Thickness(0, 0, i < columns.Count - 1 ? gap : 0, 0);
            if (i == 0)
            {
                var first = new StackPanel { Orientation = Orientation.Horizontal };
                var spacer = new IndentationSpacer { Level = _node.Level };
                Union.SetType(spacer, "indentation");
                Union.SetStateSource(spacer, UnionStateSource.None);
                first.Children.Add(spacer);
                _indicator = new ToggleButton { IsVisible = _node.HasChildren, IsChecked = _node.IsExpanded, VerticalAlignment = VerticalAlignment.Center };
                _indicator.Bind(ThemeProperty, this.GetResourceObservable("UnionTreeIndicator"));
                _indicator.IsCheckedChanged += (_, _) =>
                {
                    if (_node is { } node && _indicator.IsChecked != node.IsExpanded)
                    {
                        node.Owner.SetExpanded(node.Item, _indicator.IsChecked == true);
                    }
                };
                first.Children.Add(_indicator);
                first.Children.Add(cell);
                cell = first;
            }

            Grid.SetColumn(cell, i);
            _cells.Children.Add(cell);
        }
    }

    private static Control Cell(ColumnTreeViewColumn column, object item)
    {
        if (column.CellTemplate is { } template)
        {
            return new ContentControl { Content = item, ContentTemplate = template, VerticalAlignment = VerticalAlignment.Center };
        }

        var text = new TextBlock { DataContext = item, VerticalAlignment = VerticalAlignment.Center, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
        if (column.Binding is { } binding)
        {
            text.Bind(TextBlock.TextProperty, binding);
        }

        return text;
    }
}
