using System;

namespace AvaUnion.Presenters.Layout;

/// <summary>
/// The layout of the positioner.
/// The item container uses the full size.
/// The background container is smaller by the inset, and the content container is smaller by the inset and the padding.
/// </summary>
public sealed class PositionerLayout
{
    /// <summary>Items relative to the whole control.</summary>
    public LayoutContainer ItemContainer { get; } = new();

    /// <summary>Items relative to the content area.</summary>
    public LayoutContainer ContentContainer { get; } = new();

    /// <summary>Items relative to the background.</summary>
    public LayoutContainer BackgroundContainer { get; } = new();

    /// <summary>The size for the layout. If it is empty, the layout calculates the implicit size.</summary>
    public LayoutSize Size { get; set; }

    /// <summary>The inset.</summary>
    public LayoutMargins Inset { get; set; }

    /// <summary>The padding.</summary>
    public LayoutMargins Padding { get; set; }

    /// <summary>The spacing.</summary>
    public double Spacing { get; set; }

    /// <summary>The implicit size after <see cref="Layout"/>.</summary>
    public LayoutSize ImplicitSize { get; private set; }

    /// <summary>The padding of the content container after the layout.</summary>
    public LayoutMargins ItemPadding { get; private set; }

    /// <summary>The containers in layout order.</summary>
    public LayoutContainer[] Containers => [ItemContainer, ContentContainer, BackgroundContainer];

    /// <summary>Runs the layout.</summary>
    public void Layout()
    {
        foreach (var container in Containers)
        {
            container.SetSpacing(Spacing);
            container.Sort();
        }

        ItemContainer.Size = Size;
        ItemContainer.Layout();

        var remainingPosition = new LayoutSize(ItemContainer.Start.Size.Width, 0.0);
        var remainingSize = ItemContainer.Size - new LayoutSize(ItemContainer.Start.Size.Width + ItemContainer.End.Size.Width, 0.0);

        BackgroundContainer.Position = remainingPosition + new LayoutSize(Inset.Left, Inset.Top);
        BackgroundContainer.Size = remainingSize - new LayoutSize(Inset.Left + Inset.Right, Inset.Top + Inset.Bottom);
        BackgroundContainer.Layout();

        ContentContainer.Position = remainingPosition + new LayoutSize(Padding.Left, Padding.Top);
        ContentContainer.Size = remainingSize - new LayoutSize(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
        ContentContainer.Layout();

        var implicitCenterWidth = Math.Max(
            BackgroundContainer.ImplicitSize.Width + Inset.Left + Inset.Right,
            ContentContainer.ImplicitSize.Width + Padding.Left + Padding.Right);

        var implicitWidth = LayoutHelpers.SpacedSize(Spacing, ItemContainer.Start.ImplicitSize.Width, implicitCenterWidth, ItemContainer.End.ImplicitSize.Width);
        var implicitHeight = Math.Max(
            Math.Max(ItemContainer.ImplicitSize.Height, BackgroundContainer.ImplicitSize.Height + Inset.Top + Inset.Bottom),
            ContentContainer.ImplicitSize.Height + Padding.Top + Padding.Bottom);

        ImplicitSize = new LayoutSize(Math.Ceiling(implicitWidth), Math.Ceiling(implicitHeight));

        var bottomRight = ItemContainer.Size - ContentContainer.Size;
        ItemPadding = new LayoutMargins(
            ContentContainer.Position.Width,
            bottomRight.Width - ContentContainer.Position.Width,
            ContentContainer.Position.Height,
            bottomRight.Height - ContentContainer.Position.Height);
    }
}
