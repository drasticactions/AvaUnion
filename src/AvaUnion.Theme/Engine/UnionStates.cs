using System;

namespace AvaUnion.Engine;

/// <summary>The states of an element.</summary>
[Flags]
public enum UnionStates
{
    /// <summary>No state.</summary>
    None = 0,

    /// <summary>The pointer is over the element.</summary>
    Hovered = 1 << 0,

    /// <summary>The element has focus.</summary>
    ActiveFocus = 1 << 1,

    /// <summary>The element has keyboard focus that must be visible.</summary>
    VisualFocus = 1 << 2,

    /// <summary>The element is pressed.</summary>
    Pressed = 1 << 3,

    /// <summary>The element is checked.</summary>
    Checked = 1 << 4,

    /// <summary>The element is disabled.</summary>
    Disabled = 1 << 5,

    /// <summary>The element is selected.</summary>
    Highlighted = 1 << 6,
}
