using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaUnion.Engine;

namespace AvaUnion.Elements;

internal static class PopupShadowSpace
{
    public static void Register()
    {
        Union.StyleProperty.Changed.AddClassHandler<Control>(OnStyleChanged);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private static void OnPointerPressed(TopLevel _, PointerPressedEventArgs e)
    {
        if (e.Handled || TopLevel.GetTopLevel(e.Source as Visual) is not PopupRoot root
            || root.Content is not Control { Margin: var margin } content || margin == default)
        {
            return;
        }

        var point = e.GetPosition(content);
        if (new Rect(content.Bounds.Size).Contains(point))
        {
            return;
        }

        if (ChainBuilder.OwningPopup(root) is { IsLightDismissEnabled: true } popup)
        {
            e.Handled = true;
            if (popup.TemplatedParent is MenuItem item && item.FindLogicalAncestorOfType<MenuBase>() is { } menu)
            {
                menu.Close();
            }
            else
            {
                popup.Close();
            }
        }
    }

    private static void OnStyleChanged(Control element, AvaloniaPropertyChangedEventArgs e)
    {
        if (Union.GetType(element) is null || HostedContent(element) is not { } content)
        {
            return;
        }

        var extents = Extents(Union.GetStyle(element));
        if ((content as StyledElement)?.Parent is Popup popup)
        {
            extents = AwayFromAnchor(extents, popup.Placement);
        }

        content.SetCurrentValue(Layoutable.MarginProperty, extents);
    }

    private static Thickness AwayFromAnchor(Thickness extents, PlacementMode placement) => placement switch
    {
        PlacementMode.Bottom or PlacementMode.BottomEdgeAlignedLeft or PlacementMode.BottomEdgeAlignedRight
            => new Thickness(extents.Left, 0, extents.Right, extents.Bottom),
        PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight
            => new Thickness(extents.Left, extents.Top, extents.Right, 0),
        PlacementMode.Right or PlacementMode.RightEdgeAlignedTop or PlacementMode.RightEdgeAlignedBottom
            => new Thickness(0, extents.Top, extents.Right, extents.Bottom),
        PlacementMode.Left or PlacementMode.LeftEdgeAlignedTop or PlacementMode.LeftEdgeAlignedBottom
            => new Thickness(extents.Left, extents.Top, 0, extents.Bottom),
        _ => extents,
    };

    private static Control? HostedContent(Control element)
    {
        Visual? current = element;
        while (current is not null)
        {
            if (!ReferenceEquals(current, element) && current is StyledElement styled && Union.GetType(styled) is not null)
            {
                return null;
            }

            var parent = current.GetVisualParent();
            if (parent is ContentPresenter { TemplatedParent: PopupRoot or OverlayPopupHost } && current is Control content)
            {
                return content;
            }

            current = parent;
        }

        return null;
    }

    public static Thickness Extents(ResolvedStyle? style)
    {
        var shadow = style?.Shadow;
        if (shadow is null || shadow.Color is not { A: > 0 })
        {
            return default;
        }

        var reach = (shadow.Size ?? 0.0) + (shadow.Blur ?? 0.0);
        if (reach <= 0)
        {
            return default;
        }

        var offset = shadow.Offset.ToVector();
        return new Thickness(
            Math.Ceiling(Math.Max(0, reach - offset.X)),
            Math.Ceiling(Math.Max(0, reach - offset.Y)),
            Math.Ceiling(Math.Max(0, reach + offset.X)),
            Math.Ceiling(Math.Max(0, reach + offset.Y)));
    }
}
