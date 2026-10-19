using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;

namespace AvaUnion.Controls;

/// <summary>A spin box that edits a time of day, one section at a time.</summary>
public class TimeEdit : DateTimeEditBase
{
    /// <summary>Defines the <see cref="Time"/> property.</summary>
    public static readonly StyledProperty<TimeSpan?> TimeProperty =
        AvaloniaProperty.Register<TimeEdit, TimeSpan?>(nameof(Time), TimeSpan.Zero, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The time, from 0 to less than 24 hours, or null for an empty field. The default is midnight.</summary>
    public TimeSpan? Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(DateTimeEditBase);

    /// <inheritdoc/>
    protected override string DefaultFormat => CultureFormats.ShortTime(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    protected override DateTime? DateTimeValue
    {
        get => Time is { } t ? DateTime.MinValue.Add(t) : null;
        set => SetCurrentValue(TimeProperty, value?.TimeOfDay);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TimeProperty)
        {
            Render();
        }
    }
}
