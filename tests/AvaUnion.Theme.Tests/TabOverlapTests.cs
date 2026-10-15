using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TabOverlapTests
{
    [AvaloniaTheory]
    [InlineData(Dock.Top, 0, -1, 0, 0)]
    [InlineData(Dock.Bottom, 0, 0, 0, -1)]
    [InlineData(Dock.Left, -1, 0, 0, 0)]
    [InlineData(Dock.Right, 0, 0, -1, 0)]
    public void Tab_Strip_Overlaps_The_Frame(Dock placement, double left, double top, double right, double bottom)
    {
        var tabs = new TabControl { TabStripPlacement = placement, Width = 400, Height = 140 };
        tabs.Items.Add(new TabItem { Header = "General", Content = "General settings" });
        tabs.Items.Add(new TabItem { Header = "Advanced", Content = "Advanced settings" });
        var window = Shots.Show(tabs, 500, 200);
        var parts = tabs.GetVisualDescendants().OfType<Control>().ToList();
        Assert.Equal(new Thickness(left, top, right, bottom), parts.First(c => c.Name == "PART_Pane").Margin);
        Assert.True(parts.First(c => c.Name == "PART_TabScroll").ZIndex > 0);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Dock.Top, "PART_HorizontalButtons", "PART_ScrollRight")]
    [InlineData(Dock.Left, "PART_VerticalButtons", "PART_ScrollDown")]
    public void Tabs_That_Do_Not_Fit_Scroll(Dock placement, string buttons, string forward)
    {
        var tabs = new TabControl { TabStripPlacement = placement, Width = 400, Height = 200 };
        for (var i = 0; i < 12; i++)
        {
            tabs.Items.Add(new TabItem { Header = $"Tab {i + 1}", Content = $"Page {i + 1}" });
        }

        var window = Shots.Show(tabs, 500, 260);
        var parts = tabs.GetVisualDescendants().OfType<Control>().ToList();
        var scroll = (ScrollViewer)parts.First(c => c.Name == "PART_TabScroll");
        Assert.True(parts.First(c => c.Name == buttons).IsVisible);
        Assert.Equal(0, scroll.Offset.X + scroll.Offset.Y);
        Assert.False(parts.First(c => c.Name == (placement == Dock.Top ? "PART_ScrollLeft" : "PART_ScrollUp")).IsEnabled);

        var back = parts.First(c => c.Name == (placement == Dock.Top ? "PART_ScrollLeft" : "PART_ScrollUp"));
        var backColor = back.GetVisualDescendants().OfType<Presenters.UnionIcon>().Single().Color;
        var forwardColor = parts.First(c => c.Name == forward).GetVisualDescendants().OfType<Presenters.UnionIcon>().Single().Color;
        Assert.NotNull(forwardColor);
        Assert.NotNull(backColor);
        Assert.NotEqual(forwardColor, backColor);

        var button = (RepeatButton)parts.First(c => c.Name == forward);
        button.Command!.Execute(null);
        Shots.Settle();
        Assert.True(scroll.Offset.X + scroll.Offset.Y > 0);

        tabs.SelectedIndex = 0;
        Shots.Settle();
        tabs.SelectedIndex = 11;
        Shots.Settle();
        var last = tabs.ContainerFromIndex(11)!;
        var origin = last.TranslatePoint(default, scroll)!.Value;
        var viewport = scroll.Viewport;
        Assert.True(origin.X + last.Bounds.Width <= viewport.Width + 0.5 && origin.Y + last.Bounds.Height <= viewport.Height + 0.5);
        Shots.Capture(window, $"tab-scroll-{placement}".ToLowerInvariant());
        window.Close();
    }

    [AvaloniaFact]
    public void Tabs_That_Fit_Show_No_Scroll_Buttons()
    {
        var tabs = new TabControl { Width = 400, Height = 140 };
        tabs.Items.Add(new TabItem { Header = "General", Content = "General settings" });
        tabs.Items.Add(new TabItem { Header = "Advanced", Content = "Advanced settings" });
        var window = Shots.Show(tabs, 500, 200);
        var parts = tabs.GetVisualDescendants().OfType<Control>().ToList();
        Assert.False(parts.First(c => c.Name == "PART_HorizontalButtons").IsVisible);
        Assert.False(parts.First(c => c.Name == "PART_VerticalButtons").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void The_First_And_Last_Tabs_Say_So()
    {
        var tabs = new TabControl { Width = 400, Height = 140 };
        for (var i = 0; i < 3; i++)
        {
            tabs.Items.Add(new TabItem { Header = $"Tab {i + 1}" });
        }

        var window = Shots.Show(tabs, 500, 200);
        string[] Hints(int index) => [.. Union.GetNode(tabs.ContainerFromIndex(index)!)!.Hints];
        Assert.Contains("first", Hints(0));
        Assert.DoesNotContain("last", Hints(0));
        Assert.DoesNotContain("first", Hints(1));
        Assert.DoesNotContain("last", Hints(1));
        Assert.Contains("last", Hints(2));

        tabs.Items.Add(new TabItem { Header = "Tab 4" });
        Shots.Settle();
        Assert.DoesNotContain("last", Hints(2));
        Assert.Contains("last", Hints(3));
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(Dock.Left, -90)]
    [InlineData(Dock.Right, 90)]
    [InlineData(Dock.Top, 0)]
    public void Side_Tabs_Turn_Their_Headers(Dock placement, double degrees)
    {
        var tabs = new TabControl { TabStripPlacement = placement, Width = 400, Height = 300 };
        tabs.Items.Add(new TabItem { Header = "General" });
        var window = Shots.Show(tabs, 500, 360);
        var tab = (TabItem)tabs.ContainerFromIndex(0)!;
        var transform = tab.GetVisualDescendants().OfType<LayoutTransformControl>().Single();
        var angle = transform.LayoutTransform?.Value is { } m ? System.Math.Round(System.Math.Atan2(m.M12, m.M11) * 180 / System.Math.PI) : 0;
        Assert.Equal(degrees, angle);
        if (degrees != 0)
        {
            Assert.True(tab.Bounds.Height > tab.Bounds.Width * 2, $"tab is {tab.Bounds.Size}");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void The_Wheel_Over_The_Tabs_Changes_The_Tab()
    {
        var tabs = new TabControl { Width = 400, Height = 200 };
        for (var i = 0; i < 12; i++)
        {
            tabs.Items.Add(new TabItem { Header = $"Tab {i + 1}", Content = new Border { Height = 100 }, IsEnabled = i != 3 });
        }

        var window = Shots.Show(tabs, 500, 260);
        var strip = tabs.ContainerFromIndex(0)!;
        var over = strip.TranslatePoint(new Point(10, strip.Bounds.Height / 2), window)!.Value;
        window.MouseWheel(over, new Vector(0, -1));
        window.MouseWheel(over, new Vector(0, -1));
        Shots.Settle();
        Assert.Equal(2, tabs.SelectedIndex);

        window.MouseWheel(over, new Vector(0, -1));
        Shots.Settle();
        Assert.Equal(4, tabs.SelectedIndex);

        window.MouseWheel(over, new Vector(0, 1));
        Shots.Settle();
        Assert.Equal(2, tabs.SelectedIndex);

        window.MouseWheel(new Point(200, 150), new Vector(0, -1));
        Shots.Settle();
        Assert.Equal(2, tabs.SelectedIndex);

        for (var i = 0; i < 5; i++)
        {
            window.MouseWheel(over, new Vector(0, -0.5));
        }

        Shots.Settle();
        Assert.Equal(5, tabs.SelectedIndex);
        for (var i = 0; i < 6; i++)
        {
            window.MouseWheel(over, new Vector(0.5, 0));
        }

        Shots.Settle();
        Assert.Equal(1, tabs.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void Tab_Items_Are_Tab_Buttons()
    {
        var tabs = new TabControl { Height = 120 };
        tabs.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "First page" } });
        tabs.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "Second page" } });
        tabs.SelectedIndex = 0;
        var window = Shots.Show(tabs, 360, 160);
        Shots.Capture(window, "tabs");

        var first = (TabItem)tabs.ContainerFromIndex(0)!;
        var node = Union.GetNode(first)!;
        Assert.Equal("tabbutton", node.Type);
        Assert.Contains("tabwidget > tabbar[tab-position=top] > tabbutton", node.ChainText);
        Assert.True((node.States & Engine.UnionStates.Checked) != 0);
        window.Close();
    }
}
