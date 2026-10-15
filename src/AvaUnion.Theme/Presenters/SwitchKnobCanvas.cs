using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Presenters;

/// <summary>
/// The track of the handle of a switch.
/// Its width is the available width minus the width of the handle.
/// The canvas centers the handle vertically.
/// </summary>
public class SwitchKnobCanvas : Canvas
{
    private double _lastWidth = double.NaN;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
        }

        return default;
    }

    /// <summary>Uses the arranged width minus the width of the handle. This is the travel.</summary>
    protected override void ArrangeCore(Rect finalRect)
    {
        var knob = 0.0;
        foreach (var child in Children)
        {
            knob = Math.Max(knob, child.DesiredSize.Width);
        }

        base.ArrangeCore(finalRect.WithWidth(Math.Max(0, finalRect.Width - knob)));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width != _lastWidth)
        {
            if (TemplatedParent is ToggleSwitch { IsChecked: true })
            {
                foreach (var child in Children)
                {
                    var left = GetLeft(child);
                    if (double.IsNaN(left) || left == 0 || left == _lastWidth)
                    {
                        SetLeft(child, finalSize.Width);
                    }
                }
            }

            _lastWidth = finalSize.Width;
        }

        foreach (var child in Children)
        {
            var left = GetLeft(child);
            var x = double.IsNaN(left) ? 0 : left;
            var y = (finalSize.Height - child.DesiredSize.Height) / 2;
            child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
        }

        return finalSize;
    }
}
