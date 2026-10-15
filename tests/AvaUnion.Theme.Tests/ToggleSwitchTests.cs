using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ToggleSwitchTests
{
    [AvaloniaFact]
    public void Knob_Sits_At_The_Checked_End()
    {
        var off = new ToggleSwitch { Content = "Switch off" };
        var on = new ToggleSwitch { Content = "Switch on", IsChecked = true };
        var window = Shots.Show(new StackPanel { Spacing = 10, Children = { off, on } }, 240, 120);
        Shots.Capture(window, "switches");
        Assert.Equal("switch", Union.GetNode(on)!.Type);

        var track = on.GetVisualDescendants().OfType<Presenters.SwitchKnobCanvas>().Single();
        var knob = track.Children[0];
        Assert.True(track.Bounds.Width > 0);
        Assert.Equal(track.Bounds.Width, Canvas.GetLeft(knob));
        var offKnob = off.GetVisualDescendants().OfType<Presenters.SwitchKnobCanvas>().Single().Children[0];
        Assert.True(double.IsNaN(Canvas.GetLeft(offKnob)) || Canvas.GetLeft(offKnob) == 0);
        window.Close();
    }
}
