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
    public void Percentage_Is_Centered_On_The_Bar()
    {
        var bar = new ProgressBar { Width = 260, HorizontalAlignment = HorizontalAlignment.Left, Value = 5, ShowProgressText = true };
        var window = Shots.Show(bar, 400, 100);
        var content = Part(bar, "PART_Content");
        var text = Part(bar, "PART_Text");
        Assert.True(text.IsVisible);
        var middle = Avalonia.VisualExtensions.TranslatePoint(text, new Avalonia.Point(text.Bounds.Width / 2, text.Bounds.Height / 2), bar)!.Value;
        Assert.Equal(bar.Bounds.Width / 2, middle.X, 1.0);
        Assert.Equal(bar.Bounds.Height / 2, middle.Y, 1.0);

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
