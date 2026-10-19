using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DialogTests
{
    [AvaloniaFact]
    public void Shows_In_The_Overlay_And_Accepts()
    {
        var content = new Panel();
        var window = Shots.Show(content, 400, 300);
        var dialog = new Dialog { Title = "Title", Content = "Body", StandardButtons = DialogButtons.Ok | DialogButtons.Cancel, IsModal = true };
        var accepted = 0;
        dialog.Accepted += (_, _) => accepted++;
        var task = dialog.ShowAsync(content);
        Shots.Settle();

        Assert.True(dialog.IsOpen);
        Assert.Equal("dialog", Union.GetNode(dialog)!.Type);
        Assert.Contains(window.GetVisualDescendants(), v => v is Panel p && Union.GetNode(p)?.ChainText.EndsWith("overlay.dialog.modal") == true);
        Assert.Equal(200, dialog.Bounds.Center.X, 1.0);
        var header = dialog.GetVisualDescendants().OfType<Label>().Single();
        Assert.Matches(@"dialog(:[a-z-]+)* > label\.header$", Union.GetNode(header)!.ChainText);
        Assert.Equal(20, header.FontSize);
        var buttons = dialog.Buttons.ToList();
        Assert.Equal(2, buttons.Count);
        Assert.Contains("dialogbuttonbox[position=footer] > button", Union.GetNode(buttons[0])!.ChainText);
        Assert.Contains("[role=accept]", Union.GetNode(buttons[0])!.ChainText);
        Assert.Contains("[role=reject]", Union.GetNode(buttons[1])!.ChainText);
        Assert.True(buttons[0].Bounds.X < buttons[1].Bounds.X);
        Assert.Equal(AutomationControlType.Window, ControlAutomationPeer.CreatePeerForElement(dialog).GetAutomationControlType());
        Shots.Capture(window, "dialog");

        var point = buttons[0].TranslatePoint(new Point(5, 5), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.True(task.IsCompleted);
        Assert.True(task.Result);
        Assert.Equal(1, accepted);
        Assert.False(dialog.IsOpen);
        Assert.DoesNotContain(window.GetVisualDescendants(), v => v == dialog);
        window.Close();
    }

    [AvaloniaFact]
    public void Escape_Rejects()
    {
        var content = new Panel();
        var window = Shots.Show(content, 400, 300);
        var dialog = new Dialog { Title = "Title", StandardButtons = DialogButtons.Ok };
        var task = dialog.ShowAsync(content);
        Shots.Settle();
        dialog.Buttons.First().Focus();
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Assert.True(task.IsCompleted);
        Assert.False(task.Result);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_Press_Outside_Rejects(bool modal)
    {
        var content = new Panel { Background = Avalonia.Media.Brushes.Transparent };
        var window = Shots.Show(content, 400, 300);
        var dialog = new Dialog { Title = "Title", StandardButtons = DialogButtons.Ok, IsModal = modal };
        var task = dialog.ShowAsync(content);
        Shots.Settle();
        var inside = dialog.TranslatePoint(new Point(10, 10), window)!.Value;
        window.MouseDown(inside, MouseButton.Left);
        window.MouseUp(inside, MouseButton.Left);
        Assert.False(task.IsCompleted);
        window.MouseDown(new Point(5, 5), MouseButton.Left);
        window.MouseUp(new Point(5, 5), MouseButton.Left);
        Assert.True(task.IsCompleted);
        Assert.False(task.Result);
        window.Close();
    }

    [AvaloniaFact]
    public void Closes_When_The_Owner_Leaves()
    {
        var page = new Panel();
        var root = new ContentControl { Content = page };
        var window = Shots.Show(root, 400, 300);
        var dialog = new Dialog { Title = "Title", StandardButtons = DialogButtons.Ok };
        var task = dialog.ShowAsync(page);
        Shots.Settle();
        root.Content = new Panel();
        Shots.Settle();
        Assert.True(task.IsCompleted);
        Assert.Null(task.Result);
        Assert.DoesNotContain(window.GetVisualDescendants(), v => v == dialog);
        window.Close();
    }

    [AvaloniaFact]
    public void Shows_In_A_Window()
    {
        var owner = Shots.Show(new Panel(), 400, 300);
        var dialog = new Dialog { Title = "Title", Content = "Body", StandardButtons = DialogButtons.Ok };
        var task = dialog.ShowWindowAsync(owner);
        Shots.Settle();
        var window = (Window)TopLevel.GetTopLevel(dialog)!;
        Assert.NotSame(owner, window);
        Assert.Equal("Title", window.Title);
        Assert.False(dialog.GetVisualDescendants().OfType<Label>().Single().IsEffectivelyVisible);
        dialog.Accept();
        Shots.Settle();
        Assert.True(task.IsCompleted);
        Assert.True(task.Result);
        owner.Close();
    }
}
