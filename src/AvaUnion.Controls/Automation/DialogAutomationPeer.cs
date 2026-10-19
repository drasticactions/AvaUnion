using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="Dialog"/> to accessibility tools as a window with its title as the name.</summary>
public class DialogAutomationPeer : ContentControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public DialogAutomationPeer(Dialog owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() ?? ((Dialog)Owner).Title;
}
