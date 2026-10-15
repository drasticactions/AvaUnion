using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SliderTests
{
    [AvaloniaFact]
    public void Ticks_Are_Elements()
    {
        var slider = new Slider { Width = 220, Minimum = 0, Maximum = 100, Value = 50, TickFrequency = 25, TickPlacement = TickPlacement.Outside };
        var window = Shots.Show(slider, 300, 100);
        Shots.Capture(window, "ticks");

        var ticks = slider.GetVisualDescendants().OfType<Presenters.UnionBox>().Where(b => Union.GetType(b) == "tickmark").ToList();
        Assert.Equal(10, ticks.Count);
        Assert.Equal(6, ticks.Count(t => Union.GetNode(t)!.Hints.Contains("active")));
        Assert.True(Union.GetNode(ticks[0])!.ChainText.Contains("slider.horizontal > tickmark.active"), Union.GetNode(ticks[0])!.ChainText);
        Assert.All(ticks, t => Assert.Equal(2, t.Bounds.Width));
        window.Close();
    }
}
