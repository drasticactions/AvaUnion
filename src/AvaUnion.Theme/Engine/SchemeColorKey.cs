using System;

namespace AvaUnion.Engine;

/// <summary>The names and counts of the KColorScheme table.</summary>
public static class SchemeColorKey
{
    /// <summary>Lowercase group names, in <see cref="ColorGroup"/> order.</summary>
    public static readonly string[] Groups = ["active", "inactive", "disabled"];

    /// <summary>Lowercase set names, in <see cref="ColorSet"/> order.</summary>
    public static readonly string[] Sets = ["view", "window", "button", "selection", "tooltip", "complementary", "header"];

    /// <summary>Lowercase role kind names, in <see cref="ColorRoleKind"/> order.</summary>
    public static readonly string[] Kinds = ["background", "foreground", "decoration"];

    /// <summary>The background role names that the Union provider uses.</summary>
    public static readonly string[] BackgroundRoles = ["normal", "alternate", "active", "link", "visited", "negative", "neutral", "positive"];

    /// <summary>The foreground role names that the Union provider uses.</summary>
    public static readonly string[] ForegroundRoles = ["text", "inactive", "active", "link", "visited", "negative", "neutral", "positive"];

    /// <summary>The decoration role names that the Union provider uses.</summary>
    public static readonly string[] DecorationRoles = ["focus", "hover", "frame", "framecontrast"];

    /// <summary>The number of colors in one table.</summary>
    public const int Count = 3 * 7 * 20;

    /// <summary>Gets the role names for a kind.</summary>
    public static string[] Roles(ColorRoleKind kind) => kind switch
    {
        ColorRoleKind.Background => BackgroundRoles,
        ColorRoleKind.Foreground => ForegroundRoles,
        _ => DecorationRoles,
    };

    /// <summary>Gets the flat index of a color in the table.</summary>
    public static int Index(ColorGroup group, ColorSet set, ColorRoleKind kind, int role)
    {
        var roles = Roles(kind);
        if ((uint)role >= (uint)roles.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        var offset = kind switch
        {
            ColorRoleKind.Background => 0,
            ColorRoleKind.Foreground => 8,
            _ => 16,
        };
        return (((int)group * 7) + (int)set) * 20 + offset + role;
    }

    /// <summary>Gets the dotted lowercase key, for example <c>active.view.background.normal</c>.</summary>
    public static string Key(ColorGroup group, ColorSet set, ColorRoleKind kind, int role) =>
        $"{Groups[(int)group]}.{Sets[(int)set]}.{Kinds[(int)kind]}.{Roles(kind)[role]}";

    /// <summary>Gets the resource key, for example <c>Union.Active.View.Background.Normal</c>.</summary>
    public static string ResourceKey(ColorGroup group, ColorSet set, ColorRoleKind kind, int role) =>
        $"Union.{group}.{set}.{kind}.{RoleName(kind, role)}";

    /// <summary>Gets the enum name of a role.</summary>
    public static string RoleName(ColorRoleKind kind, int role) => kind switch
    {
        ColorRoleKind.Background => ((BackgroundRole)role).ToString(),
        ColorRoleKind.Foreground => ((ForegroundRole)role).ToString(),
        _ => ((DecorationRole)role).ToString(),
    };
}
