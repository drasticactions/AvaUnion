using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaUnion.Elements;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ControlShadowTests
{
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void Shadow_Shows_A_Dip_Below_The_Box(double scale)
    {
        foreach (var control in new Control[] { new Button { Content = "Button" }, new ComboBox { ItemsSource = new[] { "Kate" }, SelectedIndex = 0 } })
        {
            control.Margin = new Thickness(10);
            control.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            control.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            var window = Shots.Show(new Panel { Children = { control } }, 200, 100);
            window.SetRenderScaling(scale);
            for (var i = 0; i < 4; i++)
            {
                Dispatcher.UIThread.RunJobs();
                UnionEnvironment.Flush();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            using var frame = window.CaptureRenderedFrame()!;
            var bottom = control.Bounds.Bottom;
            var x = (int)((control.Bounds.X + 8) * scale);
            var behind = Pixel(frame, x, (int)Math.Ceiling((bottom + 3) * scale));

            for (var y = (int)Math.Ceiling(bottom * scale); y < (int)Math.Floor((bottom + 1) * scale); y++)
            {
                Assert.True(Pixel(frame, x, y) < behind - 10, $"{control.GetType().Name} at {scale}: no shadow at y={y}");
            }

            window.Close();
        }
    }

    private static int Pixel(Bitmap bitmap, int x, int y)
    {
        var data = new byte[4];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(x, y, 1, 1), handle.AddrOfPinnedObject(), 4, 4);
        }
        finally
        {
            handle.Free();
        }

        return data[0] + data[1] + data[2];
    }
}
