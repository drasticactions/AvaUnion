namespace AvaUnion.Engine;

/// <summary>The parts of a <c>QFont</c> that a style sets. Parts that are not set are null.</summary>
/// <param name="Family">The family.</param>
/// <param name="PointSize">The size in points.</param>
/// <param name="PixelSize">The size in pixels.</param>
/// <param name="Weight">The weight, 100 to 900.</param>
/// <param name="Italic">True when the font is italic.</param>
public sealed record UnionFont(string? Family, double? PointSize, double? PixelSize, int? Weight, bool? Italic)
{
    /// <summary>The size in device-independent pixels. It is null when no size is set.</summary>
    public double? SizeInDips => PixelSize ?? (PointSize is { } pt ? UnionFontInfo.PointsToDips(pt) : null);
}
