using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a <see cref="DayOfWeekRow"/> or a <see cref="WeekNumberColumn"/> to accessibility tools as a header.</summary>
public class CalendarHeaderAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public CalendarHeaderAutomationPeer(Control owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;
}
