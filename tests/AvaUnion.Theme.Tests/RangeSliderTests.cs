using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using AvaUnion.Controls.Primitives;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class RangeSliderTests
{
    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);

    [AvaloniaFact]
    public void Lays_Out_Like_Union()
    {
        var slider = new RangeSlider { LowerValue = 25, UpperValue = 75, HorizontalAlignment = HorizontalAlignment.Left };
        var vertical = new RangeSlider { LowerValue = 25, UpperValue = 75, Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Left };
        var window = Shots.Show(new StackPanel { Spacing = 8, Children = { slider, vertical } }, 300, 300);
        var lower = Part<RangeSliderHandle>(slider, "PART_LowerHandle");
        var upper = Part<RangeSliderHandle>(slider, "PART_UpperHandle");
        var groove = Part<UnionBox>(slider, "PART_Groove");
        var fill = Part<UnionBox>(slider, "PART_Fill");
        Assert.Contains("rangeslider.horizontal", Union.GetNode(slider)!.ChainText);
        Assert.Contains("rangeslider.horizontal > handle.first", Union.GetNode(lower)!.ChainText);
        Assert.Contains("handle.second", Union.GetNode(upper)!.ChainText);
        Assert.Equal(new Size(200, 20), slider.Bounds.Size);
        Assert.Equal(6, groove.Bounds.Height);
        Assert.Equal(0.25 * 180, lower.Bounds.X);
        Assert.Equal(0.75 * 180, upper.Bounds.X);
        Assert.Equal(new Rect(53, 7, 94, 6), fill.Bounds);

        Assert.Contains("rangeslider.vertical", Union.GetNode(vertical)!.ChainText);
        Assert.Equal(new Size(20, 200), vertical.Bounds.Size);
        Assert.Equal(0.75 * 180, Part<RangeSliderHandle>(vertical, "PART_LowerHandle").Bounds.Y);
        Shots.Capture(window, "rangeslider");
        window.Close();
    }

    [AvaloniaFact]
    public void Values_Stay_In_Order()
    {
        var slider = new RangeSlider { LowerValue = 40, UpperValue = 60 };
        slider.LowerValue = 80;
        Assert.Equal(60, slider.LowerValue);
        slider.UpperValue = 10;
        Assert.Equal(60, slider.UpperValue);
        slider.Maximum = 50;
        Assert.Equal(50, slider.UpperValue);
        Assert.Equal(50, slider.LowerValue);
    }

    [AvaloniaFact]
    public void Pointer_And_Keys_Move_The_Handles()
    {
        var slider = new RangeSlider { LowerValue = 25, UpperValue = 75, HorizontalAlignment = HorizontalAlignment.Left, IsSnapToTickEnabled = true, TickFrequency = 1 };
        var window = Shots.Show(slider, 300, 100);
        var origin = slider.TranslatePoint(default, window)!.Value;

        window.MouseDown(origin + new Point(10 + (0.9 * 180), 10), MouseButton.Left);
        window.MouseUp(origin + new Point(10 + (0.9 * 180), 10), MouseButton.Left);
        Assert.Equal(90, slider.UpperValue);
        Assert.Equal(25, slider.LowerValue);

        var lower = Part<RangeSliderHandle>(slider, "PART_LowerHandle");
        var start = origin + lower.Bounds.Center;
        window.MouseDown(start, MouseButton.Left);
        window.MouseMove(start + new Point(18, 0));
        window.MouseUp(start + new Point(18, 0), MouseButton.Left);
        Assert.Equal(35, slider.LowerValue);

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(36, slider.LowerValue);

        var peer = ControlAutomationPeer.CreatePeerForElement(Part<RangeSliderHandle>(slider, "PART_UpperHandle"));
        Assert.Equal(AutomationControlType.Slider, peer.GetAutomationControlType());
        var range = peer.GetProvider<IRangeValueProvider>()!;
        Assert.Equal(90, range.Value);
        Assert.Equal(36, range.Minimum);
        range.SetValue(50);
        Assert.Equal(50, slider.UpperValue);
        window.Close();
    }
}
