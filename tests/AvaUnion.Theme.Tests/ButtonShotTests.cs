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
    public void Union_Icons_Are_Button_Icons()
    {
        var iconOnly = new Button { Content = new AvaUnion.Presenters.UnionIcon { IconName = "document-save" } };
        var underIcon = new Button
        {
            Content = new StackPanel
            {
                Children = { new AvaUnion.Presenters.UnionIcon { IconName = "document-save" }, new TextBlock { Text = "Save" } },
            },
        };
        var window = Shots.Show(new StackPanel { Children = { iconOnly, underIcon } });
        Assert.Contains("with-icon", AvaUnion.Union.GetNode(iconOnly)!.Hints);
        Assert.Contains("[display=icon-only]", AvaUnion.Union.GetNode(iconOnly)!.ChainText);
        Assert.Contains("[display=text-under-icon]", AvaUnion.Union.GetNode(underIcon)!.ChainText);
        window.Close();
    }

    [AvaloniaFact]
    public void Icons_That_Do_Not_Fit_Stay_Centered()
    {
        var icon = new AvaUnion.Presenters.UnionIcon { IconName = "document-save" };
        var button = new Button { Width = 24, Height = 24, Content = icon };
        var window = Shots.Show(new StackPanel { Children = { button } });
        var center = Avalonia.VisualExtensions.TranslatePoint(icon, new Avalonia.Point(icon.Bounds.Width / 2, icon.Bounds.Height / 2), button)!.Value;
        Assert.Equal(16, icon.Bounds.Width);
        Assert.Equal(12, center.X, 0.5);
        Assert.Equal(12, center.Y, 0.5);
        window.Close();
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
