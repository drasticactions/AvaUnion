using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaUnion.Elements;

namespace AvaUnion.Controls;

/// <summary>
/// A list item that the user drags sideways to show the content behind it.
/// Dragging to the right shows <see cref="LeftContent"/> and dragging to the
/// left shows <see cref="RightContent"/>. Past half the width the item stays
/// open and <see cref="Completed"/> occurs; otherwise it closes again. When
/// the item has focus, the Left and Right keys open it and Escape closes it.
/// </summary>
[PseudoClasses(":pressed", ":reveal-left", ":reveal-right")]
public class SwipeDelegate : ContentControl
{
    /// <summary>Defines the <see cref="LeftContent"/> property.</summary>
    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<SwipeDelegate, object?>(nameof(LeftContent));

    /// <summary>Defines the <see cref="RightContent"/> property.</summary>
    public static readonly StyledProperty<object?> RightContentProperty =
        AvaloniaProperty.Register<SwipeDelegate, object?>(nameof(RightContent));

    /// <summary>Defines the <see cref="SwipePosition"/> property.</summary>
    public static readonly DirectProperty<SwipeDelegate, double> SwipePositionProperty =
        AvaloniaProperty.RegisterDirect<SwipeDelegate, double>(nameof(SwipePosition), s => s.SwipePosition);

    /// <summary>Defines the <see cref="Completed"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CompletedEvent =
        RoutedEvent.Register<SwipeDelegate, RoutedEventArgs>(nameof(Completed), RoutingStrategies.Bubble);

    private double _position;
    private Point? _start;
    private double _startPosition;

    static SwipeDelegate()
    {
        FocusableProperty.OverrideDefaultValue<SwipeDelegate>(true);
        UnionHintProviders.Register<SwipeDelegate>(
            static (element, _, hints, attributes) =>
            {
                var item = (SwipeDelegate)element;
                hints.Add("hover-enabled");
                if (AutoHints.HasIcon(item.Content))
                {
                    hints.Add("with-icon");
                }

                attributes.Add(new("display", AutoHints.DisplayOf(item.Content)));
            },
            ContentProperty);
    }

    /// <summary>The content behind the item on the left. Dragging to the right shows it.</summary>
    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    /// <summary>The content behind the item on the right. Dragging to the left shows it.</summary>
    public object? RightContent
    {
        get => GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    /// <summary>How far the item is open, from -1 (left content hidden, right content fully shown) to 1. Zero is closed.</summary>
    public double SwipePosition
    {
        get => _position;
        private set
        {
            SetAndRaise(SwipePositionProperty, ref _position, value);
            PseudoClasses.Set(":reveal-left", value > 0);
            PseudoClasses.Set(":reveal-right", value < 0);
        }
    }

    /// <summary>True when the item is fully open to one side.</summary>
    public bool IsOpen => Math.Abs(SwipePosition) >= 1.0;

    /// <summary>Occurs when the item opens fully.</summary>
    public event EventHandler<RoutedEventArgs>? Completed
    {
        add => AddHandler(CompletedEvent, value);
        remove => RemoveHandler(CompletedEvent, value);
    }

    /// <summary>Closes the item.</summary>
    public void Close() => SwipePosition = 0.0;

    /// <summary>Opens the item to show <see cref="LeftContent"/> when <paramref name="left"/> is true, else <see cref="RightContent"/>.</summary>
    public void Open(bool left)
    {
        if ((left ? LeftContent : RightContent) is null)
        {
            return;
        }

        SwipePosition = left ? 1.0 : -1.0;
        RaiseEvent(new RoutedEventArgs(CompletedEvent));
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _start = e.GetPosition(this);
        _startPosition = SwipePosition;
        e.Pointer.Capture(this);
        PseudoClasses.Set(":pressed", true);
        Focus(NavigationMethod.Pointer);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_start is not { } start || !ReferenceEquals(e.Pointer.Captured, this) || Bounds.Width <= 0)
        {
            return;
        }

        var position = _startPosition + ((e.GetPosition(this).X - start.X) / Bounds.Width);
        var min = RightContent is null ? 0.0 : -1.0;
        var max = LeftContent is null ? 0.0 : 1.0;
        SwipePosition = Math.Clamp(position, min, max);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_start is null)
        {
            return;
        }

        _start = null;
        e.Pointer.Capture(null);
        PseudoClasses.Set(":pressed", false);
        Settle();
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
        if (_start is not null)
        {
            _start = null;
            Settle();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Right when LeftContent is not null:
                Open(left: true);
                e.Handled = true;
                break;
            case Key.Left when RightContent is not null:
                Open(left: false);
                e.Handled = true;
                break;
            case Key.Escape when SwipePosition != 0:
                Close();
                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.SwipeDelegateAutomationPeer(this);

    private void Settle()
    {
        if (Math.Abs(SwipePosition) > 0.5)
        {
            var wasOpen = IsOpen;
            SwipePosition = Math.Sign(SwipePosition);
            if (!wasOpen)
            {
                RaiseEvent(new RoutedEventArgs(CompletedEvent));
            }
        }
        else
        {
            SwipePosition = 0.0;
        }
    }
}
