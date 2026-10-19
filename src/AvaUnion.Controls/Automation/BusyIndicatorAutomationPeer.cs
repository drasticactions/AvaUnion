using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="BusyIndicator"/> to accessibility tools as a progress bar.</summary>
public class BusyIndicatorAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public BusyIndicatorAutomationPeer(BusyIndicator owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;
}
