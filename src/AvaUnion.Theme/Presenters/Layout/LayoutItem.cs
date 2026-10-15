using AvaUnion.Engine;

namespace AvaUnion.Presenters.Layout;

/// <summary>One item placed by the positioner.</summary>
public sealed class LayoutItem
{
    /// <summary>The position, relative to the layout.</summary>
    public LayoutSize Position { get; set; }

    /// <summary>The size.</summary>
    public LayoutSize Size { get; set; }

    /// <summary>The preferred size of the item.</summary>
    public LayoutSize ImplicitSize { get; set; }

    /// <summary>The minimum size of the item.</summary>
    public LayoutSize MinimumSize { get; set; }

    /// <summary>The vertical alignment.</summary>
    public UnionAlignment VerticalAlignment { get; set; } = UnionAlignment.Unspecified;

    /// <summary>The order of the item in its bucket.</summary>
    public int Order { get; set; }

    /// <summary>The margins.</summary>
    public LayoutMargins Margins { get; set; }

    /// <summary>The object that the item represents.</summary>
    public object? Tag { get; set; }

    /// <summary>The final position in the parent, rounded. The size is rounded up.</summary>
    public (double X, double Y, double Width, double Height) Placement()
    {
        return (System.Math.Round(Position.Width, System.MidpointRounding.AwayFromZero), System.Math.Round(Position.Height, System.MidpointRounding.AwayFromZero),
            System.Math.Ceiling(Size.Width), System.Math.Ceiling(Size.Height));
    }
}
