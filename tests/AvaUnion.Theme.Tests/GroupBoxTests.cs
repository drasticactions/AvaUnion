using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class GroupBoxTests
{
    [AvaloniaFact]
    public void Title_Is_Centered_Inside_The_Frame()
    {
        var group = new GroupBox { Header = "Notifications", Width = 400, Content = new CheckBox { Content = "Show banners" } };
        var window = Shots.Show(group, 500, 200);
        var parts = group.GetVisualDescendants().OfType<Control>().ToList();
        var header = parts.First(c => c.Name == "PART_HeaderPresenter");
        var content = parts.First(c => c.Name == "PART_ContentPresenter");
        var headerBox = header.Bounds.Translate(header.TranslatePoint(default, group)!.Value - header.Bounds.Position);
        var contentTop = content.TranslatePoint(default, group)!.Value.Y;

        Assert.True(headerBox.Top > 0, $"title at {headerBox} is not inside the frame");
        Assert.True(Math.Abs(headerBox.Center.X - group.Bounds.Width / 2) < 1, $"title at {headerBox} is not centered");
        Assert.True(contentTop >= headerBox.Bottom, $"content at {contentTop} overlaps the title {headerBox}");
        window.Close();
    }

    [AvaloniaFact]
    public void Headered_Content_Control_Is_A_Group_Box()
    {
        var group = new HeaderedContentControl { Header = "Group", Content = new CheckBox { Content = "Inside a group" } };
        var window = Shots.Show(group, 300, 120);
        Shots.Capture(window, "groupbox");
        Assert.Equal("groupbox", Union.GetNode(group)!.Type);
        window.Close();
    }
}
