using Avalonia.Automation.Peers;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="MonthGrid"/> to accessibility tools as a calendar.</summary>
public class MonthGridAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public MonthGridAutomationPeer(MonthGrid owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Calendar;
}
