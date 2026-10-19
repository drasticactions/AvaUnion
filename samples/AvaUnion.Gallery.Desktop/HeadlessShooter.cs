using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaUnion.BreezeIcons;
using AvaUnion.Elements;

namespace AvaUnion.Gallery.Desktop;

internal static class HeadlessShooter
{
    public static int Run(string[] args)
    {
        var scale = 1.0;
        var scaleAt = Array.IndexOf(args, "--scale");
        if (scaleAt > 0 && scaleAt + 1 < args.Length)
        {
            scale = double.Parse(args[scaleAt + 1], System.Globalization.CultureInfo.InvariantCulture);
            args = [.. args[..scaleAt], .. args[(scaleAt + 2)..]];
        }

        string? style = null;
        var styleAt = Array.IndexOf(args, "--style");
        if (styleAt > 0 && styleAt + 1 < args.Length)
        {
            style = args[styleAt + 1];
            args = [.. args[..styleAt], .. args[(styleAt + 2)..]];
        }

        var rtl = Array.IndexOf(args, "--rtl") > 0;
        args = Array.FindAll(args, a => a != "--rtl");

        var outDir = args.Length > 1 ? args[1] : "shots";
        var size = args.Length > 2 ? args[2].Split('x') : ["900", "700"];
        var scheme = args.Length > 3 ? args[3] : null;
        Directory.CreateDirectory(outDir);
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithAvaUnion(App.CreateOptions())
            .WithBreezeIcons()
            .SetupWithoutStarting();
        if (style is not null)
        {
            App.Instance.Theme.StyleId = style;
        }

        foreach (var (section, page) in GalleryPages.All)
        {
            var window = new Window
            {
                Width = double.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture),
                Height = double.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture),
                Content = new Border { Padding = new Thickness(16), Child = page.Create() },
                FlowDirection = rtl ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight,
            };
            window.Show();
            if (scale != 1.0)
            {
                window.SetRenderScaling(scale);
            }

            Settle();
            if (scheme is not null)
            {
                App.Instance.Theme.ColorScheme = scheme;
                for (var i = 0; i < 20 && UnionEnvironment.Engine is AvaUnion.Engine.Native.NativeEngine native; i++)
                {
                    native.Pump();
                    Settle();
                }
            }

            for (var i = 0; i < 4; i++)
            {
                Dispatcher.UIThread.RunJobs();
                UnionEnvironment.Flush();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            using var frame = window.CaptureRenderedFrame();
            var name = section.Key + "-" + page.Title.Replace(" & ", "-").Replace(' ', '-').ToLowerInvariant();
            frame?.Save(Path.Combine(outDir, name + ".png"), new PngBitmapEncoderOptions());
            window.Close();
            Console.WriteLine($"{name}.png");
        }

        return 0;
    }

    private static void Settle()
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            UnionEnvironment.Flush();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
