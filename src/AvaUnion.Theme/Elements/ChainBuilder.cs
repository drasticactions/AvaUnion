using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace AvaUnion.Elements;

/// <summary>
/// Builds the root-first chain of a node. The chain contains the typed visual
/// ancestors and skips the untyped ones. The parent of a popup root is the root
/// element of the owner window.
/// </summary>
public static class ChainBuilder
{
    /// <summary>The chain ending at <paramref name="node"/>, root first.</summary>
    public static List<ElementNode> Build(ElementNode node)
    {
        var chain = new List<ElementNode>(8);
        Visual? current = node.Owner as Visual;
        while (current is not null)
        {
            if (current.GetValue(Union.NodeProperty) is { } n)
            {
                chain.Add(n);
            }

            if (IsPopupRoot(current))
            {
                if (OwnerWindow(current) is { } window && window.GetValue(Union.NodeProperty) is { } windowNode && !chain.Contains(windowNode))
                {
                    chain.Add(windowNode);
                }

                break;
            }

            current = current.GetVisualParent();
        }

        chain.Reverse();
        return chain;
    }

    /// <summary>The nearest typed node above <paramref name="node"/>.</summary>
    public static ElementNode? FindParent(ElementNode node)
    {
        var chain = Build(node);
        return chain.Count >= 2 ? chain[^2] : null;
    }

    /// <summary>Returns true when a visual is the root of the visual tree of a popup.</summary>
    public static bool IsPopupRoot(Visual visual) => visual is PopupRoot or OverlayPopupHost;

    /// <summary>The Popup control whose content the popup root hosts.</summary>
    public static Popup? OwningPopup(Visual popupRoot)
    {
        if ((popupRoot as StyledElement)?.Parent is Popup direct)
        {
            return direct;
        }

        var content = popupRoot switch
        {
            PopupRoot root => root.Content,
            OverlayPopupHost host => host.Content,
            _ => null,
        };
        return (content as StyledElement)?.Parent as Popup;
    }

    /// <summary>The top level of a popup root.</summary>
    public static TopLevel? OwnerWindow(Visual popupRoot)
    {
        if (popupRoot is OverlayPopupHost)
        {
            return TopLevel.GetTopLevel(popupRoot);
        }

        var popup = OwningPopup(popupRoot);
        var fromPopup = popup is null ? null : TopLevel.GetTopLevel(popup.PlacementTarget ?? popup);
        return fromPopup ?? (popupRoot as PopupRoot)?.ParentTopLevel;
    }
}
