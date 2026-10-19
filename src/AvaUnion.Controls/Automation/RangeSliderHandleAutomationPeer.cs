using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.VisualTree;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls.Automation;

/// <summary>Exposes a handle of a <see cref="RangeSlider"/> to accessibility tools as a slider with a range value.</summary>
public class RangeSliderHandleAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
    /// <summary>Creates the peer for <paramref name="owner"/>.</summary>
    public RangeSliderHandleAutomationPeer(RangeSliderHandle owner)
        : base(owner)
    {
    }

    private new RangeSliderHandle Owner => (RangeSliderHandle)base.Owner;

    private RangeSlider? Slider => Owner.FindAncestorOfType<RangeSlider>();

    /// <inheritdoc/>
    public bool IsReadOnly => !Owner.IsEnabled;

    /// <inheritdoc/>
    public double Minimum => Slider is { } s ? (Owner.IsUpper ? s.LowerValue : s.Minimum) : 0;

    /// <inheritdoc/>
    public double Maximum => Slider is { } s ? (Owner.IsUpper ? s.Maximum : s.UpperValue) : 0;

    /// <inheritdoc/>
    public double Value => Slider is { } s ? (Owner.IsUpper ? s.UpperValue : s.LowerValue) : 0;

    /// <inheritdoc/>
    public double SmallChange => Slider?.SmallChange ?? 0;

    /// <inheritdoc/>
    public double LargeChange => Slider?.LargeChange ?? 0;

    /// <inheritdoc/>
    public void SetValue(double value) => Slider?.SetHandleValue(Owner, value);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() ?? (Owner.IsUpper ? "Upper value" : "Lower value");
}
