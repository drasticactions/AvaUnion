using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;

namespace AvaUnion.Controls;

/// <summary>A spin box that edits a date, one section at a time.</summary>
public class DateEdit : DateTimeEditBase
{
    /// <summary>Defines the <see cref="Date"/> property.</summary>
    public static readonly StyledProperty<DateOnly?> DateProperty =
        AvaloniaProperty.Register<DateEdit, DateOnly?>(nameof(Date), new DateOnly(2000, 1, 1), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The date, or null for an empty field. The default is 1 January 2000.</summary>
    public DateOnly? Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(DateTimeEditBase);

    /// <inheritdoc/>
    protected override string DefaultFormat => CultureFormats.ShortDate(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    protected override DateTime? DateTimeValue
    {
        get => Date?.ToDateTime(TimeOnly.MinValue);
        set => SetCurrentValue(DateProperty, value is { } v ? DateOnly.FromDateTime(v) : null);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateProperty)
        {
            Render();
        }
    }
}
