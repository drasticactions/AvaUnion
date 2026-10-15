using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class PipsPagerTests
{
    [AvaloniaFact]
    public void Selected_Page_Is_The_Current_Pip()
    {
        var pips = new PipsPager { NumberOfPages = 5, SelectedPageIndex = 2 };
        var window = Shots.Show(pips, 300, 80);
        Shots.Capture(window, "pips");

        var current = pips.GetVisualDescendants().OfType<ListBoxItem>().Where(i => Union.GetNode(i)!.Hints.Contains("current")).ToList();
        Assert.Single(current);
        Assert.Equal(2, pips.GetVisualDescendants().OfType<ListBoxItem>().ToList().IndexOf(current[0]));
        window.Close();
    }
}
