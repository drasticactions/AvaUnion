using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaUnion.Elements;

internal static class TabWheel
{
    private static readonly AttachedProperty<double> PendingProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, ScrollViewer, double>("PendingWheel");

    public static void Register()
    {
        InputElement.PointerWheelChangedEvent.AddClassHandler<ScrollViewer>(OnWheel, RoutingStrategies.Tunnel);
    }

    private static void OnWheel(ScrollViewer strip, PointerWheelEventArgs e)
    {
        if (strip.Name != "PART_TabScroll" || strip.TemplatedParent is not TabControl tabs)
        {
            return;
        }

        e.Handled = true;
        var delta = Math.Abs(e.Delta.Y) >= Math.Abs(e.Delta.X) ? e.Delta.Y : e.Delta.X;
        var pending = strip.GetValue(PendingProperty);
        if (Math.Sign(pending) == -Math.Sign(delta))
        {
            pending = 0;
        }

        pending += delta;
        var steps = (int)Math.Truncate(pending);
        strip.SetValue(PendingProperty, pending - steps);
        for (var i = 0; i < Math.Abs(steps); i++)
        {
            Step(tabs, steps < 0 ? 1 : -1);
        }
    }

    private static void Step(TabControl tabs, int direction)
    {
        for (var index = tabs.SelectedIndex + direction; index >= 0 && index < tabs.ItemCount; index += direction)
        {
            if (tabs.ContainerFromIndex(index) is not { IsEffectivelyEnabled: false })
            {
                tabs.SelectedIndex = index;
                return;
            }
        }
    }
}
