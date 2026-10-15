using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DecorationsShotTests
{
    private static Visual ForceDrawnDecorations(Window window)
    {
        var host = (Visual)typeof(TopLevel).GetProperty("TopLevelHost", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        var update = host.GetType().GetMethod("UpdateDrawnDecorations", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var partsType = Nullable.GetUnderlyingType(update.GetParameters()[0].ParameterType)!;
        update.Invoke(host, [Enum.ToObject(partsType, 0xFF), window.WindowState, null]);
        return host;
    }

    [AvaloniaFact]
    public void Drawn_Title_Bar_Uses_Union_Title_Bar_Buttons()
    {
        var window = Shots.Show(new TextBlock { Text = "content" }, 420, 200);
        var host = ForceDrawnDecorations(window);
        Shots.Settle();
        Shots.Capture(window, "decorations");

        var close = host.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
        var node = Union.GetNode(close)!;
        Assert.True(node.ChainText.Contains("titlebar > closebutton"), node.ChainText);
        Assert.Equal("window-close-symbolic", node.Resolved.Icon.Name);

        var maximize = Union.GetNode(host.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_MaximizeButton"))!;
        Assert.Equal("window-maximize-symbolic", maximize.Resolved.Icon.Name);
    }
}
