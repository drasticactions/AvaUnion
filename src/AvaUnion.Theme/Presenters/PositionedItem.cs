using Avalonia;
using AvaUnion.Engine;

namespace AvaUnion.Presenters;

/// <summary>Attached properties for children of a <see cref="UnionPositioner"/>.</summary>
public static class PositionedItem
{
    /// <summary>The resolved alignment that applies to the item.</summary>
    public static readonly AttachedProperty<PositionerSource> SourceProperty =
        AvaloniaProperty.RegisterAttached<Visual, PositionerSource>("Source", typeof(PositionedItem));

    /// <summary>Overrides the horizontal alignment.</summary>
    public static readonly AttachedProperty<UnionAlignment> HorizontalAlignmentProperty =
        AvaloniaProperty.RegisterAttached<Visual, UnionAlignment>("HorizontalAlignment", typeof(PositionedItem));

    /// <summary>Overrides the vertical alignment.</summary>
    public static readonly AttachedProperty<UnionAlignment> VerticalAlignmentProperty =
        AvaloniaProperty.RegisterAttached<Visual, UnionAlignment>("VerticalAlignment", typeof(PositionedItem));

    /// <summary>The minimum width of the item.</summary>
    public static readonly AttachedProperty<double> MinimumWidthProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("MinimumWidth", typeof(PositionedItem));

    /// <summary>The minimum height of the item.</summary>
    public static readonly AttachedProperty<double> MinimumHeightProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("MinimumHeight", typeof(PositionedItem));

    /// <summary>Gets <see cref="SourceProperty"/>.</summary>
    public static PositionerSource GetSource(Visual v) => v.GetValue(SourceProperty);

    /// <summary>Sets <see cref="SourceProperty"/>.</summary>
    public static void SetSource(Visual v, PositionerSource value) => v.SetValue(SourceProperty, value);

    /// <summary>Gets <see cref="HorizontalAlignmentProperty"/>.</summary>
    public static UnionAlignment GetHorizontalAlignment(Visual v) => v.GetValue(HorizontalAlignmentProperty);

    /// <summary>Sets <see cref="HorizontalAlignmentProperty"/>.</summary>
    public static void SetHorizontalAlignment(Visual v, UnionAlignment value) => v.SetValue(HorizontalAlignmentProperty, value);

    /// <summary>Gets <see cref="VerticalAlignmentProperty"/>.</summary>
    public static UnionAlignment GetVerticalAlignment(Visual v) => v.GetValue(VerticalAlignmentProperty);

    /// <summary>Sets <see cref="VerticalAlignmentProperty"/>.</summary>
    public static void SetVerticalAlignment(Visual v, UnionAlignment value) => v.SetValue(VerticalAlignmentProperty, value);

    /// <summary>Gets <see cref="MinimumWidthProperty"/>.</summary>
    public static double GetMinimumWidth(Visual v) => v.GetValue(MinimumWidthProperty);

    /// <summary>Sets <see cref="MinimumWidthProperty"/>.</summary>
    public static void SetMinimumWidth(Visual v, double value) => v.SetValue(MinimumWidthProperty, value);

    /// <summary>Gets <see cref="MinimumHeightProperty"/>.</summary>
    public static double GetMinimumHeight(Visual v) => v.GetValue(MinimumHeightProperty);

    /// <summary>Sets <see cref="MinimumHeightProperty"/>.</summary>
    public static void SetMinimumHeight(Visual v, double value) => v.SetValue(MinimumHeightProperty, value);
}
