using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TabbedPageTests
{
    [AvaloniaFact]
    public void Tabbed_Page_Is_A_Page()
    {
        var tabbed = new TabbedPage { Height = 160 };
        tabbed.Pages = new[] { new ContentPage { Header = "Home", Content = new TextBlock { Text = "Home page" } }, new ContentPage { Header = "Settings" } };
        var window = Shots.Show(tabbed, 360, 200);
        Shots.Capture(window, "pages");
        Assert.Equal("page", Union.GetNode(tabbed)!.Type);
        window.Close();
    }
}
