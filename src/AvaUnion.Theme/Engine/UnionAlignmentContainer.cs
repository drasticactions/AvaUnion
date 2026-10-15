namespace AvaUnion.Engine;

/// <summary>The area that an alignment is relative to.</summary>
public enum UnionAlignmentContainer
{
    /// <summary>Relative to the whole item.</summary>
    Item,

    /// <summary>Relative to the content area, inside the padding.</summary>
    Content,

    /// <summary>Relative to the background, inside the inset.</summary>
    Background,
}
