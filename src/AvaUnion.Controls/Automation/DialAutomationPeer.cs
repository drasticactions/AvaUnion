using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="Dial"/> to accessibility tools as a slider with a range value.</summary>
public class DialAutomationPeer : RangeBaseAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public DialAutomationPeer(Dial owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
}
