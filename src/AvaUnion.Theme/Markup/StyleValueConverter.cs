using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.Engine;

namespace AvaUnion.Markup;

/// <summary>
/// Reads one path of a <see cref="ResolvedStyle"/> and converts the value to
/// the target type. An unset leaf becomes <see cref="AvaloniaProperty.UnsetValue"/>.
/// </summary>
public sealed class StyleValueConverter : IValueConverter
{
    private readonly Func<ResolvedStyle, object?> _getter;

    /// <summary>Creates a converter for a path such as <c>Text.Color</c>.</summary>
    /// <exception cref="ArgumentException">The path does not exist.</exception>
    public StyleValueConverter(string path, string? sides = null, string? fallback = null, string? fallbackColor = null)
    {
        Path = path;
        Sides = sides;
        Fallback = fallback;
        _fallbackColor = fallbackColor is null ? null : ParseSchemeColor(fallbackColor);
        _getter = StylePaths.Get(path) ?? throw new ArgumentException($"'{path}' is not a ResolvedStyle path", nameof(path));
    }

    /// <summary>The path.</summary>
    public string Path { get; }

    /// <summary>
    /// The sides that get a length when it converts to a thickness, for
    /// example <c>Left</c> or <c>Left,Right</c>. Null means all sides. A
    /// leading <c>-</c> negates the length.
    /// </summary>
    public string? Sides { get; }

    /// <summary>The value, as written in XAML, that applies while the leaf is not set.</summary>
    public string? Fallback { get; }

    private readonly (ColorGroup Group, ColorSet Set, ColorRoleKind Kind, int Role)? _fallbackColor;

    private static (ColorGroup, ColorSet, ColorRoleKind, int) ParseSchemeColor(string text)
    {
        var parts = text.Split('.');
        var group = ColorGroup.Active;
        var i = 0;
        if (parts.Length == 4)
        {
            group = Enum.Parse<ColorGroup>(parts[i++]);
        }

        if (parts.Length - i != 3)
        {
            throw new ArgumentException($"'{text}' is not [Group.]Set.Kind.Role", nameof(text));
        }

        var set = Enum.Parse<ColorSet>(parts[i++]);
        var kind = Enum.Parse<ColorRoleKind>(parts[i++]);
        var names = kind switch
        {
            ColorRoleKind.Background => Enum.GetNames<BackgroundRole>(),
            ColorRoleKind.Foreground => Enum.GetNames<ForegroundRole>(),
            _ => Enum.GetNames<DecorationRole>(),
        };
        var role = Array.IndexOf(names, parts[i]);
        if (role < 0)
        {
            throw new ArgumentException($"'{parts[i]}' is not a {kind} role", nameof(text));
        }

        return (group, set, kind, role);
    }

    private (Type Type, object? Value)? _fallback;

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = value switch
        {
            StyleValue v => ConvertForBinding(v, targetType),
            ResolvedStyle style => ConvertStyle(style, targetType),
            _ => AvaloniaProperty.UnsetValue,
        };
        if (result == AvaloniaProperty.UnsetValue && _fallbackColor is { } scheme && Elements.UnionEnvironment.IsReady)
        {
            var color = Elements.UnionEnvironment.Engine.SchemeColor(scheme.Group, scheme.Set, scheme.Kind, scheme.Role);
            return ConvertLeaf(color, targetType);
        }

        if (Fallback is null || result != AvaloniaProperty.UnsetValue)
        {
            return result;
        }

        if (_fallback is not { } cached || cached.Type != targetType)
        {
            cached = (targetType, ParseFallback(Fallback, targetType));
            _fallback = cached;
        }

