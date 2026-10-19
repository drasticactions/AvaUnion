using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaUnion.Presenters;

namespace AvaUnion.Controls.Primitives;

internal sealed class ActionRow : Panel
{
    private const double Spacing = 4;

    public ActionRow() => IsVisible = false;

    public void Update(IReadOnlyList<TextFieldAction> actions)
    {
        Children.Clear();
        foreach (var action in actions)
        {
            var icon = new UnionIcon { Width = 16, Height = 16 };
            var button = new Button { Content = icon, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            Union.SetType(button, "toolbutton");
            icon.Bind(UnionIcon.IconNameProperty, action.GetObservable(TextFieldAction.IconNameProperty));
            button.Bind(ToolTip.TipProperty, action.GetObservable(TextFieldAction.TextProperty));
            button.Bind(AutomationProperties.NameProperty, action.GetObservable(TextFieldAction.TextProperty));
            button.Bind(IsVisibleProperty, action.GetObservable(TextFieldAction.IsVisibleProperty));
            button.Bind(IsEnabledProperty, action.GetObservable(TextFieldAction.IsEnabledProperty));
            button.Click += (_, _) => action.Trigger();
            Children.Add(button);
        }

        IsVisible = Children.Count > 0;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }

        if (!double.IsInfinity(availableSize.Height))
        {
            height = Math.Max(height, availableSize.Height);
        }

        return new Size(RowWidth(height), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var side = Math.Max(child.DesiredSize.Width, finalSize.Height);
            child.Arrange(new Rect(x, 0, side, finalSize.Height));
            x += side + Spacing;
        }

        return finalSize;
    }

    private double RowWidth(double height)
    {
        var width = 0.0;
        var count = 0;
        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                width += Math.Max(child.DesiredSize.Width, height);
                count++;
            }
        }

        return count == 0 ? 0 : width + (Spacing * (count - 1));
    }
}
