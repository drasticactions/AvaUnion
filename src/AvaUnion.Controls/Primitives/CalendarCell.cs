using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class CalendarCell : ContentControl
{
    public System.DateOnly Date { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new CalendarCellAutomationPeer(this);

    private sealed class CalendarCellAutomationPeer(CalendarCell owner) : ContentControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string? GetNameCore() => base.GetNameCore() ?? (Owner as ContentControl)?.Content?.ToString();
    }
}
