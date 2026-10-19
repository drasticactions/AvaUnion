using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DateTimeEditTests
{
    private static TextBox Field(Control edit) => edit.GetVisualDescendants().OfType<TextBox>().Single();

    [AvaloniaFact]
    public void Steps_The_Section_At_The_Caret()
    {
        var edit = new TimeEdit { Format = "HH:mm" };
        var window = Shots.Show(edit);
        Assert.Contains("spinbox.editable.constrained", Union.GetNode(edit)!.ChainText);
        var increase = edit.GetVisualDescendants().OfType<RepeatButton>().Last();
        Assert.Contains("spinbox.editable.constrained > indicator.increase", Union.GetNode(increase)!.ChainText);
        Assert.Equal("00:00", Field(edit).Text);

        edit.Step(1);
        Assert.Equal(TimeSpan.FromHours(1), edit.Time);
        Assert.Equal("01:00", Field(edit).Text);
        Assert.Equal(0, Field(edit).SelectionStart);
        Assert.Equal(2, Field(edit).SelectionEnd);

        Field(edit).CaretIndex = 4;
        edit.Step(-1);
        Assert.Equal(new TimeSpan(1, 0, 0), edit.Time);
        edit.Step(5);
        Assert.Equal(new TimeSpan(1, 5, 0), edit.Time);
        Assert.Equal(AutomationControlType.Spinner, ControlAutomationPeer.CreatePeerForElement(edit).GetAutomationControlType());
        Shots.Capture(window, "timeedit");
        window.Close();
    }

    [AvaloniaFact]
    public void Typed_Text_Is_Read_On_Commit()
    {
        var edit = new TimeEdit { Format = "HH:mm" };
        var window = Shots.Show(edit);
        Assert.True(edit.TryCommit("13:45"));
        Assert.Equal(new TimeSpan(13, 45, 0), edit.Time);
        Assert.False(edit.TryCommit("later"));
        Assert.Equal(new TimeSpan(13, 45, 0), edit.Time);
        Assert.Equal("13:45", Field(edit).Text);
        window.Close();
    }

    [AvaloniaFact]
    public void Dates_Stay_In_Their_Month()
    {
        var edit = new DateEdit { Format = "yyyy-MM-dd", Date = new DateOnly(2000, 1, 31) };
        var stamp = new DateTimeEdit { Format = "yyyy-MM-dd HH:mm" };
        var window = Shots.Show(new StackPanel { Children = { edit, stamp } });
        Assert.Equal("2000-01-31", Field(edit).Text);
        Field(edit).CaretIndex = 9;
        edit.Step(1);
        Assert.Equal(new DateOnly(2000, 1, 31), edit.Date);
        Field(edit).CaretIndex = 6;
        edit.Step(1);
        Assert.Equal(new DateOnly(2000, 2, 29), edit.Date);
        Assert.Equal("2000-01-01 00:00", Field(stamp).Text);
        window.Close();
    }

    [Theory]
    [InlineData("en-US", "M/d/yy")]
    [InlineData("de-DE", "dd.MM.yy")]
    [InlineData("es-ES", "d/M/yy")]
    [InlineData("en-GB", "dd/MM/yyyy")]
    [InlineData("ja-JP", "yyyy/MM/dd")]
    public void Short_Dates_Use_The_Year_Digits_Of_Qt(string culture, string pattern)
    {
        Assert.Equal(pattern, CultureFormats.ShortDate(new System.Globalization.CultureInfo(culture)));
    }

    [AvaloniaFact]
    public void Two_Digit_Years_Step_And_Commit()
    {
        var edit = new DateEdit { Format = "M/d/yy" };
        var window = Shots.Show(edit);
        Assert.Equal("1/1/00", Field(edit).Text);
        Field(edit).CaretIndex = 5;
        edit.Step(1);
        Assert.Equal(new DateOnly(2001, 1, 1), edit.Date);
        Assert.Equal("1/1/01", Field(edit).Text);
        Assert.True(edit.TryCommit("12/31/49"));
        Assert.Equal(new DateOnly(2049, 12, 31), edit.Date);
        window.Close();
    }

    [AvaloniaFact]
    public void Losing_Focus_Clears_The_Selection()
    {
        var edit = new DateEdit { Format = "M/d/yy" };
        var other = new TextBox();
        var window = Shots.Show(new StackPanel { Children = { edit, other } });
        Field(edit).Focus();
        Field(edit).CaretIndex = 5;
        edit.Step(-1);
        Assert.NotEqual(Field(edit).SelectionStart, Field(edit).SelectionEnd);
        other.Focus();
        Shots.Settle();
        Assert.Equal(Field(edit).SelectionStart, Field(edit).SelectionEnd);
        window.Close();
    }

    [AvaloniaFact]
    public void Clicking_An_Arrow_Focuses_The_Edit()
    {
        var edit = new DateEdit { Format = "M/d/yy" };
        var numeric = new NumericUpDown { Value = 1 };
        var window = Shots.Show(new StackPanel { Children = { edit, numeric } });
        foreach (var control in new Control[] { edit, numeric })
        {
            var increase = control.GetVisualDescendants().OfType<RepeatButton>().Last();
            var point = increase.TranslatePoint(new Avalonia.Point(increase.Bounds.Width / 2, increase.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, Avalonia.Input.MouseButton.Left);
            window.MouseUp(point, Avalonia.Input.MouseButton.Left);
            Shots.Settle();
            Assert.True(control.IsKeyboardFocusWithin, control.GetType().Name);
            Assert.True(Union.GetNode(control)!.States.HasFlag(AvaUnion.Engine.UnionStates.ActiveFocus), control.GetType().Name);
        }

        Assert.Equal(new DateOnly(2000, 2, 1), edit.Date);
        window.Close();
    }
}
