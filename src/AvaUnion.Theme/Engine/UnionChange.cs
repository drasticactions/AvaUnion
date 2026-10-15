using System;

namespace AvaUnion.Engine;

/// <summary>The session settings that changed, from <see cref="IUnionEngine.Changed"/>.</summary>
[Flags]
public enum UnionChange
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>The color scheme.</summary>
    Colors = 1,

    /// <summary>The default Union style.</summary>
    Style = 2,

    /// <summary>The general, fixed or small fonts.</summary>
    Fonts = 4,

    /// <summary>The icon theme.</summary>
    Icons = 8,

    /// <summary>The animation speed or smooth scrolling.</summary>
    Animation = 16,
}
