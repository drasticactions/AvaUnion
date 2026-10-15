using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using AvaUnion.Engine;

namespace AvaUnion.Markup;

/// <summary>
/// <c>{union:SchemeColor View, Foreground, Link}</c> gives one KColorScheme
/// color. For a brush property, it gives a brush. The value changes when the
/// color scheme changes.
/// </summary>
public sealed class SchemeColorExtension : MarkupExtension
{
    /// <summary>Creates the extension.</summary>
    public SchemeColorExtension()
    {
    }

    /// <summary>Creates the extension.</summary>
    public SchemeColorExtension(ColorSet set, ColorRoleKind kind, string role)
    {
        Set = set;
        Kind = kind;
        Role = role;
    }

    /// <summary>The color set.</summary>
    public ColorSet Set { get; set; }

    /// <summary>The role family.</summary>
    public ColorRoleKind Kind { get; set; }

    /// <summary>The role name, for example <c>Normal</c>, <c>Link</c>, or <c>Focus</c>.</summary>
    public string Role { get; set; } = "Normal";

    /// <summary>The color group.</summary>
    public ColorGroup Group { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var roleIndex = RoleIndex(Kind, Role);
        var key = SchemeColorKey.ResourceKey(Group, Set, Kind, roleIndex);
        var targetType = TargetType(serviceProvider);
        if (targetType is null || !typeof(Color).IsAssignableFrom(Nullable.GetUnderlyingType(targetType) ?? targetType))
        {
            key += "Brush";
        }

        return new DynamicResourceExtension(key);
    }

    private static int RoleIndex(ColorRoleKind kind, string role)
    {
        var names = kind switch
        {
            ColorRoleKind.Background => Enum.GetNames<BackgroundRole>(),
            ColorRoleKind.Foreground => Enum.GetNames<ForegroundRole>(),
            _ => Enum.GetNames<DecorationRole>(),
        };
        var index = Array.FindIndex(names, n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new ArgumentException($"'{role}' is not a {kind} role");
        }

        return index;
    }

    private static Type? TargetType(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget target)
        {
            return null;
        }

        return target.TargetProperty switch
        {
            AvaloniaProperty p => p.PropertyType,
            _ when target.TargetObject is Setter { Property: { } sp } => sp.PropertyType,
            _ => null,
        };
    }
}
