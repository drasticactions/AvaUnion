using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class CommandBarTests
{
    [AvaloniaFact]
    public void Toggles_And_Separators_Are_Tool_Bar_Elements()
    {
        var toggle = new CommandBarToggleButton { Label = "Bold", IsChecked = true };
        var bar = new CommandBar();
        bar.PrimaryCommands!.Add(new CommandBarButton { Label = "New" });
        bar.PrimaryCommands.Add(new CommandBarSeparator());
        bar.PrimaryCommands.Add(toggle);
        var window = Shots.Show(bar, 420, 100);
        Shots.Capture(window, "commandbar");

        Assert.True(Union.GetNode(toggle)!.ChainText.Contains("toolbar.header > toolbutton[display=text-only]:checked"), Union.GetNode(toggle)!.ChainText);
        var separator = bar.GetVisualDescendants().OfType<CommandBarSeparator>().Single();
        Assert.Contains("vertical", Union.GetNode(separator)!.Hints);
        window.Close();
    }
}
