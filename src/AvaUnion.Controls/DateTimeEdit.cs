using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;

namespace AvaUnion.Controls;

/// <summary>A spin box that edits a date and a time, one section at a time.</summary>
public class DateTimeEdit : DateTimeEditBase
{
    /// <summary>Defines the <see cref="DateTime"/> property.</summary>
    public static readonly StyledProperty<DateTime?> DateTimeProperty =
        AvaloniaProperty.Register<DateTimeEdit, DateTime?>(nameof(DateTime), new DateTime(2000, 1, 1), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The date and time, or null for an empty field. The default is midnight on 1 January 2000.</summary>
    public DateTime? DateTime
    {
        get => GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(DateTimeEditBase);

    /// <inheritdoc/>
    protected override string DefaultFormat =>
        CultureFormats.ShortDate(CultureInfo.CurrentCulture) + " " + CultureFormats.ShortTime(CultureInfo.CurrentCulture);

    /// <inheritdoc/>
    protected override System.DateTime? DateTimeValue
    {
        get => DateTime;
        set => SetCurrentValue(DateTimeProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateTimeProperty)
        {
            Render();
        }
    }
}
