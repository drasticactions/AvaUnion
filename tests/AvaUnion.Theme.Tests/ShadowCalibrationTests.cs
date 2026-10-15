using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ShadowCalibrationTests
{
    [AvaloniaTheory]
    [InlineData("menu", 20, 2, 0x33)]
    [InlineData("dialog", 32, 1, 0x4c)]
    public void Blurred_Shadow_Matches_Union(string type, double blur, double offsetY, int alpha)
    {
        var box = new Border { Width = 160, Height = 60, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 0, 0) };
        Union.SetType(box, type);
        Union.SetStateSource(box, Elements.UnionStateSource.None);
        box.Child = new Presenters.UnionBox();
        var window = Shots.Show(new Panel { Children = { box } }, 300, 160);
        var node = Union.GetNode(box)!;
        Assert.Equal(blur, node.Resolved.Shadow.Blur);
        Assert.Equal(offsetY, node.Resolved.Shadow.Offset.Vertical);
        Assert.Equal((byte)alpha, node.Resolved.Shadow.Color!.Value.A);

        using var frame = Shots.Capture(window, $"shadow-{type}")!;
        var pixels = Read(frame);
        var x = (int)(box.Bounds.X + (box.Bounds.Width / 2));
        var bottom = box.Bounds.Bottom;
        var background = pixels[(frame.PixelSize.Height - 1) * frame.PixelSize.Width + x].R;
        var errors = new List<string>();
        for (var y = (int)Math.Ceiling(bottom); y < Math.Min(frame.PixelSize.Height - 1, bottom + offsetY + blur + 4); y++)
        {
            var d = y + 0.5 - (bottom + offsetY);
            var t = Math.Clamp((d + blur) / (2 * blur), 0, 1);
            var coverage = 1 - (t * t * (3 - (2 * t)));
            var expected = background * (1 - (coverage * alpha / 255.0));
            var actual = pixels[(y * frame.PixelSize.Width) + x].R;
            if (Math.Abs(actual - expected) > 2.5)
            {
                errors.Add($"y={y} d={d:0.0}: {actual} vs {expected:0.0}");
            }
        }

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private static unsafe Pixel[] Read(WriteableBitmap bitmap)
    {
        using var buffer = bitmap.Lock();
        var pixels = new Pixel[bitmap.PixelSize.Width * bitmap.PixelSize.Height];
        for (var row = 0; row < bitmap.PixelSize.Height; row++)
        {
            var src = new ReadOnlySpan<Pixel>((byte*)buffer.Address + (row * buffer.RowBytes), bitmap.PixelSize.Width);
            src.CopyTo(pixels.AsSpan(row * bitmap.PixelSize.Width));
        }

        return pixels;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Pixel(byte B, byte G, byte R, byte A);
}
