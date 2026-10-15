namespace AvaUnion.Engine;

/// <summary>Where text is elided.</summary>
public enum UnionTextElide
{
    /// <summary>The text is not elided.</summary>
    None,

    /// <summary>Elides the text at the start.</summary>
    Left,

    /// <summary>Elides the text in the middle.</summary>
    Middle,

    /// <summary>Elides the text at the end.</summary>
    Right,
}
