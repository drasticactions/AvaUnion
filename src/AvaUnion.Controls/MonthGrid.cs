using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>
/// The days of one month in a grid of six weeks. The days of the month and
/// today are marked. Days of the next and previous months fill the grid.
/// </summary>
[TemplatePart("PART_Cells", typeof(Panel))]
public class MonthGrid : TemplatedControl
{
    /// <summary>Defines the <see cref="Month"/> property.</summary>
    public static readonly StyledProperty<int> MonthProperty =
        AvaloniaProperty.Register<MonthGrid, int>(nameof(Month), DateTime.Today.Month, coerce: (_, v) => Math.Clamp(v, 1, 12));

    /// <summary>Defines the <see cref="Year"/> property.</summary>
    public static readonly StyledProperty<int> YearProperty =
        AvaloniaProperty.Register<MonthGrid, int>(nameof(Year), DateTime.Today.Year, coerce: (_, v) => Math.Clamp(v, 1, 9999));

    /// <summary>Defines the <see cref="FirstDayOfWeek"/> property.</summary>
    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        AvaloniaProperty.Register<MonthGrid, DayOfWeek>(nameof(FirstDayOfWeek), CalendarMath.CultureFirstDay);

    /// <summary>Defines the <see cref="DateClicked"/> event.</summary>
    public static readonly RoutedEvent<DateClickedEventArgs> DateClickedEvent =
        RoutedEvent.Register<MonthGrid, DateClickedEventArgs>(nameof(DateClicked), RoutingStrategies.Bubble);

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

    /// <summary>Occurs when the user clicks a day.</summary>
    public event EventHandler<DateClickedEventArgs>? DateClicked
    {
        add => AddHandler(DateClickedEvent, value);
        remove => RemoveHandler(DateClickedEvent, value);
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
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Left && e.Source is Visual source && FindCell(source) is { } cell)
        {
            RaiseEvent(new DateClickedEventArgs(DateClickedEvent, cell.Date));
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.MonthGridAutomationPeer(this);

    private static CalendarCell? FindCell(Visual? visual)
    {
        while (visual is not null and not CalendarCell)
        {
            visual = Avalonia.VisualTree.VisualExtensions.GetVisualParent(visual);
        }

        return visual as CalendarCell;
    }

    private void Fill()
    {
        if (_cells is null)
        {
            return;
        }

        _cells.Children.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var date = CalendarMath.FirstVisibleDate(Year, Month, FirstDayOfWeek);
        for (var i = 0; i < 42; i++, date = date.AddDays(1))
        {
            var cell = new CalendarCell { Date = date, Content = date.Day.ToString(System.Globalization.CultureInfo.CurrentCulture) };
            cell.Classes.Set("current-month", date.Month == Month);
            cell.Classes.Set("today", date == today);
            _cells.Children.Add(cell);
        }
    }
}
