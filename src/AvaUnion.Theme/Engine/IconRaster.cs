namespace AvaUnion.Engine;

/// <summary>A rasterized icon or image, in premultiplied BGRA, row-major, with no padding.</summary>
/// <param name="PixelWidth">Width in device pixels.</param>
/// <param name="PixelHeight">Height in device pixels.</param>
/// <param name="Pixels">The pixels, 4 bytes each.</param>
public sealed record IconRaster(int PixelWidth, int PixelHeight, byte[] Pixels);
