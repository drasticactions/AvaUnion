using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace AvaUnion.Controls;

/// <summary>
/// A round range control. The user turns the handle with the pointer or
/// changes the value with the arrow, Page Up, Page Down, Home and End keys.
/// </summary>
[PseudoClasses(":pressed")]
public class Dial : RangeBase
{
    /// <summary>Defines the <see cref="StartAngle"/> property.</summary>
    public static readonly StyledProperty<double> StartAngleProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(StartAngle), -140.0);

    /// <summary>Defines the <see cref="EndAngle"/> property.</summary>
    public static readonly StyledProperty<double> EndAngleProperty =
        AvaloniaProperty.Register<Dial, double>(nameof(EndAngle), 140.0);

    /// <summary>Defines the <see cref="IsWrapEnabled"/> property.</summary>
    public static readonly StyledProperty<bool> IsWrapEnabledProperty =
        AvaloniaProperty.Register<Dial, bool>(nameof(IsWrapEnabled));

    /// <summary>Defines the <see cref="Angle"/> property.</summary>
    public static readonly DirectProperty<Dial, double> AngleProperty =
        AvaloniaProperty.RegisterDirect<Dial, double>(nameof(Angle), d => d.Angle);

    private double _angle = -140.0;

    static Dial()
    {
        FocusableProperty.OverrideDefaultValue<Dial>(true);
    }

    /// <summary>The angle of the handle at <see cref="RangeBase.Minimum"/>, in degrees clockwise from the top. The default is -140.</summary>
    public double StartAngle
    {
        get => GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    /// <summary>The angle of the handle at <see cref="RangeBase.Maximum"/>, in degrees clockwise from the top. The default is 140.</summary>
    public double EndAngle
    {
        get => GetValue(EndAngleProperty);
        set => SetValue(EndAngleProperty, value);
    }

    /// <summary>True when the handle can turn past the end to the start, and back. The default is false.</summary>
    public bool IsWrapEnabled
    {
        get => GetValue(IsWrapEnabledProperty);
        set => SetValue(IsWrapEnabledProperty, value);
    }

    /// <summary>The angle of the handle for the value, in degrees clockwise from the top.</summary>
    public double Angle
    {
        get => _angle;
        private set => SetAndRaise(AngleProperty, ref _angle, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty || change.Property == MinimumProperty || change.Property == MaximumProperty
            || change.Property == StartAngleProperty || change.Property == EndAngleProperty)
        {
            var range = Maximum - Minimum;
            var position = range > 0 ? (Value - Minimum) / range : 0.0;
            Angle = StartAngle + (position * (EndAngle - StartAngle));
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

        e.Pointer.Capture(this);
        PseudoClasses.Set(":pressed", true);
        Focus(NavigationMethod.Pointer);
        SetValueFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            SetValueFromPoint(e.GetPosition(this));
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }

        PseudoClasses.Set(":pressed", false);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double? value = e.Key switch
        {
            Key.Up or Key.Right => Value + SmallChange,
            Key.Down or Key.Left => Value - SmallChange,
            Key.PageUp => Value + LargeChange,
            Key.PageDown => Value - LargeChange,
            Key.Home => Minimum,
            Key.End => Maximum,
            _ => null,
        };
        if (value is { } v)
        {
            SetCurrentValue(ValueProperty, Math.Clamp(v, Minimum, Maximum));
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.DialAutomationPeer(this);

    private void SetValueFromPoint(Point point)
    {
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5)
        {
            return;
        }

        var angle = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        if (FlowDirection == Avalonia.Media.FlowDirection.RightToLeft)
        {
            angle = -angle;
        }

        var start = StartAngle;
        var end = EndAngle;
        while (angle < start)
        {
            angle += 360.0;
        }

        while (angle > start + 360.0)
        {
            angle -= 360.0;
        }

        if (angle > end)
        {
            if (!IsWrapEnabled && !(Angle > (start + end) / 2))
            {
                angle = start;
            }
            else if (!IsWrapEnabled || angle - end < (start + 360.0 - angle))
            {
                angle = end;
            }
            else
            {
                angle = start;
            }
        }

        var span = end - start;
        var position = span != 0 ? (angle - start) / span : 0.0;
        SetCurrentValue(ValueProperty, Minimum + (position * (Maximum - Minimum)));
    }
}
