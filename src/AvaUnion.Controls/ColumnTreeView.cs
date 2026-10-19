using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>
/// A tree whose rows have columns, with an optional header. Each item of
/// <see cref="ItemsSource"/> is a top-level row, and
/// <see cref="ChildrenSelector"/> gives the children of a row. The children
/// are read when the row expands. The user selects rows with the pointer or
/// the Up and Down keys, and expands them with the indicator or the Left and
/// Right keys.
/// </summary>
[TemplatePart("PART_Header", typeof(Grid))]
[TemplatePart("PART_Rows", typeof(ItemsControl))]
public class ColumnTreeView : TemplatedControl
{
    /// <summary>Defines the <see cref="ItemsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ColumnTreeView, IEnumerable?>(nameof(ItemsSource));

    /// <summary>Defines the <see cref="ChildrenSelector"/> property.</summary>
    public static readonly StyledProperty<Func<object, IEnumerable?>?> ChildrenSelectorProperty =
        AvaloniaProperty.Register<ColumnTreeView, Func<object, IEnumerable?>?>(nameof(ChildrenSelector));

    /// <summary>Defines the <see cref="IsHeaderVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<ColumnTreeView, bool>(nameof(IsHeaderVisible), true);

    /// <summary>Defines the <see cref="SelectedItem"/> property.</summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ColumnTreeView, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    private readonly AvaloniaList<ColumnTreeNode> _rows = [];
    private readonly Dictionary<object, ColumnTreeNode> _nodes = new(ReferenceEqualityComparer.Instance);
    private Grid? _header;
    private ItemsControl? _rowsControl;

    static ColumnTreeView()
    {
        FocusableProperty.OverrideDefaultValue<ColumnTreeView>(true);
    }

    /// <summary>Creates the tree.</summary>
    public ColumnTreeView() => Columns.CollectionChanged += (_, _) => Rebuild();

    /// <summary>The columns.</summary>
    public AvaloniaList<ColumnTreeViewColumn> Columns { get; } = [];

    /// <summary>The top-level items.</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gives the children of an item, or null when it has none.</summary>
    public Func<object, IEnumerable?>? ChildrenSelector
    {
        get => GetValue(ChildrenSelectorProperty);
        set => SetValue(ChildrenSelectorProperty, value);
    }

    /// <summary>True when the column headers show. The default is true.</summary>
    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    /// <summary>The item of the selected row, or null.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>The rows that show now, from the top.</summary>
    public IReadOnlyList<object> VisibleItems
    {
        get
        {
            var items = new List<object>(_rows.Count);
            foreach (var row in _rows)
            {
                items.Add(row.Item);
            }

            return items;
        }
    }

    internal IReadOnlyList<ColumnTreeNode> Rows => _rows;

    /// <summary>Expands the row of <paramref name="item"/>. Its parent rows must be expanded for it to show.</summary>
    public void Expand(object item) => SetExpanded(item, true);

    /// <summary>Collapses the row of <paramref name="item"/>.</summary>
    public void Collapse(object item) => SetExpanded(item, false);

    /// <summary>True when the row of <paramref name="item"/> is expanded.</summary>
    public bool IsExpanded(object item) => _nodes.TryGetValue(item, out var node) && node.IsExpanded;

    /// <summary>Expands every row, including rows that do not show yet.</summary>
    public void ExpandAll()
    {
        var pending = new Stack<ColumnTreeNode>();
        foreach (var root in Roots())
        {
            pending.Push(root);
        }

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!node.HasChildren)
            {
                continue;
            }

