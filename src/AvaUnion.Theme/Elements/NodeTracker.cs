using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.VisualTree;

namespace AvaUnion.Elements;

internal static class NodeTracker
{
    private static readonly ConditionalWeakTable<StyledElement, object> s_subscribed = new();

    public static void Update(StyledElement element)
    {
        if (element is not Visual visual)
        {
            return;
        }

        if (!s_subscribed.TryGetValue(element, out _))
        {
            s_subscribed.Add(element, element);
            visual.AttachedToVisualTree += OnAttached;
            visual.DetachedFromVisualTree += OnDetached;
        }

        var type = element.GetValue(Union.TypeProperty);
        var node = element.GetValue(Union.NodeProperty);
        if (type is null)
        {
            if (node is not null)
            {
                Destroy(element, node);
            }

            return;
        }

        if (node is not null)
        {
            node.Refresh();
            return;
        }

        if (visual.IsAttachedToVisualTree())
        {
            Create(element);
        }
    }

    public static void StyleChanged(StyledElement element)
    {
        element.GetValue(Union.NodeProperty)?.InvalidateStyleHandle();
        if (element is Visual v)
        {
            ElementNode.MarkSubtreeDirty(v, includeSelf: false, styleHandles: true);
        }
    }

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is StyledElement element && element.GetValue(Union.TypeProperty) is not null && element.GetValue(Union.NodeProperty) is null)
        {
            Create(element);
        }
    }

    private static void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is StyledElement element && element.GetValue(Union.NodeProperty) is { } node)
        {
            Destroy(element, node);
        }
    }

    private static void Create(StyledElement element)
    {
        var node = new ElementNode(element);
        element.SetValue(Union.NodeProperty, node);
        node.Attach();
    }

    private static void Destroy(StyledElement element, ElementNode node)
    {
        node.Detach();
        element.ClearValue(Union.NodeProperty);
        element.ClearValue(Union.StyleProperty);
    }
}
