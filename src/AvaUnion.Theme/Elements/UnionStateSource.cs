namespace AvaUnion.Elements;

/// <summary>The source of the Union states of an element.</summary>
public enum UnionStateSource
{
    /// <summary>
    /// The element itself for templated controls.
    /// The templated parent for other template parts, for example a panel, a <c>UnionBox</c> or a <c>UnionIcon</c>.
    /// </summary>
    Auto,

    /// <summary>The pseudo-classes of the element.</summary>
    Self,

    /// <summary>The pseudo-classes of the templated parent.</summary>
    TemplatedParent,

    /// <summary>No states.</summary>
    None,
}
