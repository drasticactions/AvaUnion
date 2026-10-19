using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaUnion.Presenters;

namespace AvaUnion.Controls.Primitives;

internal sealed class DialogButtonBox : Panel
{
    private static readonly (DialogButtons Button, string Role, string Text, string Icon)[] s_order =
    [
        (DialogButtons.Help, "help", "Help", "help-contents"),
        (DialogButtons.Reset, "reset", "Reset", "edit-undo"),
        (DialogButtons.RestoreDefaults, "reset", "Restore Defaults", "document-revert"),
        (DialogButtons.Yes, "yes", "Yes", "dialog-ok-apply"),
        (DialogButtons.YesToAll, "yes", "Yes to All", "dialog-ok"),
        (DialogButtons.No, "no", "No", "dialog-cancel"),
        (DialogButtons.NoToAll, "no", "No to All", "dialog-cancel"),
        (DialogButtons.Ok, "accept", "OK", "dialog-ok"),
        (DialogButtons.Open, "accept", "Open", "document-open"),
        (DialogButtons.Save, "accept", "Save", "document-save"),
        (DialogButtons.SaveAll, "accept", "Save All", "document-save-all"),
        (DialogButtons.Retry, "accept", "Retry", "view-refresh"),
        (DialogButtons.Ignore, "accept", "Ignore", "dialog-cancel"),
        (DialogButtons.Apply, "apply", "Apply", "dialog-ok-apply"),
        (DialogButtons.Discard, "destructive", "Discard", "edit-delete"),
        (DialogButtons.Cancel, "reject", "Cancel", "dialog-cancel"),
        (DialogButtons.Close, "reject", "Close", "dialog-close"),
        (DialogButtons.Abort, "reject", "Abort", "dialog-cancel"),
    ];

    private readonly StackPanel _start = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly StackPanel _end = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

    public DialogButtonBox()
    {
        Union.SetType(this, "dialogbuttonbox");
        Union.SetAttributes(this, Elements.UnionAttributes.Parse("position=footer"));
        Children.Add(new UnionBox());
        Children.Add(_start);
        Children.Add(_end);
    }

    public event Action<DialogButtons>? Clicked;

    public IEnumerable<Button> Buttons
    {
        get
        {
            foreach (var child in _start.Children)
            {
                yield return (Button)child;
            }

            foreach (var child in _end.Children)
            {
                yield return (Button)child;
            }
        }
    }

    public void Update(DialogButtons buttons, Thickness padding, double spacing)
    {
        _start.Children.Clear();
        _end.Children.Clear();
        _start.Margin = _end.Margin = padding;
        _start.Spacing = _end.Spacing = spacing;
        foreach (var (button, role, text, icon) in s_order)
        {
            if (!buttons.HasFlag(button))
            {
                continue;
            }

            var control = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 4,
                    Children = { new UnionIcon { IconName = icon, Width = 16, Height = 16 }, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center } },
                },
            };
            Union.SetAttributes(control, Elements.UnionAttributes.Parse("role=" + role));
            control.Click += (_, _) => Clicked?.Invoke(button);
            (role is "help" or "reset" ? _start : _end).Children.Add(control);
        }

        IsVisible = _start.Children.Count + _end.Children.Count > 0;
    }
}
