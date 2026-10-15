namespace AvaUnion.Engine;

/// <summary>A session font.</summary>
/// <param name="Family">The family name.</param>
/// <param name="SizeInDips">The size in device-independent pixels.</param>
/// <param name="Weight">The weight, 100 to 900.</param>
/// <param name="Italic">Whether the font is italic.</param>
public sealed record UnionFontInfo(string Family, double SizeInDips, int Weight, bool Italic)
{
    /// <summary>
    /// Converts a point size to device-independent pixels, rounded to whole
    /// pixels. For example, 10 pt is 13 px.
    /// </summary>
    public static double PointsToDips(double points) => System.Math.Round(points * 96.0 / 72.0, System.MidpointRounding.AwayFromZero);
}
