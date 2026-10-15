namespace AvaUnion.Engine;

/// <summary>The value kind of a style leaf.</summary>
public enum UnionLeafKind
{
    /// <summary>A real number.</summary>
    Real,

    /// <summary>An integer.</summary>
    Int,

    /// <summary>A boolean.</summary>
    Bool,

    /// <summary>An enum.</summary>
    Enum,

    /// <summary>A set of flags.</summary>
    Flags,

    /// <summary>A color.</summary>
    Color,

    /// <summary>A string.</summary>
    String,

    /// <summary>A URL.</summary>
    Url,

    /// <summary>A file path.</summary>
    Path,

    /// <summary>A font.</summary>
    Font,
}
