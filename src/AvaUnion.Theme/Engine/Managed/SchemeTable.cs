using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>
/// A resolved KColorScheme. It has all groups, sets and roles, with the
/// inactive and disabled effects of KColorScheme.
/// </summary>
public sealed class SchemeTable
{
    private readonly Color[] _colors;

    /// <summary>Creates a table from colors in <see cref="SchemeColorKey.Index"/> order.</summary>
    public SchemeTable(string name, Color[] colors)
    {
        if (colors.Length != SchemeColorKey.Count)
        {
            throw new ArgumentException($"expected {SchemeColorKey.Count} colors, got {colors.Length}", nameof(colors));
        }

        Name = name;
        _colors = colors;
    }

    /// <summary>The scheme name, for example <c>BreezeLight</c>.</summary>
    public string Name { get; }

    /// <summary>One color.</summary>
    public Color Get(ColorGroup group, ColorSet set, ColorRoleKind kind, int role) => _colors[SchemeColorKey.Index(group, set, kind, role)];

    /// <summary>
    /// Resolves the four arguments of <c>Custom(kcolorscheme, …)</c>.
    /// </summary>
    public UnionRgba? Resolve(IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 4)
        {
            return null;
        }

        var group = IndexOf(SchemeColorKey.Groups, arguments[0]);
        var set = IndexOf(SchemeColorKey.Sets, arguments[1]);
        if (group < 0 || set < 0)
        {
            return null;
        }

        var kindName = arguments[2];
        int role;
        ColorRoleKind kind;
        if (kindName.Equals("background", StringComparison.OrdinalIgnoreCase))
        {
            kind = ColorRoleKind.Background;
            role = IndexOf(SchemeColorKey.BackgroundRoles, arguments[3]);
        }
        else if (kindName.Equals("foreground", StringComparison.OrdinalIgnoreCase))
        {
            kind = ColorRoleKind.Foreground;
            role = IndexOf(SchemeColorKey.ForegroundRoles, arguments[3]);
        }
        else if (kindName.Equals("decoration", StringComparison.OrdinalIgnoreCase))
        {
            kind = ColorRoleKind.Decoration;

            role = arguments[3] switch
            {
                "frame" => (int)DecorationRole.Frame,
                "framecontrast" => (int)DecorationRole.FrameContrast,
                _ => arguments[3].Equals("focus", StringComparison.OrdinalIgnoreCase) ? (int)DecorationRole.Focus
                    : arguments[3].Equals("hover", StringComparison.OrdinalIgnoreCase) ? (int)DecorationRole.Hover
                    : -1,
            };
        }
        else
        {
            return null;
        }

        if (role < 0)
        {
            return null;
        }

        return UnionRgba.FromColor(Get((ColorGroup)group, (ColorSet)set, kind, role));
    }

    private static int IndexOf(string[] names, string value)
    {
        for (var i = 0; i < names.Length; i++)
        {
            if (names[i].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
