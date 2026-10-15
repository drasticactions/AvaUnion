using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaUnion.Elements;

namespace AvaUnion.Theme.Tests;

internal static class Shots
{
    public static string? Directory => Environment.GetEnvironmentVariable("AVAUNION_SHOTS");

    public static Window Show(Control content, double width = 400, double height = 300)
    {
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        Settle();
        return window;
    }

    public static void Settle()
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            UnionEnvironment.Flush();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    public static WriteableBitmap? Capture(Window window, string? name = null)
    {
        Settle();
        var frame = window.CaptureRenderedFrame();
        if (frame is not null && name is not null && Directory is { } dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            frame.Save(Path.Combine(dir, name + ".png"), new PngBitmapEncoderOptions());
        }

        return frame;
    }
}
