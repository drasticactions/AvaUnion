using Avalonia;
using Avalonia.Media;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class NativeIconTests
{
    [Theory]
    [InlineData(0xff232629u)]
    [InlineData(0xfffcfcfcu)]
    public void Icons_Render_With_Any_Tint(uint argb)
    {
        Assert.SkipWhen(NativeHost.Unavailable is not null, NativeHost.Unavailable ?? string.Empty);
        var raster = NativeHost.Run(e => e.RenderIcon("arrow-down", Color.FromUInt32(argb), new PixelSize(16, 16), 1.0));
        Assert.NotNull(raster);
        var expected = Color.FromUInt32(argb);
        var best = 3;
        for (var i = 3; i < raster!.Pixels.Length; i += 4)
        {
            if (raster.Pixels[i] > raster.Pixels[best])
            {
                best = i;
            }
        }

        var a = raster.Pixels[best];
        Assert.True(a > 100, $"most opaque alpha is {a}");
        var red = raster.Pixels[best - 1] * 255 / a;
        Assert.True(System.Math.Abs(red - expected.R) <= 8, $"red {red} != {expected.R}");
    }
}
