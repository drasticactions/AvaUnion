namespace AvaUnion.Engine.Managed;

/// <summary>The kinds of selector.</summary>
public enum SelectorKind
{
    /// <summary>Never matches.</summary>
    Empty,

    /// <summary>Matches the element type.</summary>
    Type,

    /// <summary>Matches the element id.</summary>
    Id,

    /// <summary>Matches any of the state bits.</summary>
    State,

    /// <summary>Matches a hint.</summary>
    Hint,

    /// <summary>Matches when an attribute exists.</summary>
    AttributeExists,

    /// <summary>Matches an attribute value.</summary>
    AttributeEquals,

    /// <summary>Matches part of an attribute value.</summary>
    AttributeSubstringMatch,

    /// <summary>Matches any element.</summary>
    AnyElement,

    /// <summary>Matches the element one level up.</summary>
    ChildCombinator,

    /// <summary>Matches an element any number of levels up.</summary>
    DescendantCombinator,
}
