using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Elements;
using AvaUnion.Engine;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SplitButtonTests
{
    private static (UnionStates States, List<string> Hints) Map(StyledElement source)
    {
        var hints = new List<string>();
        return (StateMapper.Map(source, "button", hints), hints);
    }

    private static void Set(StyledElement e, string pseudoClass, bool on) => ((IPseudoClasses)e.Classes).Set(pseudoClass, on);

    [AvaloniaFact]
    public void States_Follow_Qt()
    {
        var split = new SplitButton();

        Set(split, ":flyout-open", true);
        var (states, hints) = Map(split);
        Assert.False(states.HasFlag(UnionStates.Pressed));
        Assert.Contains("menu-open", hints);

        Set(split, ":flyout-open", false);
        Set(split, ":primary-pressed", true);
        (states, hints) = Map(split);
        Assert.True(states.HasFlag(UnionStates.Pressed));
        Assert.DoesNotContain("menu-open", hints);

        Set(split, ":primary-pressed", false);
        Set(split, ":checked", true);
        Set(split, ":flyout-open", true);
        (states, hints) = Map(split);
        Assert.True(states.HasFlag(UnionStates.Pressed));
        Assert.DoesNotContain("menu-open", hints);

        var dropDown = new DropDownButton();
        Set(dropDown, ":flyout-open", true);
        var (dropStates, dropHints) = Map(dropDown);
        Assert.False(dropStates.HasFlag(UnionStates.Pressed));
        Assert.DoesNotContain("menu-open", dropHints);
    }

    [AvaloniaFact]
    public void Arrow_Half_Spans_The_Height_At_The_Right_Edge()
    {
        var split = new SplitButton { Content = "Split" };
        var window = Shots.Show(split, 300, 100);
        var parts = split.GetVisualDescendants().OfType<Button>().ToList();
        var primary = parts.First(b => b.Name == "PART_PrimaryButton");
        var arrow = parts.First(b => b.Name == "PART_SecondaryButton");
        var arrowBox = arrow.Bounds.Translate(arrow.TranslatePoint(default, split)!.Value - arrow.Bounds.Position);

        Assert.Equal(split.Bounds.Height, arrowBox.Height, 1);
        Assert.Equal(split.Bounds.Width, arrowBox.Right, 1);
        Assert.Equal(20, arrowBox.Width, 1);

        Assert.DoesNotContain(":primary-pressed", split.Classes);
        primary.GetType().GetProperty("IsPressed")!.SetValue(primary, true);
        Assert.Contains(":primary-pressed", split.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Split_Button_Has_A_Menu_Button()
    {
        var split = new SplitButton { Content = "Split", Flyout = new MenuFlyout() };
        var window = Shots.Show(split, 200, 80);
        Shots.Capture(window, "split-button");
        Assert.Contains("with-menu-button", Union.GetNode(split)!.Hints);
        window.Close();
    }
}
