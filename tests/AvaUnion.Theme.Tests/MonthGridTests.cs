using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class MonthGridTests
{
    private static ContentControl[] Cells(Control control) =>
        control.GetVisualDescendants().OfType<ContentControl>().Where(c => Union.GetNode(c)?.Type == "label").ToArray();

    [AvaloniaFact]
    public void Shows_Six_Weeks_Of_The_Month()
    {
        var grid = new MonthGrid { Year = 2026, Month = 10, FirstDayOfWeek = DayOfWeek.Sunday, Width = 350, Height = 300 };
        var days = new DayOfWeekRow { FirstDayOfWeek = DayOfWeek.Sunday, Width = 350 };
        var weeks = new WeekNumberColumn { Year = 2026, Month = 10, FirstDayOfWeek = DayOfWeek.Sunday, Height = 300 };
        var window = Shots.Show(new StackPanel { Children = { days, new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { weeks, grid } } } }, 500, 400);

        var cells = Cells(grid);
        Assert.Equal(42, cells.Length);
        Assert.Equal("27", cells[0].Content);
        Assert.Equal("1", cells[4].Content);
        Assert.Equal("monthgrid", Union.GetNode(grid)!.Type);
        Assert.Contains("monthgrid > label", Union.GetNode(cells[0])!.ChainText);
        Assert.DoesNotContain("current-month", Union.GetNode(cells[0])!.ChainText);
        Assert.Contains("monthgrid > label.current-month", Union.GetNode(cells[4])!.ChainText);
        Assert.Equal(cells[0].Bounds.Width, cells[6].Bounds.Width);

        Assert.Equal(new[] { "39", "40", "41", "42", "43", "44" }, Cells(weeks).Select(c => (string)c.Content!));
        var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
        Assert.Equal(names, Cells(days).Select(c => (string)c.Content!));
        Assert.Contains("dayofweekrow > label", Union.GetNode(Cells(days)[0])!.ChainText);
        Assert.Equal(AutomationControlType.Calendar, ControlAutomationPeer.CreatePeerForElement(grid).GetAutomationControlType());
        Shots.Capture(window, "monthgrid");

        DateOnly? clicked = null;
        grid.DateClicked += (_, e) => clicked = e.Date;
        var point = cells[10].TranslatePoint(new Point(5, 5), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.Equal(new DateOnly(2026, 10, 7), clicked);
        window.Close();
    }

    [AvaloniaFact]
    public void Marks_Today()
    {
        var today = DateTime.Today;
        var grid = new MonthGrid();
        var window = Shots.Show(grid);
        Assert.Equal(today.Month, grid.Month);
        var cell = Cells(grid).Single(c => Union.GetNode(c)!.ChainText.Contains("today"));
        Assert.Equal(today.Day.ToString(CultureInfo.CurrentCulture), cell.Content);
        Assert.Contains("label.current-month.today", Union.GetNode(cell)!.ChainText);
        window.Close();
    }
}
