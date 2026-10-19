using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class BusyIndicatorTests
{
    [AvaloniaFact]
    public void Shows_The_Turning_Icon_Only_While_Running()
    {
        var busy = new BusyIndicator();
        var window = Shots.Show(busy);
        var icon = busy.GetVisualDescendants().OfType<UnionIcon>().Single();
        Assert.Equal("busyindicator", Union.GetNode(busy)!.Type);
        Assert.Equal(36, busy.Bounds.Width);
        Assert.Equal(36, busy.Bounds.Height);
        Assert.Equal("process-working-symbolic", icon.EffectiveName);
        Assert.Equal(1, icon.Opacity);
        Shots.Capture(window, "busyindicator");

        busy.IsRunning = false;
        Shots.Settle();
        Assert.Equal(0, icon.Opacity);
        Assert.Equal(AutomationControlType.ProgressBar, ControlAutomationPeer.CreatePeerForElement(busy).GetAutomationControlType());
        window.Close();
    }
}
