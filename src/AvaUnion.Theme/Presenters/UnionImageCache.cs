using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaUnion.Engine;

namespace AvaUnion.Presenters;

/// <summary>
/// A cache of rasters and geometries for style images and icons. The cache
/// clears when the icon theme or the colors change.
/// </summary>
public static class UnionImageCache
{
    private static readonly Dictionary<string, Bitmap?> s_bitmaps = [];
    private static readonly Dictionary<string, Geometry?> s_geometries = [];

    /// <summary>Removes all entries.</summary>
    public static void Clear()
    {
        s_bitmaps.Clear();
        s_geometries.Clear();
    }

    /// <summary>
    /// Gets a raster as a bitmap. A null raster gives null. The bitmap is
    /// 96 DPI.
    /// </summary>
    public static Bitmap? ToBitmap(IconRaster? raster, double scale)
    {
        if (raster is null)
        {
            return null;
        }

        var bitmap = new WriteableBitmap(new PixelSize(raster.PixelWidth, raster.PixelHeight), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        using (var fb = bitmap.Lock())
        {
            for (var y = 0; y < raster.PixelHeight; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(raster.Pixels, y * raster.PixelWidth * 4, fb.Address + y * fb.RowBytes, raster.PixelWidth * 4);
            }
        }

        return bitmap;
    }

    /// <summary>Gets the cached bitmap for a key. If there is none, <paramref name="create"/> makes it.</summary>
    public static Bitmap? Bitmap(string key, System.Func<Bitmap?> create)
    {
        if (!s_bitmaps.TryGetValue(key, out var bitmap))
        {
            bitmap = create();
            s_bitmaps[key] = bitmap;
        }

        return bitmap;
    }

    /// <summary>Gets the primitives of a style image as one geometry in view box units, or null.</summary>
    public static Geometry? Geometry(UnionImageGeometry image, string key, double strokeScale)
    {
        if (s_geometries.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var group = new GeometryGroup { FillRule = FillRule.NonZero };
        foreach (var shape in image.Shapes)
        {
            Geometry? g = shape.Kind switch
            {
                0 when shape.PathData is { } d => StreamGeometry.Parse(d),
                1 => new EllipseGeometry(new Rect(shape.X - shape.Width, shape.Y - shape.Width, shape.Width * 2, shape.Width * 2)),
                2 => new RectangleGeometry(new Rect(shape.X, shape.Y, shape.Width, shape.Height)),
                _ => null,
            };
            if (g is null)
            {
                continue;
            }

            if (shape.Stroke)
            {
                var pen = new Pen(Brushes.Black, shape.StrokeWidth * strokeScale,
                    lineCap: shape.LineCap switch { 1 => PenLineCap.Round, 2 => PenLineCap.Square, _ => PenLineCap.Flat },
                    lineJoin: shape.LineJoin switch { 1 => PenLineJoin.Round, 2 => PenLineJoin.Bevel, _ => PenLineJoin.Miter });
                var widened = g.GetWidenedGeometry(pen);
                group.Children.Add(widened);
            }

            if (shape.Fill)
            {
                group.Children.Add(g);
            }
        }

        s_geometries[key] = group;
        return group;
    }
}
