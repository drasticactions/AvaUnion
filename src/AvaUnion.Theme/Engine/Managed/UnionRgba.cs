using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>An 8-bit RGBA color.</summary>
/// <param name="R">Red.</param>
/// <param name="G">Green.</param>
/// <param name="B">Blue.</param>
/// <param name="A">Alpha.</param>
public readonly record struct UnionRgba(byte R, byte G, byte B, byte A)
{
    /// <summary>Transparent, with all channels 0.</summary>
    public static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);

    /// <summary>Converts to an Avalonia color.</summary>
    public Color ToColor() => Color.FromArgb(A, R, G, B);

    /// <summary>Converts from an Avalonia color.</summary>
    public static UnionRgba FromColor(Color c) => new(c.R, c.G, c.B, c.A);
}
