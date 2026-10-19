using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using AvaUnion.Controls.Primitives;

namespace AvaUnion.Controls;

/// <summary>
/// A text field with action buttons inside it, at the start, the end or both.
/// Each action shows as an icon button with its text as a tool tip.
/// </summary>
public class ActionTextField : TextBox
{
    private const double Gap = 4;

    private readonly ActionRow _left = new();
    private readonly ActionRow _right = new();

    /// <summary>Creates the field.</summary>
    public ActionTextField()
    {
        LeftActions.CollectionChanged += (_, _) => _left.Update(LeftActions);
        RightActions.CollectionChanged += (_, _) => _right.Update(RightActions);
        InnerLeftContent = _left;
        InnerRightContent = _right;
    }

    /// <summary>The actions at the start of the field.</summary>
    public AvaloniaList<TextFieldAction> LeftActions { get; } = [];

    /// <summary>The actions at the end of the field.</summary>
    public AvaloniaList<TextFieldAction> RightActions { get; } = [];

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PaddingProperty)
        {
            var p = Padding;
            _left.Margin = new Thickness(1 - p.Left, 1 - p.Top, Gap, 1 - p.Bottom);
            _right.Margin = new Thickness(Gap, 1 - p.Top, 1 - p.Right, 1 - p.Bottom);
        }
    }
}
