namespace AvaUnion.Presenters.Layout;

/// <summary>A size or a point. Its values can be negative during layout.</summary>
/// <param name="Width">The width, or x.</param>
/// <param name="Height">The height, or y.</param>
public readonly record struct LayoutSize(double Width, double Height)
{
    /// <summary>True when one or both dimensions are zero or negative.</summary>
    public bool IsEmpty => Width <= 0.0 || Height <= 0.0;

    /// <summary>Adds the components.</summary>
    public static LayoutSize operator +(LayoutSize a, LayoutSize b) => new(a.Width + b.Width, a.Height + b.Height);

    /// <summary>Subtracts the components.</summary>
    public static LayoutSize operator -(LayoutSize a, LayoutSize b) => new(a.Width - b.Width, a.Height - b.Height);
}
