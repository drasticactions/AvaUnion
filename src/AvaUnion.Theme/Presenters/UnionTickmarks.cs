using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaUnion.Elements;
using AvaUnion.Engine;

namespace AvaUnion.Presenters;

/// <summary>
/// The tick marks of a slider. Each tick is one <c>tickmark</c> element at
/// the position of the handle center for that value. Ticks at or below the
/// value get the <c>active</c> hint.
/// </summary>
public class UnionTickmarks : Panel
{
    private const int MaxTicks = 1000;

    /// <summary>Defines <see cref="Minimum"/>.</summary>
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<UnionTickmarks, double>(nameof(Minimum));

    /// <summary>Defines <see cref="Maximum"/>.</summary>
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<UnionTickmarks, double>(nameof(Maximum), 100.0);

    /// <summary>Defines <see cref="Value"/>.</summary>
    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<UnionTickmarks, double>(nameof(Value));

    /// <summary>Defines <see cref="TickFrequency"/>.</summary>
    public static readonly StyledProperty<double> TickFrequencyProperty = AvaloniaProperty.Register<UnionTickmarks, double>(nameof(TickFrequency));

    /// <summary>Defines <see cref="Ticks"/>.</summary>
    public static readonly StyledProperty<AvaloniaList<double>?> TicksProperty = AvaloniaProperty.Register<UnionTickmarks, AvaloniaList<double>?>(nameof(Ticks));

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<UnionTickmarks, Orientation>(nameof(Orientation));

    /// <summary>Defines <see cref="IsDirectionReversed"/>.</summary>
    public static readonly StyledProperty<bool> IsDirectionReversedProperty = AvaloniaProperty.Register<UnionTickmarks, bool>(nameof(IsDirectionReversed));

    /// <summary>Defines <see cref="HandleLength"/>.</summary>
    public static readonly StyledProperty<double> HandleLengthProperty = AvaloniaProperty.Register<UnionTickmarks, double>(nameof(HandleLength));

    private readonly List<double> _values = [];

    static UnionTickmarks()
    {
        AffectsArrange<UnionTickmarks>(IsDirectionReversedProperty, HandleLengthProperty);
        AffectsMeasure<UnionTickmarks>(OrientationProperty);
    }

    /// <summary>The minimum of the slider.</summary>
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <summary>The maximum of the slider.</summary>
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <summary>The value of the slider. Ticks at or below this value are active.</summary>
    public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The distance between ticks, used when <see cref="Ticks"/> is empty.</summary>
    public double TickFrequency { get => GetValue(TickFrequencyProperty); set => SetValue(TickFrequencyProperty, value); }

    /// <summary>Explicit tick values.</summary>
    public AvaloniaList<double>? Ticks { get => GetValue(TicksProperty); set => SetValue(TicksProperty, value); }

    /// <summary>The orientation of the slider.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>True when the maximum is at the start.</summary>
    public bool IsDirectionReversed { get => GetValue(IsDirectionReversedProperty); set => SetValue(IsDirectionReversedProperty, value); }

    /// <summary>The length of the handle along the groove. The ticks are between the handle centers at the two ends.</summary>
    public double HandleLength { get => GetValue(HandleLengthProperty); set => SetValue(HandleLengthProperty, value); }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MinimumProperty || change.Property == MaximumProperty
            || change.Property == TickFrequencyProperty || change.Property == TicksProperty)
        {
            Rebuild();
        }
        else if (change.Property == ValueProperty)
        {
            UpdateActive();
        }
    }

    private void Rebuild()
    {
        _values.Clear();
        double min = Minimum, max = Maximum;
        if (max > min)
        {
            if (Ticks is { Count: > 0 } ticks)
            {
                foreach (var t in ticks)
                {
                    if (t >= min && t <= max && _values.Count < MaxTicks)
                    {
                        _values.Add(t);
                    }
                }
            }
            else if (TickFrequency > 0)
            {
                for (var i = 0; i < MaxTicks; i++)
                {
                    var t = min + (i * TickFrequency);
                    if (t > max + 1e-9)
                    {
                        break;
                    }

                    _values.Add(Math.Min(t, max));
                }
            }
        }

        if (Children.Count != _values.Count)
        {
            Children.Clear();
            for (var i = 0; i < _values.Count; i++)
            {
                var tick = new UnionBox();
                Union.SetType(tick, "tickmark");
                Union.SetStateSource(tick, UnionStateSource.None);
                Children.Add(tick);
            }
        }

        UpdateActive();
        InvalidateArrange();
    }

    private void UpdateActive()
    {
        var value = Value;
        for (var i = 0; i < Children.Count && i < _values.Count; i++)
        {
            Union.SetHints(Children[i], _values[i] <= value + 1e-9 ? "active" : null);
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var extent = 0.0;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            var margins = Margins(child);
            extent = Math.Max(extent, Orientation == Orientation.Horizontal
                ? child.DesiredSize.Height + margins.Top + margins.Bottom
                : child.DesiredSize.Width + margins.Left + margins.Right);
        }

        return Orientation == Orientation.Horizontal ? new Size(0, extent) : new Size(extent, 0);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var length = horizontal ? finalSize.Width : finalSize.Height;
        var handle = Math.Min(HandleLength, length);
        var range = Maximum - Minimum;
        for (var i = 0; i < Children.Count && i < _values.Count; i++)
        {
            var child = Children[i];
            var size = child.DesiredSize;
            var margins = Margins(child);
            var fraction = range > 0 ? (_values[i] - Minimum) / range : 0.0;

            if (IsDirectionReversed == horizontal)
            {
                fraction = 1.0 - fraction;
            }

            var center = (handle / 2) + (fraction * (length - handle));
            var rect = horizontal
                ? new Rect(center - (size.Width / 2), margins.Top, size.Width, size.Height)
                : new Rect(margins.Left, center - (size.Height / 2), size.Width, size.Height);
            child.Arrange(rect);
        }

        return finalSize;
    }

    private static Thickness Margins(Control child) => (Union.GetStyle(child) ?? ResolvedStyle.Unmatched).Layout.Margins.ToThickness();
}