            node.IsExpanded = true;
            foreach (var child in Children(node))
            {
                pending.Push(child);
            }
        }

        Flatten();
    }

    /// <summary>Collapses every row.</summary>
    public void CollapseAll()
    {
        foreach (var node in _nodes.Values)
        {
            node.IsExpanded = false;
        }

        Flatten();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _header = e.NameScope.Find<Grid>("PART_Header");
        _rowsControl = e.NameScope.Find<ItemsControl>("PART_Rows");
        if (_rowsControl is not null)
        {
            _rowsControl.ItemsSource = _rows;
        }

        Rebuild();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty || change.Property == ChildrenSelectorProperty)
        {
            _nodes.Clear();
            Flatten();
        }
        else if (change.Property == SelectedItemProperty)
        {
            foreach (var row in _rows)
            {
                row.IsSelected = ReferenceEquals(row.Item, SelectedItem);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var index = IndexOfSelected();
        switch (e.Key)
        {
            case Key.Down when _rows.Count > 0:
                Select(Math.Min(index + 1, _rows.Count - 1));
                e.Handled = true;
                break;
            case Key.Up when _rows.Count > 0:
                Select(Math.Max(index - 1, 0));
                e.Handled = true;
                break;
            case Key.Right when index >= 0:
                var node = _rows[index];
                if (node.HasChildren && !node.IsExpanded)
                {
                    SetExpanded(node.Item, true);
                }
                else if (node.IsExpanded && index + 1 < _rows.Count)
                {
                    Select(index + 1);
                }

                e.Handled = true;
                break;
            case Key.Left when index >= 0:
                var current = _rows[index];
                if (current.IsExpanded)
                {
                    SetExpanded(current.Item, false);
                }
                else if (current.Parent is { } parent)
                {
                    SelectedItem = parent.Item;
                }

                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ColumnTreeViewAutomationPeer(this);

    internal void SetExpanded(object item, bool expanded)
    {
        if (_nodes.TryGetValue(item, out var node) && node.HasChildren && node.IsExpanded != expanded)
        {
            node.IsExpanded = expanded;
            Flatten();
        }
    }

    internal void Select(ColumnTreeNode node)
    {
        SelectedItem = node.Item;
        Focus(NavigationMethod.Pointer);
    }

    private void Select(int index)
    {
        if (index >= 0 && index < _rows.Count)
        {
            SelectedItem = _rows[index].Item;
        }
    }

    private int IndexOfSelected()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            if (ReferenceEquals(_rows[i].Item, SelectedItem))
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerable<ColumnTreeNode> Roots()
    {
        if (ItemsSource is null)
        {
            yield break;
        }

        foreach (var item in ItemsSource)
        {
            if (item is not null)
            {
                yield return Node(item, null);
            }
        }
    }

    private IEnumerable<ColumnTreeNode> Children(ColumnTreeNode node)
    {
        if (ChildrenSelector?.Invoke(node.Item) is not { } children)
        {
            yield break;
        }

        foreach (var child in children)
        {
            if (child is not null)
            {
                yield return Node(child, node);
            }
        }
    }

    private ColumnTreeNode Node(object item, ColumnTreeNode? parent)
    {
        if (!_nodes.TryGetValue(item, out var node))
        {
            var children = ChildrenSelector?.Invoke(item);
            var hasChildren = children is not null && (children is not ICollection collection || collection.Count > 0);
            node = new ColumnTreeNode(this, item, parent, parent is null ? 0 : parent.Level + 1, hasChildren);
            _nodes[item] = node;
        }

        return node;
    }

    private void Flatten()
    {
        var rows = new List<ColumnTreeNode>();
        void Add(ColumnTreeNode node)
        {
            node.Index = rows.Count;
            node.IsSelected = ReferenceEquals(node.Item, SelectedItem);
            rows.Add(node);
            if (node.IsExpanded)
            {
                foreach (var child in Children(node))
                {
                    Add(child);
                }
            }
        }

        foreach (var root in Roots())
        {
            Add(root);
        }

        _rows.Clear();
        _rows.AddRange(rows);
    }

    private void Rebuild()
    {
        if (_header is null)
        {
            return;
        }

        ColumnTreeLayout.Apply(_header, Columns);
        _header.Children.Clear();
        for (var i = 0; i < Columns.Count; i++)
        {
            var cell = new ColumnTreeViewHeader { Content = Columns[i].Header };
            Grid.SetColumn(cell, i);
            _header.Children.Add(cell);
        }

        Flatten();
    }
}
