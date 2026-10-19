using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;

namespace AvaUnion.Controls;

/// <summary>
/// A column of items that the user turns like a wheel to select one. The
/// selected item is in the middle. The user drags the column, turns the mouse
/// wheel or presses the Up and Down keys.
/// </summary>
public class Tumbler : SelectingItemsControl
{
    /// <summary>Defines the <see cref="VisibleItemCount"/> property.</summary>
    public static readonly StyledProperty<int> VisibleItemCountProperty =
        AvaloniaProperty.Register<Tumbler, int>(nameof(VisibleItemCount), 5, coerce: (_, v) => Math.Max(1, v));

    /// <summary>Defines the <see cref="IsWrapEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsWrapEnabledProperty =
        AvaloniaProperty.Register<Tumbler, bool>(nameof(IsWrapEnabled), true);

    /// <summary>Defines the <see cref="Position"/> property.</summary>
    public static readonly DirectProperty<Tumbler, double> PositionProperty =
        AvaloniaProperty.RegisterDirect<Tumbler, double>(nameof(Position), t => t.Position);

    private double _position;
    private Point? _dragStart;
    private double _dragPosition;

    static Tumbler()
    {
        FocusableProperty.OverrideDefaultValue<Tumbler>(true);
        SelectionModeProperty.OverrideDefaultValue<Tumbler>(SelectionMode.Single | SelectionMode.AlwaysSelected);
    }

    /// <summary>How many items show at the same time. The default is 5.</summary>
    public int VisibleItemCount
    {
        get => GetValue(VisibleItemCountProperty);
        set => SetValue(VisibleItemCountProperty, value);
    }

    /// <summary>
    /// True when the last item is followed by the first one. The items wrap
    /// only when there are more of them than <see cref="VisibleItemCount"/>.
    /// The default is true.
    /// </summary>
    public bool IsWrapEnabled
    {
        get => GetValue(IsWrapEnabledProperty);
        set => SetValue(IsWrapEnabledProperty, value);
    }

    /// <summary>The index of the item in the middle. It has a fraction while the user drags the items.</summary>
    public double Position
    {
        get => _position;
        private set => SetAndRaise(PositionProperty, ref _position, value);
    }

    internal bool Wraps => IsWrapEnabled && ItemCount > VisibleItemCount;

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new TumblerItem();

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<TumblerItem>(item, out recycleKey);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty && _dragStart is null)
        {
            Position = Math.Max(0, SelectedIndex);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragStart = e.GetPosition(this);
        _dragPosition = Position;
        e.Pointer.Capture(this);
        Focus(NavigationMethod.Pointer);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragStart is { } start && ReferenceEquals(e.Pointer.Captured, this) && ItemCount > 0)
        {
            var itemHeight = Bounds.Height / VisibleItemCount;
            var position = _dragPosition - ((e.GetPosition(this).Y - start.Y) / Math.Max(1.0, itemHeight));
            Position = Wraps ? Mod(position, ItemCount) : Math.Clamp(position, 0, ItemCount - 1);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragStart is null)
        {
            return;
        }

        _dragStart = null;
        e.Pointer.Capture(null);
        Select((int)Math.Round(Position));
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragStart is not null)
        {
            _dragStart = null;
            Select((int)Math.Round(Position));
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y != 0)
        {
            Select(SelectedIndex + (e.Delta.Y > 0 ? -1 : 1));
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down)
        {
            Select(SelectedIndex + (e.Key == Key.Up ? -1 : 1));
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TumblerAutomationPeer(this);

    private static double Mod(double value, int count) => ((value % count) + count) % count;

    private void Select(int index)
    {
        if (ItemCount == 0)
        {
            return;
        }

        index = Wraps ? (int)Mod(index, ItemCount) : Math.Clamp(index, 0, ItemCount - 1);
        SelectedIndex = index;
        Position = index;
    }
}
