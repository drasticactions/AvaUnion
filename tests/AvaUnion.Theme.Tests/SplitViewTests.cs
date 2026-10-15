using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SplitViewTests
{
    [AvaloniaFact]
    public void Split_View_Is_A_Pane()
    {
        var split = new SplitView { IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline, OpenPaneLength = 120, Height = 120, Pane = new TextBlock { Text = "Pane" }, Content = new TextBlock { Text = "Content" } };
        var window = Shots.Show(split, 360, 160);
        Shots.Capture(window, "splitview");
        Assert.Equal("pane", Union.GetNode(split)!.Type);
        window.Close();
    }
}
