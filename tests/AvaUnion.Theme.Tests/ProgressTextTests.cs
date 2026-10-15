using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ProgressTextTests
{
    private static Control Part(ProgressBar bar, string name) =>
        bar.GetVisualDescendants().OfType<Control>().First(c => c.Name == name);

    [AvaloniaFact]
    public void Percentage_Sits_Beside_The_Bar()
    {
        var bar = new ProgressBar { Width = 260, HorizontalAlignment = HorizontalAlignment.Left, Value = 5, ShowProgressText = true };
        var window = Shots.Show(bar, 400, 100);
        var content = Part(bar, "PART_Content");
        var text = Part(bar, "PART_Text");
        Assert.True(text.IsVisible);
        Assert.True(text.Bounds.Left >= content.Bounds.Right + 4, $"text {text.Bounds} overlaps bar {content.Bounds}");

        var length = content.Bounds.Width;
        bar.Value = 100;
        window.UpdateLayout();
        Assert.Equal(length, content.Bounds.Width);
        Assert.Equal(length, Part(bar, "PART_Indicator").Bounds.Width, 1);

        bar.IsIndeterminate = true;
        window.UpdateLayout();
        Assert.False(text.IsVisible);

        bar.IsIndeterminate = false;
        bar.Orientation = Orientation.Vertical;
        window.UpdateLayout();
        Assert.False(text.IsVisible);
        window.Close();
    }
}
