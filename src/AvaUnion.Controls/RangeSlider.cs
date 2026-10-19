using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using AvaUnion.Controls.Primitives;
using AvaUnion.Elements;

namespace AvaUnion.Controls;

/// <summary>
/// A slider with two handles that select a range between
/// <see cref="Minimum"/> and <see cref="Maximum"/>.
/// </summary>
[TemplatePart("PART_LowerHandle", typeof(RangeSliderHandle))]
[TemplatePart("PART_UpperHandle", typeof(RangeSliderHandle))]
[PseudoClasses(":horizontal", ":vertical")]
public class RangeSlider : TemplatedControl
{
    /// <summary>Defines the <see cref="Minimum"/> property.</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Minimum));

    /// <summary>Defines the <see cref="Maximum"/> property.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Maximum), 100.0);

    /// <summary>Defines the <see cref="LowerValue"/> property.</summary>
    public static readonly StyledProperty<double> LowerValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(LowerValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay, coerce: CoerceLower);

    /// <summary>Defines the <see cref="UpperValue"/> property.</summary>
    public static readonly StyledProperty<double> UpperValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(UpperValue), 100.0, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay, coerce: CoerceUpper);

    /// <summary>Defines the <see cref="SmallChange"/> property.</summary>
    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(SmallChange), 1.0);

    /// <summary>Defines the <see cref="LargeChange"/> property.</summary>
    public static readonly StyledProperty<double> LargeChangeProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(LargeChange), 10.0);

    /// <summary>Defines the <see cref="TickFrequency"/> property.</summary>
    public static readonly StyledProperty<double> TickFrequencyProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(TickFrequency));

    /// <summary>Defines the <see cref="IsSnapToTickEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty =
        AvaloniaProperty.Register<RangeSlider, bool>(nameof(IsSnapToTickEnabled));

    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<RangeSlider, Orientation>(nameof(Orientation));

    private RangeSliderHandle? _lower;
    private RangeSliderHandle? _upper;
    private RangeSliderHandle? _active;
    private double _dragValue;

    static RangeSlider()
    {
        FocusableProperty.OverrideDefaultValue<RangeSlider>(true);
        UnionHintProviders.Register<RangeSlider>(
            static (element, _, hints, _) => hints.Add(((RangeSlider)element).Orientation == Orientation.Vertical ? "vertical" : "horizontal"),
            OrientationProperty);
    }

    /// <summary>Creates the slider.</summary>
    public RangeSlider() => UpdatePseudoClasses();

    /// <summary>The smallest value. The default is 0.</summary>
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>The largest value. The default is 100.</summary>
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>The value of the first handle. It is not more than <see cref="UpperValue"/>. The default is 0.</summary>
    public double LowerValue
    {
        get => GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    /// <summary>The value of the second handle. It is not less than <see cref="LowerValue"/>. The default is 100.</summary>
    public double UpperValue
    {
        get => GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    /// <summary>How much an arrow key moves a handle. The default is 1.</summary>
    public double SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    /// <summary>How much Page Up and Page Down move a handle. The default is 10.</summary>
    public double LargeChange
    {
        get => GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>The step that the values snap to when <see cref="IsSnapToTickEnabled"/> is true. Zero means no step.</summary>
    public double TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    /// <summary>True when the values snap to multiples of <see cref="TickFrequency"/>. The default is false.</summary>
    public bool IsSnapToTickEnabled
    {
        get => GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }

    /// <summary>The direction of the slider. The default is horizontal. A vertical slider has its minimum at the bottom.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Hook(_lower, false);
        Hook(_upper, false);
        _lower = e.NameScope.Find<RangeSliderHandle>("PART_LowerHandle");
        _upper = e.NameScope.Find<RangeSliderHandle>("PART_UpperHandle");
        Hook(_lower, true);
        Hook(_upper, true);
        _active ??= _lower;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinimumProperty || change.Property == MaximumProperty)
        {
            CoerceValue(LowerValueProperty);
            CoerceValue(UpperValueProperty);
        }
        else if (change.Property == LowerValueProperty)
        {
            CoerceValue(UpperValueProperty);
        }
        else if (change.Property == UpperValueProperty)
        {
            CoerceValue(LowerValueProperty);
        }
        else if (change.Property == OrientationProperty)
        {
            UpdatePseudoClasses();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _lower is null || _upper is null)
        {
            return;
        }

        var value = ValueFromPoint(e.GetPosition(this));
        var handle = Math.Abs(value - LowerValue) <= Math.Abs(value - UpperValue) && !(value > UpperValue) ? _lower : _upper;
        _active = handle;
        SetHandleValue(handle, value);
        Focus(NavigationMethod.Pointer);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var handle = _active ?? _lower;
        if (handle is null)
        {
            return;
        }

        var current = handle.IsUpper ? UpperValue : LowerValue;
        double? value = e.Key switch
        {
            Key.Up or Key.Right => current + SmallChange,
            Key.Down or Key.Left => current - SmallChange,
            Key.PageUp => current + LargeChange,
            Key.PageDown => current - LargeChange,
            Key.Home => Minimum,
            Key.End => Maximum,
            _ => null,
        };
        if (value is { } v)
        {
            SetHandleValue(handle, v);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.RangeSliderAutomationPeer(this);

    internal void SetHandleValue(RangeSliderHandle handle, double value)
    {
        value = Snap(Math.Clamp(value, Minimum, Maximum));
        SetCurrentValue(handle.IsUpper ? UpperValueProperty : LowerValueProperty, value);
    }

    private static double CoerceLower(AvaloniaObject sender, double value)
    {
        var slider = (RangeSlider)sender;
        return Math.Clamp(value, slider.Minimum, Math.Max(slider.Minimum, Math.Min(slider.Maximum, slider.UpperValue)));
    }

    private static double CoerceUpper(AvaloniaObject sender, double value)
    {
        var slider = (RangeSlider)sender;
        return Math.Clamp(value, Math.Min(slider.Maximum, Math.Max(slider.Minimum, slider.LowerValue)), Math.Max(slider.Minimum, slider.Maximum));
    }

    private double Snap(double value)
    {
        if (!IsSnapToTickEnabled || TickFrequency <= 0)
        {
            return value;
        }

        return Math.Clamp(Minimum + (Math.Round((value - Minimum) / TickFrequency) * TickFrequency), Minimum, Maximum);
    }

    private double Travel(Control handle) => Orientation == Orientation.Horizontal
        ? Math.Max(1.0, Bounds.Width - Padding.Left - Padding.Right - handle.Bounds.Width)
        : Math.Max(1.0, Bounds.Height - Padding.Top - Padding.Bottom - handle.Bounds.Height);

    private double ValueFromPoint(Point point)
    {
        var handle = (Control?)_lower ?? this;
        double position;
        if (Orientation == Orientation.Horizontal)
        {
            position = (point.X - Padding.Left - (handle.Bounds.Width / 2)) / Travel(handle);
        }
        else
        {
            position = 1.0 - ((point.Y - Padding.Top - (handle.Bounds.Height / 2)) / Travel(handle));
        }

        return Minimum + (Math.Clamp(position, 0.0, 1.0) * (Maximum - Minimum));
    }

    private void Hook(RangeSliderHandle? handle, bool on)
    {
        if (handle is null)
        {
            return;
        }

        if (on)
        {
            handle.DragStarted += OnDragStarted;
            handle.DragDelta += OnDragDelta;
        }
        else
        {
            handle.DragStarted -= OnDragStarted;
            handle.DragDelta -= OnDragDelta;
        }
    }

    private void OnDragStarted(object? sender, VectorEventArgs e)
    {
        var handle = (RangeSliderHandle)sender!;
        _active = handle;
        _dragValue = handle.IsUpper ? UpperValue : LowerValue;
        Focus(NavigationMethod.Pointer);
    }

    private void OnDragDelta(object? sender, VectorEventArgs e)
    {
        var handle = (RangeSliderHandle)sender!;
        var distance = Orientation == Orientation.Horizontal ? e.Vector.X : -e.Vector.Y;
        _dragValue += distance / Travel(handle) * (Maximum - Minimum);
        SetHandleValue(handle, _dragValue);
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
    }
}
