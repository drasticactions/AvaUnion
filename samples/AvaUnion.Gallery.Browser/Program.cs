using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using AvaUnion;
using AvaUnion.Gallery;

internal static class Program
{
    private const string Fonts = "fonts:Noto";

    private static Task Main(string[] args) =>
        BuildAvaloniaApp().WithAvaUnion(App.CreateOptions()).StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .ConfigureFonts(fontManager => fontManager.AddFontCollection(
            new EmbeddedFontCollection(new Uri(Fonts), new Uri("avares://AvaUnion.Gallery.Browser/Assets/Fonts"))))
        .With(new FontManagerOptions
        {
            DefaultFamilyName = Fonts + "#Noto Sans",
            FontFamilyMappings = new Dictionary<string, FontFamily>
            {
                ["Noto Sans"] = new(Fonts + "#Noto Sans"),
                ["monospace"] = new(Fonts + "#Noto Sans Mono"),
            },
            FontFallbacks = [new FontFallback { FontFamily = new FontFamily(Fonts + "#Noto Sans CJK JP") }],
        });
}
