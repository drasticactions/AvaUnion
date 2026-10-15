using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ButtonShotTests
{
    [AvaloniaFact]
    public void Buttons_Render()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(16) };
        panel.Children.Add(new Button { Content = "Push me" });
        panel.Children.Add(new Button { Content = "Flat", Classes = { "flat" } });
        panel.Children.Add(new Button { Content = "Disabled", IsEnabled = false });
        panel.Children.Add(new Button { Content = "With menu", Flyout = new MenuFlyout() });
        panel.Children.Add(new ToggleButton { Content = "Toggle", IsChecked = true });
        panel.Children.Add(new RepeatButton { Content = "Repeat" });
        panel.Children.Add(new HyperlinkButton { Content = "A link" });
        var window = Shots.Show(panel, 300, 380);
        var first = (Button)panel.Children[0];
        Assert.Equal("button", AvaUnion.Union.GetNode(first)!.Type);
        Assert.True(first.Bounds.Height >= 32);
        Shots.Capture(window, "buttons");
    }

    [AvaloniaFact]
    public void Checks_Render()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(16) };
        panel.Children.Add(new CheckBox { Content = "Unchecked" });
        panel.Children.Add(new CheckBox { Content = "Checked", IsChecked = true });
        panel.Children.Add(new CheckBox { Content = "Indeterminate", IsThreeState = true, IsChecked = null });
        panel.Children.Add(new CheckBox { Content = "Disabled checked", IsChecked = true, IsEnabled = false });
        panel.Children.Add(new RadioButton { Content = "Radio", GroupName = "g" });
        panel.Children.Add(new RadioButton { Content = "Radio checked", GroupName = "g", IsChecked = true });
        panel.Children.Add(new Label { Content = "A label" });
        var window = Shots.Show(panel, 300, 320);
        Shots.Capture(window, "checks");
        var check = (CheckBox)panel.Children[1];
        Assert.Contains(AvaUnion.Engine.UnionStates.Checked.ToString(), AvaUnion.Union.GetNode(check)!.States.ToString());
    }
}
