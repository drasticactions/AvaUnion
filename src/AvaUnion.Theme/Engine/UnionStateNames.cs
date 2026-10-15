using System;

namespace AvaUnion.Engine;

/// <summary>The CSS pseudo-class names of the Union states.</summary>
public static class UnionStateNames
{
    /// <summary>The CSS name of one state, for example <c>visual-focus</c>.</summary>
    public static string Css(UnionStates state) => state switch
    {
        UnionStates.Hovered => "hovered",
        UnionStates.ActiveFocus => "active-focus",
        UnionStates.VisualFocus => "visual-focus",
        UnionStates.Pressed => "pressed",
        UnionStates.Checked => "checked",
        UnionStates.Disabled => "disabled",
        UnionStates.Highlighted => "highlighted",
        _ => state.ToString().ToLowerInvariant(),
    };

    /// <summary>Parses the CSS name or the enum name of a state. It returns None when the name is not known.</summary>
    public static UnionStates Parse(string name)
    {
        var n = name.Replace("-", string.Empty, StringComparison.Ordinal);
        return n.ToLowerInvariant() switch
        {
            "hovered" => UnionStates.Hovered,
            "activefocus" => UnionStates.ActiveFocus,
            "visualfocus" => UnionStates.VisualFocus,
            "pressed" => UnionStates.Pressed,
            "checked" => UnionStates.Checked,
            "disabled" => UnionStates.Disabled,
            "highlighted" => UnionStates.Highlighted,
            _ => UnionStates.None,
        };
    }
}
