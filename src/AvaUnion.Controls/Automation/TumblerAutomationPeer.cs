using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="Tumbler"/> to accessibility tools as a list with one selected item.</summary>
public class TumblerAutomationPeer : SelectingItemsControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public TumblerAutomationPeer(Tumbler owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
}
