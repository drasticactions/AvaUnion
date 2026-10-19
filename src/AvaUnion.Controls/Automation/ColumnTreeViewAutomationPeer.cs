using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="ColumnTreeView"/> to accessibility tools as a tree.</summary>
public class ColumnTreeViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public ColumnTreeViewAutomationPeer(ColumnTreeView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tree;
}
