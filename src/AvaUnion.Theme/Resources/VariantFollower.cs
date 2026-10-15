using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Engine.Managed;

namespace AvaUnion.Resources;

internal sealed class VariantFollower
{
    private readonly Application _application;
    private readonly Func<bool> _enabled;

    public VariantFollower(Application application, Func<bool> enabled)
    {
        _application = application;
        _enabled = enabled;
        UnionEnvironment.Changed += OnChanged;
        application.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        Apply();
    }

    public static double RelativeLuminance(Color c)
    {
        static double Channel(byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    public static bool IsDark(IUnionEngine engine) =>
        RelativeLuminance(engine.SchemeColor(ColorGroup.Active, ColorSet.Window, ColorRoleKind.Background, (int)BackgroundRole.Normal)) < 0.5;

    public void Apply()
    {
        var engine = UnionEnvironment.Engine;
        if (engine is ManagedEngine managed)
        {
            managed.SetVariant(_application.ActualThemeVariant == ThemeVariant.Dark);
            return;
        }

        if (_enabled())
        {
            var wanted = IsDark(engine) ? ThemeVariant.Dark : ThemeVariant.Light;
            if (_application.RequestedThemeVariant != wanted)
            {
                _application.RequestedThemeVariant = wanted;
            }
        }
    }

    private void OnChanged(UnionChange change)
    {
        if ((change & UnionChange.Colors) != 0 && UnionEnvironment.Engine is not ManagedEngine)
        {
            Apply();
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (UnionEnvironment.Engine is ManagedEngine managed)
        {
            managed.SetVariant(_application.ActualThemeVariant == ThemeVariant.Dark);
        }
    }
}
