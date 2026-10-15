using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Markup;

namespace AvaUnion.Resources;

/// <summary>
/// Gives the session values as resources. These are the KColorScheme colors and
/// brushes, the session fonts, the animation settings and some Fluent keys. A
/// change raises <c>ResourcesChanged</c>, and <c>DynamicResource</c> updates.
/// </summary>
public sealed class UnionResourceProvider : ResourceProvider
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);
    private bool _subscribed;

    /// <summary>The key for the general font family.</summary>
    public const string FontFamilyKey = "UnionFontFamily";

    /// <summary>The key for the general font size in DIPs.</summary>
    public const string FontSizeKey = "UnionFontSize";

    /// <summary>The key for the fixed font family.</summary>
    public const string FixedFontFamilyKey = "UnionFixedFontFamily";

    /// <summary>The key for the fixed font size in DIPs.</summary>
    public const string FixedFontSizeKey = "UnionFixedFontSize";

    /// <summary>The key for the small font size in DIPs.</summary>
    public const string SmallFontSizeKey = "UnionSmallFontSize";

    /// <summary>The key for the menu font size in DIPs.</summary>
    public const string MenuFontSizeKey = "UnionMenuFontSize";

    /// <summary>The key for the animation duration factor.</summary>
    public const string AnimationSpeedKey = "UnionAnimationSpeed";

    /// <summary>The key for the smooth scrolling setting.</summary>
    public const string SmoothScrollKey = "UnionSmoothScroll";

    /// <summary>The Fluent keys that this provider gives.</summary>
    public static readonly string[] FluentAliases =
    [
        "SystemAccentColor", "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
        "TextFillColorPrimary", "TextFillColorPrimaryBrush", "SystemControlForegroundBaseHighBrush",
        "SystemControlBackgroundAltHighBrush", "SystemControlHighlightAccentBrush",
        "ContentControlThemeFontFamily", "ControlContentThemeFontSize",
    ];

    /// <inheritdoc/>
    public override bool HasResources => true;

    /// <inheritdoc/>
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is not string name)
        {
            value = null;
            return false;
        }

        EnsureSubscribed();
        if (_values.TryGetValue(name, out value))
        {
            return value is not null;
        }

        value = Served(Compute(name));
        _values[name] = value;
        return value is not null;
    }

    private static object? Served(object? value) =>
        value is ISolidColorBrush { Transform: null } solid && value is not SolidColorBrush
            ? new SolidColorBrush(solid.Color) { Opacity = solid.Opacity }
            : value;

    private void EnsureSubscribed()
    {
        if (!_subscribed)
        {
            _subscribed = true;
            UnionEnvironment.Changed += OnChanged;
        }
    }

    private void OnChanged(UnionChange change)
    {
        var changed = false;
        foreach (var name in new List<string>(_values.Keys))
        {
            var old = _values[name];
            var now = Compute(name);
            if (old is SolidColorBrush brush && now is ISolidColorBrush solid)
            {
                brush.Color = solid.Color;
                brush.Opacity = solid.Opacity;
            }
            else if (!Equals(old, now))
            {
                _values[name] = Served(now);
                changed = true;
            }
        }

        if (changed)
        {
            RaiseResourcesChanged();
        }
    }

    private static object? Compute(string name)
    {
        var engine = UnionEnvironment.Engine;
        if (name.StartsWith("Union.", StringComparison.Ordinal))
        {
            return SchemeValue(engine, name);
        }

        var fonts = engine.Fonts;
        switch (name)
        {
            case FontFamilyKey:
            case "ContentControlThemeFontFamily":
                return Family(fonts.General.Family);
            case FontSizeKey:
            case "ControlContentThemeFontSize":
                return fonts.General.SizeInDips;
            case FixedFontFamilyKey:
                return Family(fonts.Fixed.Family);
            case FixedFontSizeKey:
                return fonts.Fixed.SizeInDips;
            case SmallFontSizeKey:
                return fonts.Small.SizeInDips;
            case MenuFontSizeKey:
                return fonts.Menu.SizeInDips;
            case AnimationSpeedKey:
                return engine.Platform.AnimationSpeed;
            case SmoothScrollKey:
                return engine.Platform.SmoothScroll;
        }

        var accent = engine.SchemeColor(ColorGroup.Active, ColorSet.Selection, ColorRoleKind.Background, (int)BackgroundRole.Normal);
        var windowText = engine.SchemeColor(ColorGroup.Active, ColorSet.Window, ColorRoleKind.Foreground, (int)ForegroundRole.Text);
        var windowBackground = engine.SchemeColor(ColorGroup.Active, ColorSet.Window, ColorRoleKind.Background, (int)BackgroundRole.Normal);
        return name switch
        {
            "SystemAccentColor" => accent,
            "SystemAccentColorLight1" => Mix(accent, Colors.White, 0.25),
            "SystemAccentColorLight2" => Mix(accent, Colors.White, 0.5),
            "SystemAccentColorLight3" => Mix(accent, Colors.White, 0.75),
            "SystemAccentColorDark1" => Mix(accent, Colors.Black, 0.25),
            "SystemAccentColorDark2" => Mix(accent, Colors.Black, 0.5),
            "SystemAccentColorDark3" => Mix(accent, Colors.Black, 0.75),
            "TextFillColorPrimary" => windowText,
            "TextFillColorPrimaryBrush" or "SystemControlForegroundBaseHighBrush" => BrushCache.Get(windowText),
            "SystemControlBackgroundAltHighBrush" => BrushCache.Get(windowBackground),
            "SystemControlHighlightAccentBrush" => BrushCache.Get(accent),
            _ => null,
        };
    }

    private static object? SchemeValue(IUnionEngine engine, string name)
    {
        var brush = name.EndsWith("Brush", StringComparison.Ordinal);
        var parts = (brush ? name[..^5] : name).Split('.');
        if (parts.Length != 5
            || !Enum.TryParse<ColorGroup>(parts[1], out var group)
            || !Enum.TryParse<ColorSet>(parts[2], out var set)
            || !Enum.TryParse<ColorRoleKind>(parts[3], out var kind))
        {
            return null;
        }

        int role = kind switch
        {
            ColorRoleKind.Background => Enum.TryParse<BackgroundRole>(parts[4], out var b) ? (int)b : -1,
            ColorRoleKind.Foreground => Enum.TryParse<ForegroundRole>(parts[4], out var f) ? (int)f : -1,
            _ => Enum.TryParse<DecorationRole>(parts[4], out var d) ? (int)d : -1,
        };
        if (role < 0)
        {
            return null;
        }

        var color = engine.SchemeColor(group, set, kind, role);
        return brush ? BrushCache.Get(color) : color;
    }

    private static FontFamily Family(string family) => family == "$Default" ? FontFamily.Default : new FontFamily(family);

    private static Color Mix(Color a, Color b, double t) => Color.FromArgb(
        a.A,
        (byte)Math.Round(a.R * (1 - t) + b.R * t),
        (byte)Math.Round(a.G * (1 - t) + b.G * t),
        (byte)Math.Round(a.B * (1 - t) + b.B * t));
}
