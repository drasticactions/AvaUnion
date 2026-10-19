using System;
using Avalonia;
using AvaUnion;
using AvaUnion.BreezeIcons;
using AvaUnion.Gallery;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--shoot")
        {
            return AvaUnion.Gallery.Desktop.HeadlessShooter.Run(args);
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseWaylandWithFallback()
        .WithAvaUnion(App.CreateOptions())
        .WithBreezeIcons()
        .LogToTrace();
}