        return cached.Value;
    }

    /// <summary>Parses a XAML fallback string for a target type. It returns UnsetValue when it cannot parse the string.</summary>
    public static object? ParseFallback(string text, Type targetType)
    {
        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var inv = CultureInfo.InvariantCulture;
        if (target == typeof(string) || target == typeof(object))
        {
            return text;
        }

        if (target == typeof(double) && double.TryParse(text, NumberStyles.Float, inv, out var d))
        {
            return d;
        }

        if (target == typeof(int) && int.TryParse(text, NumberStyles.Integer, inv, out var i))
        {
            return i;
        }

        if (target == typeof(bool) && bool.TryParse(text, out var b))
        {
            return b;
        }

        if (target == typeof(Thickness))
        {
            return Thickness.Parse(text);
        }

        if (target == typeof(CornerRadius))
        {
            return CornerRadius.Parse(text);
        }

        if (target.IsEnum && Enum.TryParse(target, text, ignoreCase: true, out var e))
        {
            return e;
        }

        return AvaloniaProperty.UnsetValue;
    }

    private object? ConvertForBinding(StyleValue value, Type targetType)
    {
        if (_getter(value.Style) is Color color
            && (Nullable.GetUnderlyingType(targetType) ?? targetType).IsAssignableFrom(typeof(SolidColorBrush)))
        {
            var brush = value.Accessor.BrushFor(value.Key, color);
            value.Accessor.Brush = brush;
            return brush;
        }

        var result = ConvertStyle(value.Style, targetType);
        return value.RightToLeft && Sides is null ? Mirror(result) : result;
    }

    private static object? Mirror(object? value) => value switch
    {
        Thickness t => new Thickness(t.Right, t.Top, t.Left, t.Bottom),
        CornerRadius r => new CornerRadius(r.TopRight, r.TopLeft, r.BottomLeft, r.BottomRight),
        _ => value,
    };

    private object? ConvertStyle(ResolvedStyle style, Type targetType)
    {
        var leaf = _getter(style);
        if (Sides is not null && leaf is double d && (Nullable.GetUnderlyingType(targetType) ?? targetType) == typeof(Thickness))
        {
            if (Sides.StartsWith('-'))
            {
                d = -d;
            }

            return new Thickness(
                Sides.Contains("Left", StringComparison.Ordinal) ? d : 0,
                Sides.Contains("Top", StringComparison.Ordinal) ? d : 0,
                Sides.Contains("Right", StringComparison.Ordinal) ? d : 0,
                Sides.Contains("Bottom", StringComparison.Ordinal) ? d : 0);
        }

        return ConvertLeaf(leaf, targetType);
    }

    /// <summary>Converts one style value to a target type.</summary>
    public static object? ConvertLeaf(object? leaf, Type targetType)
    {
        if (leaf is null)
        {
            return AvaloniaProperty.UnsetValue;
        }

        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
        switch (leaf)
        {
            case Color color:
                if (target.IsAssignableFrom(typeof(Avalonia.Media.Immutable.ImmutableSolidColorBrush)))
                {
                    return BrushCache.Get(color);
                }

                return target == typeof(Color) || target == typeof(object) ? color : AvaloniaProperty.UnsetValue;
            case ResolvedStyle.SizeGroup size:
                return target == typeof(Thickness) ? size.ToThickness() : size.HasAnyValue ? size : AvaloniaProperty.UnsetValue;
            case ResolvedStyle.CornersGroup corners:
                return target == typeof(CornerRadius) ? corners.Radii() : corners;
            case ResolvedStyle.BorderGroup border:
                return target == typeof(Thickness) ? border.DrawnWidths() : border;
            case UnionTextWrapMode wrap:
                return target == typeof(TextWrapping) ? wrap switch
                {
                    UnionTextWrapMode.NoWrap or UnionTextWrapMode.ManualWrap => TextWrapping.NoWrap,
                    UnionTextWrapMode.WrapAtWordBoundaryOrAnywhere => TextWrapping.WrapWithOverflow,
                    _ => TextWrapping.Wrap,
                } : wrap;
            case UnionAlignment alignment when target == typeof(VerticalAlignment):
                return alignment switch
                {
                    UnionAlignment.Start => VerticalAlignment.Top,
                    UnionAlignment.End => VerticalAlignment.Bottom,
                    _ => VerticalAlignment.Center,
                };
            case UnionAlignment alignment when target == typeof(TextAlignment):
                return alignment switch
                {
                    UnionAlignment.Center => TextAlignment.Center,
                    UnionAlignment.End => TextAlignment.End,
                    UnionAlignment.Fill or UnionAlignment.StackCenter or UnionAlignment.StackFill => TextAlignment.Justify,
                    _ => TextAlignment.Start,
                };
            case UnionTextElide elide:
                return target == typeof(TextTrimming) ? elide switch
                {
                    UnionTextElide.None => TextTrimming.None,
                    UnionTextElide.Left => TextTrimming.PrefixCharacterEllipsis,
                    _ => TextTrimming.CharacterEllipsis,
                } : elide;
            case UnionFont font:
                if (target == typeof(double))
                {
                    return font.SizeInDips is { } size ? size : AvaloniaProperty.UnsetValue;
                }

                if (target == typeof(FontWeight))
                {
                    return font.Weight is { } w ? (FontWeight)w : AvaloniaProperty.UnsetValue;
                }

                if (target == typeof(FontStyle))
                {
                    return font.Italic is { } i ? (i ? FontStyle.Italic : FontStyle.Normal) : AvaloniaProperty.UnsetValue;
                }

                return font;
            case double d:
                if (target == typeof(Thickness))
                {
                    return new Thickness(d);
                }

                if (target == typeof(CornerRadius))
                {
                    return new CornerRadius(d);
                }

                if (target == typeof(GridLength))
                {
                    return new GridLength(d);
                }

                return target == typeof(double) || target == typeof(object) ? d : target == typeof(float) ? (float)d : AvaloniaProperty.UnsetValue;
            case int i when target == typeof(FontWeight):
                return (FontWeight)i;
            case int i when target == typeof(double):
                return (double)i;
            default:
                return target.IsInstanceOfType(leaf) ? leaf : AvaloniaProperty.UnsetValue;
        }
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
