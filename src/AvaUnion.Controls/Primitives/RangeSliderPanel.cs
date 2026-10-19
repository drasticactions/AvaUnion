using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaUnion.Controls.Primitives;

internal sealed class RangeSliderPanel : Panel
{
    public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<RangeSliderPanel, double>(nameof(Minimum));
    public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<RangeSliderPanel, double>(nameof(Maximum));
    public static readonly StyledProperty<double> LowerValueProperty = AvaloniaProperty.Register<RangeSliderPanel, double>(nameof(LowerValue));
    public static readonly StyledProperty<double> UpperValueProperty = AvaloniaProperty.Register<RangeSliderPanel, double>(nameof(UpperValue));
    public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<RangeSliderPanel, Orientation>(nameof(Orientation));
    public static readonly StyledProperty<Control?> GrooveProperty = AvaloniaProperty.Register<RangeSliderPanel, Control?>(nameof(Groove));
    public static readonly StyledProperty<Control?> FillProperty = AvaloniaProperty.Register<RangeSliderPanel, Control?>(nameof(Fill));
    public static readonly StyledProperty<Control?> LowerHandleProperty = AvaloniaProperty.Register<RangeSliderPanel, Control?>(nameof(LowerHandle));
    public static readonly StyledProperty<Control?> UpperHandleProperty = AvaloniaProperty.Register<RangeSliderPanel, Control?>(nameof(UpperHandle));

    static RangeSliderPanel()
    {
        AffectsArrange<RangeSliderPanel>(MinimumProperty, MaximumProperty, LowerValueProperty, UpperValueProperty);
        AffectsMeasure<RangeSliderPanel>(OrientationProperty);
    }

    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public double LowerValue { get => GetValue(LowerValueProperty); set => SetValue(LowerValueProperty, value); }

    public double UpperValue { get => GetValue(UpperValueProperty); set => SetValue(UpperValueProperty, value); }

    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    public Control? Groove { get => GetValue(GrooveProperty); set => SetValue(GrooveProperty, value); }

    public Control? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    public Control? LowerHandle { get => GetValue(LowerHandleProperty); set => SetValue(LowerHandleProperty, value); }

    public Control? UpperHandle { get => GetValue(UpperHandleProperty); set => SetValue(UpperHandleProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var range = Maximum - Minimum;
        var first = range > 0 ? Math.Clamp((LowerValue - Minimum) / range, 0, 1) : 0.0;
        var second = range > 0 ? Math.Clamp((UpperValue - Minimum) / range, 0, 1) : 0.0;
        var horizontal = Orientation == Orientation.Horizontal;
        var w = finalSize.Width;
        var h = finalSize.Height;

        var groove = Groove?.DesiredSize ?? default;
        var grooveRect = horizontal
            ? new Rect(0, (h - groove.Height) / 2, w, groove.Height)
            : new Rect((w - groove.Width) / 2, 0, groove.Width, h);
        Groove?.Arrange(grooveRect);

        var smaller = Math.Min(grooveRect.Width, grooveRect.Height);
        if (Fill is { } fill)
        {
            var rect = horizontal
                ? new Rect(grooveRect.X + (first * grooveRect.Width) + (smaller / 2), grooveRect.Y, Math.Max(0, ((second - first) * grooveRect.Width) - smaller), smaller)
                : new Rect(grooveRect.X, grooveRect.Y + ((1 - second) * grooveRect.Height) + (smaller / 2), smaller, Math.Max(0, ((second - first) * grooveRect.Height) - smaller));
            fill.Arrange(rect);
        }

        ArrangeHandle(LowerHandle, first, horizontal, w, h);
        ArrangeHandle(UpperHandle, second, horizontal, w, h);
        return finalSize;
    }

    private static void ArrangeHandle(Control? handle, double position, bool horizontal, double w, double h)
    {
        if (handle is null)
        {
            return;
        }

        var size = handle.DesiredSize;
        var rect = horizontal
            ? new Rect(position * (w - size.Width), (h - size.Height) / 2, size.Width, size.Height)
            : new Rect((w - size.Width) / 2, (1 - position) * (h - size.Height), size.Width, size.Height);
        handle.Arrange(rect);
    }
}
