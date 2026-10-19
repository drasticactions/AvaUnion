using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaUnion.Elements;
using AvaUnion.Engine.Managed;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class IconSchemeTests
{
    [AvaloniaFact]
    public void Typed_Icons_Follow_A_Scheme_Change()
    {
        var icon = new UnionIcon();
        Union.SetType(icon, "searchindicator");
        var field = new TextBox { InnerLeftContent = icon, Width = 200 };
        Union.SetType(field, "searchfield");
        var window = Shots.Show(field);
        var engine = (ManagedEngine)UnionEnvironment.Engine;
        var light = icon.EffectiveColor;
        try
        {
            engine.SetColorScheme("BreezeDark");
            UnionEnvironment.Flush();
            Shots.Settle();
            Assert.NotNull(light);
            Assert.NotEqual(light, icon.EffectiveColor);
        }
        finally
        {
            engine.SetColorScheme(null);
            UnionEnvironment.Flush();
            window.Close();
        }
    }
}
