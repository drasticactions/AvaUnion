using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>
/// Shows that an operation is in progress, with a turning icon. The icon is
/// not visible when <see cref="IsRunning"/> is false.
/// </summary>
public class BusyIndicator : TemplatedControl
{
    /// <summary>Defines the <see cref="IsRunning"/> property.</summary>
    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<BusyIndicator, bool>(nameof(IsRunning), true);

    /// <summary>True when the indicator turns. The default is true.</summary>
    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.BusyIndicatorAutomationPeer(this);
}
