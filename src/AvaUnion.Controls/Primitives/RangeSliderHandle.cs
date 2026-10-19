using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace AvaUnion.Controls.Primitives;

/// <summary>A handle of a <see cref="RangeSlider"/>.</summary>
public class RangeSliderHandle : Thumb
{
    /// <summary>Defines the <see cref="IsUpper"/> property.</summary>
    public static readonly StyledProperty<bool> IsUpperProperty =
        AvaloniaProperty.Register<RangeSliderHandle, bool>(nameof(IsUpper));

    /// <summary>True for the handle of <see cref="RangeSlider.UpperValue"/>, false for the handle of <see cref="RangeSlider.LowerValue"/>.</summary>
    public bool IsUpper
    {
        get => GetValue(IsUpperProperty);
        set => SetValue(IsUpperProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.RangeSliderHandleAutomationPeer(this);
}
