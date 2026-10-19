using System;

namespace AvaUnion.Controls.Primitives;

internal sealed class ColumnTreeNode(ColumnTreeView owner, object item, ColumnTreeNode? parent, int level, bool hasChildren)
{
    private bool _isSelected;
    private int _index;

    public event Action? Changed;

    public ColumnTreeView Owner { get; } = owner;

    public object Item { get; } = item;

    public ColumnTreeNode? Parent { get; } = parent;

    public int Level { get; } = level;

    public bool HasChildren { get; } = hasChildren;

    public bool IsExpanded { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                Changed?.Invoke();
            }
        }
    }

    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                Changed?.Invoke();
            }
        }
    }
}
