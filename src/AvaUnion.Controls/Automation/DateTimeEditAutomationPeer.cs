using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a date or time spin box to accessibility tools as a spinner with a text value.</summary>
public class DateTimeEditAutomationPeer : ControlAutomationPeer, IValueProvider
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public DateTimeEditAutomationPeer(DateTimeEditBase owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public bool IsReadOnly => !Owner.IsEnabled;

    /// <inheritdoc/>
    public string? Value => ((DateTimeEditBase)Owner).Text;

    /// <inheritdoc/>
    public void SetValue(string? value) => ((DateTimeEditBase)Owner).TryCommit(value);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;
}
