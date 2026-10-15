namespace AvaUnion.Engine;

/// <summary>How text wraps.</summary>
public enum UnionTextWrapMode
{
    /// <summary>No wrapping.</summary>
    NoWrap,

    /// <summary>Wrap at word boundaries.</summary>
    WordWrap,

    /// <summary>Wrap only at explicit line breaks.</summary>
    ManualWrap,

    /// <summary>Wrap anywhere.</summary>
    WrapAnywhere,

    /// <summary>Wrap at word boundaries. If that is not possible, wrap anywhere.</summary>
    WrapAtWordBoundaryOrAnywhere,
}
