using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ActionTextFieldTests
{
    [AvaloniaFact]
    public void Actions_Are_Square_Buttons_Inside_The_Field()
    {
        var cancel = new TextFieldAction { IconName = "edit-delete-remove", Text = "Cancel" };
        var confirm = new TextFieldAction { IconName = "checkmark", Text = "Confirm", IsVisible = false };
        var field = new ActionTextField { PlaceholderText = "ActionTextField", Height = 40, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
        field.RightActions.Add(cancel);
        field.RightActions.Add(confirm);
        var window = Shots.Show(field, 400, 100);
        Assert.Equal("textfield", Union.GetNode(field)!.Type);

        var buttons = field.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(2, buttons.Count);
        var button = buttons[0];
        Assert.Contains("textfield > toolbutton", Union.GetNode(button)!.ChainText);
        Assert.Equal(38, button.Bounds.Height);
        Assert.Equal(38, button.Bounds.Width);
        Assert.Equal(299, button.TranslatePoint(new Point(button.Bounds.Width, 0), field)!.Value.X, 0.5);
        Assert.False(buttons[1].IsVisible);
        Assert.Equal("Cancel", ToolTip.GetTip(button));
        Shots.Capture(window, "actiontextfield");

        var triggered = 0;
        cancel.Triggered += (_, _) => triggered++;
        var point = button.TranslatePoint(new Point(19, 19), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.Equal(1, triggered);

        confirm.IsVisible = true;
        Shots.Settle();
        Assert.True(buttons[1].IsVisible);
        Assert.True(buttons[1].Bounds.Width > 0);
        window.Close();
    }
}
