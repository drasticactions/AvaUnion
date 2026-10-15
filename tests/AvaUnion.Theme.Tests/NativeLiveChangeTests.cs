using AvaUnion.Engine;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class NativeLiveChangeTests
{
    [Fact]
    public void Scheme_Change_Reaches_The_Next_Query()
    {
        Assert.SkipWhen(NativeHost.Unavailable is not null, NativeHost.Unavailable ?? string.Empty);
        var (before, after, changes, dark) = NativeHost.Run(engine =>
        {
            var style = engine.GetStyle("breeze")!;
            var window = engine.CreateElement();
            window.SetType("applicationwindow");
            var button = engine.CreateElement();
            button.SetType("button");
            engine.SetColorScheme("/usr/share/color-schemes/BreezeLight.colors");
            engine.Pump();
            var before = engine.Query(style, [window, button]).Background.Color;

            var changes = UnionChange.None;
            void OnChanged(UnionChange c) => changes |= c;
            engine.Changed += OnChanged;
            engine.SetColorScheme("BreezeDark");
            for (var i = 0; i < 5 && changes == UnionChange.None; i++)
            {
                engine.Pump();
            }

            engine.Changed -= OnChanged;
            var after = engine.Query(style, [window, button]).Background.Color;
            var dark = Resources.VariantFollowerProbe.IsDark(engine);
            engine.SetColorScheme(null);
            engine.Pump();
            window.Dispose();
            button.Dispose();
            return (before, after, changes, dark);
        });

        Assert.NotEqual(before, after);
        Assert.True((changes & UnionChange.Colors) != 0);
        Assert.True(dark);
    }
}
