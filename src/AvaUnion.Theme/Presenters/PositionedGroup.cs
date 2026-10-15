using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Presenters;

/// <summary>
/// A panel whose children the parent <see cref="UnionPositioner"/> puts in
/// position as its own children.
/// </summary>
public class PositionedGroup : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            size = new Size(System.Math.Max(size.Width, child.DesiredSize.Width), System.Math.Max(size.Height, child.DesiredSize.Height));
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            child.Arrange(UnionPositioner.GetPlacement(child));
        }

        return finalSize;
    }
}
