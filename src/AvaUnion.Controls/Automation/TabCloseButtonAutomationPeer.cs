using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes the close button of a tab to accessibility tools as a button that closes the tab.</summary>
public class TabCloseButtonAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public TabCloseButtonAutomationPeer(TabCloseButton owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public void Invoke() => ((TabCloseButton)Owner).Close();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
}
