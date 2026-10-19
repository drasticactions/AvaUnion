using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="RangeSlider"/> to accessibility tools as a group of two slider handles.</summary>
public class RangeSliderAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public RangeSliderAutomationPeer(RangeSlider owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}
