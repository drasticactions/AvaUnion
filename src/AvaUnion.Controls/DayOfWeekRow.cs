using System;
using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>The short names of the seven days of the week in a row, for a <see cref="MonthGrid"/>.</summary>
[TemplatePart("PART_Cells", typeof(Panel))]
public class DayOfWeekRow : TemplatedControl
{
    /// <summary>Defines the <see cref="FirstDayOfWeek"/> property.</summary>
    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        MonthGrid.FirstDayOfWeekProperty.AddOwner<DayOfWeekRow>();

    private Panel? _cells;

    /// <summary>The day that starts the row. The default comes from the current culture.</summary>
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
        if (change.Property == FirstDayOfWeekProperty)
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
        var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        for (var i = 0; i < 7; i++)
        {
            _cells.Children.Add(new CalendarCell { Content = names[((int)FirstDayOfWeek + i) % 7] });
        }
    }
}
