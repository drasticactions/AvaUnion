using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaUnion.Engine;

[assembly: AvaloniaTestApplication(typeof(AvaUnion.Theme.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaUnion.Theme.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        Theme = new UnionTheme { Engine = UnionEngineKind.Managed };
        Styles.Add(Theme);
        Styles.Add(new DataGrid.UnionDataGridTheme());
        Styles.Add(new Controls.UnionControlsTheme());
    }

    public UnionTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
