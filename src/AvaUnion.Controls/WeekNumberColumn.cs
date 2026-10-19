using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>The ISO week numbers of the six weeks of a <see cref="MonthGrid"/>, in a column.</summary>
[TemplatePart("PART_Cells", typeof(Panel))]
public class WeekNumberColumn : TemplatedControl
{
    /// <summary>Defines the <see cref="Month"/> property.</summary>
    public static readonly StyledProperty<int> MonthProperty = MonthGrid.MonthProperty.AddOwner<WeekNumberColumn>();

    /// <summary>Defines the <see cref="Year"/> property.</summary>
    public static readonly StyledProperty<int> YearProperty = MonthGrid.YearProperty.AddOwner<WeekNumberColumn>();

    /// <summary>Defines the <see cref="FirstDayOfWeek"/> property.</summary>
    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty = MonthGrid.FirstDayOfWeekProperty.AddOwner<WeekNumberColumn>();

    private Panel? _cells;

    /// <summary>The month, from 1 to 12. The default is the current month.</summary>
    public int Month
    {
        get => GetValue(MonthProperty);
        set => SetValue(MonthProperty, value);
    }

    /// <summary>The year. The default is the current year.</summary>
    public int Year
    {
        get => GetValue(YearProperty);
        set => SetValue(YearProperty, value);
    }

    /// <summary>The day that starts each week. The default comes from the current culture.</summary>
    public DayOfWeek FirstDayOfWeek
    {
        get => GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _cells = e.NameScope.Find<Panel>("PART_Cells");
        Fill();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MonthProperty || change.Property == YearProperty || change.Property == FirstDayOfWeekProperty)
        {
            Fill();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.CalendarHeaderAutomationPeer(this);

    private void Fill()
    {
        if (_cells is null)
        {
            return;
        }

        _cells.Children.Clear();
        var date = CalendarMath.FirstVisibleDate(Year, Month, FirstDayOfWeek);
        for (var i = 0; i < 6; i++, date = date.AddDays(7))
        {
            _cells.Children.Add(new CalendarCell { Content = CalendarMath.WeekNumber(date).ToString(CultureInfo.CurrentCulture) });
        }
    }
}
