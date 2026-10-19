using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaUnion.Controls;

/// <summary>
/// Gives the tabs of a <see cref="TabControl"/> a close button. A click on it
/// raises <see cref="CloseRequestedEvent"/>. The application removes the tab.
/// </summary>
public static class ClosableTabs
{
    /// <summary>Defines the IsEnabled attached property. Set it on a tab control or on one tab. Tabs take the value of their tab control.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(ClosableTabs), inherits: true);

    /// <summary>Defines the CloseRequested event. It goes from the tab up to its tab control.</summary>
    public static readonly RoutedEvent<TabCloseRequestedEventArgs> CloseRequestedEvent =
        RoutedEvent.Register<TabCloseRequestedEventArgs>("CloseRequested", RoutingStrategies.Bubble, typeof(ClosableTabs));

    /// <summary>Gets the value of the IsEnabled attached property.</summary>
    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    /// <summary>Sets the value of the IsEnabled attached property.</summary>
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    /// <summary>Adds a handler for the CloseRequested event.</summary>
    public static void AddCloseRequestedHandler(Interactive element, EventHandler<TabCloseRequestedEventArgs> handler) =>
        element.AddHandler(CloseRequestedEvent, handler);

    /// <summary>Removes a handler for the CloseRequested event.</summary>
    public static void RemoveCloseRequestedHandler(Interactive element, EventHandler<TabCloseRequestedEventArgs> handler) =>
        element.RemoveHandler(CloseRequestedEvent, handler);
}
