using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="SwipeDelegate"/> to accessibility tools as a list item that expands to show its hidden content.</summary>
public class SwipeDelegateAutomationPeer : ContentControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public SwipeDelegateAutomationPeer(SwipeDelegate owner)
        : base(owner)
    {
    }

    private new SwipeDelegate Owner => (SwipeDelegate)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => Owner.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand() => Owner.Open(left: Owner.RightContent is null);

    /// <inheritdoc/>
    public void Collapse() => Owner.Close();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
}
