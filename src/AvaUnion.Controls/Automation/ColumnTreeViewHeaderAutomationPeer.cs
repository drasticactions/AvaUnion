using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a column header of a <see cref="ColumnTreeView"/> to accessibility tools as a header item.</summary>
public class ColumnTreeViewHeaderAutomationPeer : ContentControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public ColumnTreeViewHeaderAutomationPeer(ColumnTreeViewHeader owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;
}
