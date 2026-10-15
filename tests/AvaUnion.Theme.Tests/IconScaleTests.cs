using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using AvaUnion.Engine;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class IconScaleTests
{
    [AvaloniaFact]
    public void A_Scaled_Raster_Is_Drawn_Whole()
    {
        var pixels = new byte[32 * 32 * 4];
        foreach (var (x, y) in new[] { (2, 2), (29, 2), (2, 29), (29, 29) })
        {
            pixels[((y * 32) + x) * 4 + 3] = 255;
        }

        var bitmap = UnionImageCache.ToBitmap(new IconRaster(32, 32, pixels), 2.0)!;
        using var target = new RenderTargetBitmap(new PixelSize(32, 32), new Vector(192, 192));
        using (var context = target.CreateDrawingContext())
        {
            context.DrawImage(bitmap, new Rect(0, 0, 16, 16));
        }

        var data = new byte[32 * 32 * 4];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(0, 0, 32, 32), handle.AddrOfPinnedObject(), data.Length, 32 * 4);
        }
        finally
        {
            handle.Free();
        }

        foreach (var (x, y) in new[] { (2, 2), (29, 2), (2, 29), (29, 29) })
        {
            Assert.True(data[(((y * 32) + x) * 4) + 3] > 128, $"corner pixel ({x}, {y}) is missing");
        }
    }
}
