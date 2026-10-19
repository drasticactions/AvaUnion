using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TumblerTests
{
    [AvaloniaFact]
    public void Selected_Item_Sits_In_The_Middle()
    {
        var hours = new Tumbler { ItemsSource = Enumerable.Range(0, 12).ToList() };
        var ampm = new Tumbler { ItemsSource = new[] { "AM", "PM" } };
        var window = Shots.Show(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { hours, ampm } }, 300, 260);
        Assert.Equal("tumbler", Union.GetNode(hours)!.Type);
        Assert.Equal(new Size(60, 200), hours.Bounds.Size);
        Assert.Equal(0, hours.SelectedIndex);

        var first = (TumblerItem)hours.ContainerFromIndex(0)!;
        var last = (TumblerItem)hours.ContainerFromIndex(11)!;
        Assert.Equal(new Rect(0, 80, 60, 40), first.Bounds);
        Assert.Equal(80 - 48, last.Bounds.Y);
        Assert.Contains("tumbler > text:highlighted", Union.GetNode(first)!.ChainText);
        Assert.DoesNotContain(":highlighted", Union.GetNode(last)!.ChainText);

        Assert.Equal(80 + 48, ampm.ContainerFromIndex(1)!.Bounds.Y);
        Shots.Capture(window, "tumbler");
        Assert.Equal(AutomationControlType.List, ControlAutomationPeer.CreatePeerForElement(hours).GetAutomationControlType());
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_Wheel_And_Keys_Select()
    {
        var hours = new Tumbler { ItemsSource = Enumerable.Range(0, 12).ToList() };
        var window = Shots.Show(hours, 200, 260);
        var origin = hours.TranslatePoint(default, window)!.Value;

        window.MouseDown(origin + new Point(30, 100), MouseButton.Left);
        window.MouseMove(origin + new Point(30, 18));
        Assert.Equal(2.05, hours.Position, 2);
        window.MouseUp(origin + new Point(30, 18), MouseButton.Left);
        Assert.Equal(2, hours.SelectedIndex);
        Assert.Equal(2, hours.Position);

        window.MouseWheel(origin + new Point(30, 100), new Vector(0, 1));
        Assert.Equal(1, hours.SelectedIndex);
        hours.Focus();
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        Assert.Equal(11, hours.SelectedIndex);
        window.Close();
    }
}
