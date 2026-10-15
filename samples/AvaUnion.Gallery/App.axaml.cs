using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public UnionTheme Theme => (UnionTheme)Styles[0];

    public static App Instance => (App)Current!;

    public static AvaUnionOptions CreateOptions()
    {
        var options = new AvaUnionOptions();
        foreach (var package in UnionStylePackage.Discover(typeof(App).Assembly))
        {
            options.StylePackages.Add(package);
        }

        return options;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "AvaUnion Gallery",
                    Width = 1100,
                    Height = 760,
                    Content = new MainView(),
                };
                break;
            case ISingleViewApplicationLifetime single:
                single.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
