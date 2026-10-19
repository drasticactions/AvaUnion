using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using AvaUnion.Controls.Primitives;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ClosableTabsTests
{
    [AvaloniaFact]
    public void Tabs_Get_A_Close_Button_That_Asks_To_Close()
    {
        var first = new TabItem { Header = "Tab 1", Content = "One day" };
        var second = new TabItem { Header = "Tab 2", Content = "I went to the park" };
        var tabs = new TabControl { Items = { first, second } };
        var plain = new TabItem { Header = "Tab 1" };
        var window = Shots.Show(new StackPanel { Children = { tabs, new TabControl { Items = { plain } } } }, 400, 300);
        Assert.Empty(plain.GetVisualDescendants().OfType<TabCloseButton>());

        ClosableTabs.SetIsEnabled(tabs, true);
        Shots.Settle();
        var close = second.GetVisualDescendants().OfType<TabCloseButton>().Single();
        Assert.EndsWith("] > closebutton", Union.GetNode(close)!.ChainText);
        Assert.Equal(16, close.Bounds.Width);
        Assert.True(close.TranslatePoint(default, second)!.Value.X > second.Bounds.Width / 2, $"close at {close.TranslatePoint(default, second)} in {second.Bounds}");
        Shots.Capture(window, "closabletabs");

        TabItem? asked = null;
        ClosableTabs.AddCloseRequestedHandler(tabs, (_, e) => asked = e.Tab);
        var point = close.TranslatePoint(new Point(8, 8), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.Same(second, asked);
        window.Close();
    }
}
