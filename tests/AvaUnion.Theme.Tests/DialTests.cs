using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DialTests
{
    [AvaloniaFact]
    public void Value_Turns_The_Handle()
    {
        var dial = new Dial();
        var window = Shots.Show(dial, 300, 300);
        var handle = dial.GetVisualDescendants().OfType<UnionBox>().Single(b => b.Name == "PART_Handle");
        Assert.Equal("dial", Union.GetNode(dial)!.Type);
        Assert.Equal(128, dial.Bounds.Width);
        Assert.Equal(20, handle.Bounds.Width);
        Assert.Equal(-140, dial.Angle);
        Assert.True(handle.Bounds.Center.X < 64 && handle.Bounds.Center.Y > 64, $"handle at {handle.Bounds.Center}");

        dial.Value = 50;
        Shots.Settle();
        Assert.Equal(0, dial.Angle);
        Assert.Equal(64, handle.Bounds.Center.X, 0.5);
        Assert.Equal(128 * 0.05 + 10, handle.Bounds.Center.Y, 0.5);
        Shots.Capture(window, "dial");
        Assert.Equal(AutomationControlType.Slider, ControlAutomationPeer.CreatePeerForElement(dial).GetAutomationControlType());
        window.Close();
    }

    [AvaloniaFact]
    public void Pointer_And_Keys_Change_The_Value()
    {
        var dial = new Dial();
        var window = Shots.Show(dial, 300, 300);
        var origin = dial.TranslatePoint(default, window)!.Value;

        window.MouseDown(origin + new Point(120, 64), MouseButton.Left);
        window.MouseUp(origin + new Point(120, 64), MouseButton.Left);
        Assert.Equal((90.0 + 140) / 280 * 100, dial.Value, 0.5);

        dial.Value = 10;
        window.MouseDown(origin + new Point(64, 125), MouseButton.Left);
        window.MouseUp(origin + new Point(64, 125), MouseButton.Left);
        Assert.Equal(0, dial.Value);

        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null);
        Assert.Equal(1, dial.Value);
        window.KeyPress(Key.End, RawInputModifiers.None, PhysicalKey.End, null);
        Assert.Equal(100, dial.Value);
        window.Close();
    }
}
