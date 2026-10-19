using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a row of a <see cref="ColumnTreeView"/> to accessibility tools as a tree item that expands and collapses.</summary>
public class ColumnTreeViewRowAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public ColumnTreeViewRowAutomationPeer(ColumnTreeViewRow owner)
        : base(owner)
    {
    }

    private new ColumnTreeViewRow Owner => (ColumnTreeViewRow)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => !Owner.HasChildren
        ? ExpandCollapseState.LeafNode
        : Owner.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand()
    {
        if (!Owner.IsExpanded)
        {
            Owner.Toggle();
        }
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        if (Owner.IsExpanded)
        {
            Owner.Toggle();
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TreeItem;

    /// <inheritdoc/>
    protected override string? GetNameCore() =>
        base.GetNameCore() ?? Owner.FindDescendantOfType<TextBlock>()?.Text;
}
