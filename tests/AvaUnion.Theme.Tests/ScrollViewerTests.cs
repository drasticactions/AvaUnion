using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ScrollViewerTests
{
    [AvaloniaFact]
    public void Scroll_Corner_Is_An_Element()
    {
        var scroller = new ScrollViewer
        {
            Width = 160,
            Height = 100,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = new Border { Width = 400, Height = 400 },
        };
        var window = Shots.Show(scroller, 240, 160);
        Shots.Capture(window, "scroll-corner");

        var corner = scroller.GetVisualDescendants().OfType<Presenters.UnionBox>().Single(b => Union.GetType(b) == "scrollareacorner");
        Assert.True(corner.IsEffectivelyVisible);
        Assert.True(Union.GetNode(corner)!.Resolved.Background.Color is { A: > 0 });
        window.Close();
    }
}
