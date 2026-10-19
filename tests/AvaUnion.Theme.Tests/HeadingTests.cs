using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class HeadingTests
{
    [AvaloniaFact]
    public void Level_And_Type_Pick_The_Font()
    {
        var first = new Heading { Content = "Heading" };
        var second = new Heading { Content = "Second", Level = 2 };
        var primary = new Heading { Content = "Primary", Type = HeadingType.Primary };
        var disabled = new Heading { Content = "Disabled", IsEnabled = false };
        var window = Shots.Show(new StackPanel { Children = { first, second, primary, disabled } });
        Assert.Contains("heading.level-1", Union.GetNode(first)!.ChainText);
        Assert.Contains("heading.level-1.primary", Union.GetNode(primary)!.ChainText);
        Assert.Equal(18, first.FontSize);
        Assert.Equal(16, second.FontSize);
        Assert.Equal(FontWeight.Normal, first.FontWeight);
        Assert.Equal(FontWeight.SemiBold, primary.FontWeight);
        Assert.NotEqual(((ISolidColorBrush)first.Foreground!).Color, ((ISolidColorBrush)disabled.Foreground!).Color);
        Assert.Equal(2, AutomationProperties.GetHeadingLevel(second));
        second.Level = 9;
        Assert.Equal(5, second.Level);
        window.Close();
    }
}
