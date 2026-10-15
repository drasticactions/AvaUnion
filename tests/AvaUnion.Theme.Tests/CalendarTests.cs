using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class CalendarTests
{
    private static CalendarDayButton Day(Control root, DateTime date) =>
        root.GetVisualDescendants().OfType<CalendarDayButton>()
            .Single(b => b.DataContext is DateTime d && d.Date == date.Date && !b.Classes.Contains(":inactive"));

    [AvaloniaFact]
    public void Popup_Calendar_Marks_Today_When_Asked()
    {
        var picker = new CalendarDatePicker { Width = 220, IsTodayHighlighted = true };
        var window = Shots.Show(picker, 400, 400);
        picker.IsDropDownOpen = true;
        Shots.Settle();
        var calendar = picker.GetVisualDescendants().OfType<Popup>().Single(p => p.Name == "PART_Popup").Child!
            .GetVisualDescendants().OfType<Calendar>().Single();
        var today = Day(calendar, DateTime.Today);
        Assert.Contains("today", Union.GetNode(today)!.Hints);
        window.Close();
    }

    [AvaloniaFact]
    public void Selected_Day_Uses_The_Selection_Colors()
    {
        var date = new DateTime(2026, 10, 19);
        var calendar = new Calendar { DisplayDate = date, SelectedDate = date };
        var window = Shots.Show(calendar, 400, 400);
        var selected = Day(calendar, date);
        var other = Day(calendar, date.AddDays(1));
        var fill = selected.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Selection");
        Assert.True(fill.IsVisible);
        Assert.False(other.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Selection").IsVisible);
        var engine = Elements.UnionEnvironment.Engine;
        Assert.Equal(engine.SchemeColor(Engine.ColorGroup.Active, Engine.ColorSet.Selection, Engine.ColorRoleKind.Background, 0), ((ISolidColorBrush)fill.Background!).Color);
        Assert.Equal(engine.SchemeColor(Engine.ColorGroup.Active, Engine.ColorSet.Selection, Engine.ColorRoleKind.Foreground, 0), ((ISolidColorBrush)selected.Foreground!).Color);
        Shots.Capture(window, "calendar-selected");
        window.Close();
    }

    [AvaloniaFact]
    public void Selected_Day_Is_A_Month_Grid_Label()
    {
        var calendar = new Calendar { DisplayDate = new DateTime(2026, 10, 14), SelectedDate = new DateTime(2026, 10, 20) };
        var window = Shots.Show(calendar, 360, 320);
        Shots.Capture(window, "calendar");

        var days = calendar.GetVisualDescendants().OfType<CalendarDayButton>().ToList();
        Assert.True(days.Count >= 28);
        var selected = days.Single(d => d.Classes.Contains(":selected"));
        var node = Union.GetNode(selected)!;
        Assert.Equal("label", node.Type);
        Assert.Contains("current-month", node.Hints);
        Assert.Contains("monthgrid > label", node.ChainText);
        window.Close();
    }
}
