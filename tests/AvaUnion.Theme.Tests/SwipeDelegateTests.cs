using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SwipeDelegateTests
{
    [AvaloniaFact]
    public void Dragging_Past_Half_Opens()
    {
        var item = new SwipeDelegate { Content = "Swipe me!", Width = 200, RightContent = new Rectangle { Fill = Brushes.Blue } };
        var completed = 0;
        item.Completed += (_, _) => completed++;
        var window = Shots.Show(item, 300, 100);
        Assert.Contains("swipedelegate.hover-enabled", Union.GetNode(item)!.ChainText);
        var right = item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_RightContent");
        Assert.False(right.IsVisible);

        var origin = item.TranslatePoint(default, window)!.Value;
        window.MouseDown(origin + new Point(150, 10), MouseButton.Left);
        window.MouseMove(origin + new Point(110, 10));
        Assert.Equal(-0.2, item.SwipePosition, 3);
        Assert.True(right.IsVisible);
        window.MouseUp(origin + new Point(110, 10), MouseButton.Left);
        Assert.Equal(0, item.SwipePosition);
        Assert.Equal(0, completed);

        window.MouseDown(origin + new Point(180, 10), MouseButton.Left);
        window.MouseMove(origin + new Point(40, 10));
        window.MouseUp(origin + new Point(40, 10), MouseButton.Left);
        Assert.Equal(-1, item.SwipePosition);
        Assert.True(item.IsOpen);
        Assert.Equal(1, completed);
        Shots.Capture(window, "swipedelegate");

        window.MouseDown(origin + new Point(20, 10), MouseButton.Left);
        window.MouseMove(origin + new Point(190, 10));
        window.MouseUp(origin + new Point(190, 10), MouseButton.Left);
        Assert.Equal(0, item.SwipePosition);
        window.Close();
    }

    [AvaloniaFact]
    public void Keys_And_Peer_Open_And_Close()
    {
        var item = new SwipeDelegate { Content = "Swipe me!", Width = 200, RightContent = new Rectangle { Fill = Brushes.Blue } };
        var window = Shots.Show(item, 300, 100);
        item.Focusable = true;
        item.Focus();
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.Equal(0, item.SwipePosition);
        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.Equal(-1, item.SwipePosition);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.Equal(0, item.SwipePosition);

        var peer = ControlAutomationPeer.CreatePeerForElement(item);
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;
        expand.Expand();
        Assert.True(item.IsOpen);
        expand.Collapse();
        Assert.False(item.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Press_Does_Not_Take_The_Focus()
    {
        var item = new SwipeDelegate { Content = "Swipe me!" };
        var window = Shots.Show(item, 300, 100);
        var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        Shots.Settle();
        Assert.False(item.IsFocused);
        var node = Union.GetNode(item)!;
        Assert.True(node.States.HasFlag(AvaUnion.Engine.UnionStates.Pressed));
        Assert.False(node.States.HasFlag(AvaUnion.Engine.UnionStates.ActiveFocus));
        Assert.False(node.Resolved.Outline.HasAnyValue);
        window.MouseUp(center, MouseButton.Left);
        window.Close();
    }
}
