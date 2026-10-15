using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaUnion.Presenters;

/// <summary>
/// The indeterminate progress track of Union. The panel gives its children a
/// quarter of its length, at <see cref="Position"/>.
/// </summary>
public class IndeterminateTrackPanel : Panel
{
    /// <summary>Where the track is, from 0 (start) to 1 (end).</summary>
    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<IndeterminateTrackPanel, double>(nameof(Position));

    /// <summary>The orientation.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<IndeterminateTrackPanel, Orientation>(nameof(Orientation));

    static IndeterminateTrackPanel()
    {
        AffectsArrange<IndeterminateTrackPanel>(PositionProperty, OrientationProperty);
    }

    /// <summary>Where the track is, from 0 (start) to 1 (end).</summary>
    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>The orientation.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var p = Math.Clamp(Position, 0, 1);
        foreach (var child in Children)
        {
            if (Orientation == Orientation.Horizontal)
            {
                var w = finalSize.Width * 0.25;
                child.Arrange(new Rect(p * (finalSize.Width - w), 0, w, finalSize.Height));
            }
            else
            {
                var h = finalSize.Height * 0.25;
                child.Arrange(new Rect(0, p * (finalSize.Height - h), finalSize.Width, h));
            }
        }

        return finalSize;
    }
}
