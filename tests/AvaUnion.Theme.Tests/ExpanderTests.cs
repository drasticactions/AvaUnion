using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ExpanderTests
{
    [AvaloniaFact]
    public void Header_Arrow_Follows_The_Expansion()
    {
        var expander = new Expander { Header = "More options", Content = "Content" };
        var window = Shots.Show(expander, 300, 200);
        var header = expander.GetVisualDescendants().OfType<ToggleButton>().First();
        var arrow = header.GetVisualDescendants().OfType<UnionIcon>().First(i => i.Name == "PART_Arrow");
        Assert.Equal("arrow-right", arrow.EffectiveName);

        expander.IsExpanded = true;
        UnionEnvironment.Flush();
        Assert.Equal("arrow-down", arrow.EffectiveName);
        var hints = new System.Collections.Generic.List<string>();
        Assert.False(StateMapper.Map(header, "toolbutton", hints).HasFlag(UnionStates.Checked));
        Assert.Contains("expanded", hints);

        expander.ExpandDirection = ExpandDirection.Up;
        UnionEnvironment.Flush();
        Assert.Equal("arrow-up", arrow.EffectiveName);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Takes_Clicks_Away_From_Its_Text()
    {
        var expander = new Expander { Header = "More options", Content = "Content", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var window = Shots.Show(expander, 300, 200);
        var header = expander.GetVisualDescendants().OfType<ToggleButton>().First();
        var text = header.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");

        var textEnd = text.TranslatePoint(new Point(text.Bounds.Width, 0), window)!.Value;
        var headerBox = header.TranslatePoint(new Point(header.Bounds.Width, header.Bounds.Height), window)!.Value;
        Assert.True(headerBox.X - textEnd.X > 2, "the header has no room after its text");
        var point = new Point((textEnd.X + headerBox.X) / 2, headerBox.Y - 2);

        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.True(expander.IsExpanded);
        window.Close();
    }
}
