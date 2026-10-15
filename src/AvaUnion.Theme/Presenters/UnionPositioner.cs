using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.Engine;
using AvaUnion.Presenters.Layout;

namespace AvaUnion.Presenters;

/// <summary>
/// Puts the icon, text and sub-elements of a control in position from the
/// alignment properties of Union.
/// </summary>
public class UnionPositioner : Panel
{
    private static readonly AttachedProperty<Rect> PlacementProperty =
        AvaloniaProperty.RegisterAttached<UnionPositioner, Visual, Rect>("Placement");

    /// <summary>
    /// True when the positioner is drawn turned a quarter, as the header of a
    /// tab on the left or right is. The width and height of its style then
    /// apply to its height and width.
    /// </summary>
    public static readonly StyledProperty<bool> TurnedProperty =
        AvaloniaProperty.Register<UnionPositioner, bool>(nameof(Turned));

    static UnionPositioner()
    {
        AffectsMeasure<UnionPositioner>(TurnedProperty);
        AffectsParentMeasure<UnionPositioner>(
            PositionedItem.SourceProperty,
            PositionedItem.HorizontalAlignmentProperty,
            PositionedItem.VerticalAlignmentProperty,
            PositionedItem.MinimumWidthProperty,
            PositionedItem.MinimumHeightProperty);
        Union.StyleProperty.Changed.AddClassHandler<Visual>(OnChildStyleChanged);
    }

    /// <summary>True when the positioner is drawn turned a quarter.</summary>
    public bool Turned
    {
        get => GetValue(TurnedProperty);
        set => SetValue(TurnedProperty, value);
    }

    /// <summary>The position of a child, in positioner coordinates.</summary>
    public static Rect GetPlacement(Visual child) => child.GetValue(PlacementProperty);

    private ResolvedStyle OwnerStyle => Union.GetStyle(this) ?? ResolvedStyle.Unmatched;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
        }

        var layout = Build(new LayoutSize(0, 0), out _);
        layout.Layout();
        var style = OwnerStyle;
        var (width, height) = (style.Layout.Width ?? 0.0, style.Layout.Height ?? 0.0);
        if (Turned)
        {
            (width, height) = (height, width);
        }

        return new Size(
            Math.Max(layout.ImplicitSize.Width, width),
            Math.Max(layout.ImplicitSize.Height, height));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = Build(new LayoutSize(finalSize.Width, finalSize.Height), out var placed);
        layout.Layout();
        foreach (var (visual, item) in placed)
        {
            var (x, y, w, h) = item.Placement();
            var rect = new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
            visual.SetValue(PlacementProperty, rect);
            if (visual is Layoutable layoutable && visual.GetVisualParentIs(this))
            {
                layoutable.Arrange(rect);
            }
        }

        foreach (var child in Children)
        {
            if (child is PositionedGroup group)
            {
                group.InvalidateArrange();
                group.Arrange(new Rect(finalSize));
            }
            else if (!placed.Exists(p => ReferenceEquals(p.Visual, child)))
            {
                child.Arrange(default);
            }
        }

        return finalSize;
    }

    private PositionerLayout Build(LayoutSize size, out List<(Visual Visual, LayoutItem Item)> placed)
    {
        var owner = OwnerStyle;
        var layout = new PositionerLayout
        {
            Size = size,
            Spacing = owner.Layout.Spacing ?? 0.0,
            Inset = LayoutMargins.From(owner.Layout.Inset.ToThickness()),
            Padding = TemplatedParent is TemplatedControl control
                ? LayoutMargins.From(control.Padding)
                : LayoutMargins.From(owner.Layout.Padding.ToThickness()),
        };

        placed = [];
        foreach (var child in Children)
        {
            if (child is PositionedGroup group)
            {
                if (group.IsVisible)
                {
                    foreach (var inner in group.Children)
                    {
                        AddItem(layout, owner, inner, placed);
                    }
                }

                continue;
            }

            AddItem(layout, owner, child, placed);
        }

        return layout;
    }

    private static void AddItem(PositionerLayout layout, ResolvedStyle owner, Control child, List<(Visual, LayoutItem)> placed)
    {
        if (!child.IsVisible)
        {
            return;
        }

        var source = PositionedItem.GetSource(child);
        var own = Union.GetStyle(child) ?? ResolvedStyle.Unmatched;
        var alignment = source switch
        {
            PositionerSource.Icon => owner.Icon.Alignment,
            PositionerSource.Text => owner.Text.Alignment,
            _ => own.Layout.Alignment,
        };
        if (!alignment.HasAnyValue)
        {
            return;
        }

        var horizontal = alignment.Horizontal ?? UnionAlignment.Unspecified;
        var vertical = alignment.Vertical ?? UnionAlignment.Unspecified;
        if (PositionedItem.GetHorizontalAlignment(child) is var h and not UnionAlignment.Unspecified)
        {
            horizontal = h;
        }

        if (PositionedItem.GetVerticalAlignment(child) is var v and not UnionAlignment.Unspecified)
        {
            vertical = v;
        }

        var item = new LayoutItem
        {
            ImplicitSize = new LayoutSize(child.DesiredSize.Width, child.DesiredSize.Height),
            VerticalAlignment = vertical,
            Order = alignment.Order ?? 0,
            MinimumSize = new LayoutSize(Math.Max(0, PositionedItem.GetMinimumWidth(child)), Math.Max(0, PositionedItem.GetMinimumHeight(child))),
            Margins = source == PositionerSource.Layout ? LayoutMargins.From(own.Layout.Margins.ToThickness()) : default,
            Tag = child,
        };

        var container = (alignment.Container ?? UnionAlignmentContainer.Item) switch
        {
            UnionAlignmentContainer.Content => layout.ContentContainer,
            UnionAlignmentContainer.Background => layout.BackgroundContainer,
            _ => layout.ItemContainer,
        };

        var stackCenter = vertical == UnionAlignment.StackCenter;
        var stackFill = vertical == UnionAlignment.StackFill;
        var bucket = horizontal switch
        {
            UnionAlignment.Center => container.Center,
            UnionAlignment.End => container.End,
            UnionAlignment.Fill => container.Fill,
            _ => container.Start,
        };
        bucket.Items.Add(item);
        bucket.StackCenter |= stackCenter;
        bucket.StackFill |= stackFill;
        placed.Add((child, item));
    }

    private static void OnChildStyleChanged(Visual visual, AvaloniaPropertyChangedEventArgs e)
    {
        if (ResolvedStyle.SameMetrics(e.GetOldValue<Elements.UnionStyleSlot?>()?.Current, e.GetNewValue<Elements.UnionStyleSlot?>()?.Current))
        {
            return;
        }

        if (visual is UnionPositioner self)
        {
            self.InvalidateMeasure();
        }

        if (visual.GetVisualParentAsLayoutable() is UnionPositioner positioner)
        {
            positioner.InvalidateMeasure();
        }
    }
}
