using System;

namespace AvaUnion.Engine;

/// <summary>How a style image is drawn.</summary>
[Flags]
public enum UnionImageFlags
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>Repeats the image horizontally.</summary>
    RepeatX = 1 << 0,

    /// <summary>Repeats the image vertically.</summary>
    RepeatY = 1 << 1,

    /// <summary>Repeats the image horizontally and vertically.</summary>
    RepeatBoth = RepeatX | RepeatY,

    /// <summary>Stretches the image horizontally.</summary>
    StretchX = 1 << 2,

    /// <summary>Stretches the image vertically.</summary>
    StretchY = 1 << 3,

    /// <summary>Stretches the image horizontally and vertically.</summary>
    StretchBoth = StretchX | StretchY,

    /// <summary>Uses the alpha of the image as a mask, filled with the mask color.</summary>
    Mask = 1 << 4,

    /// <summary>Uses the inverted alpha of the image as a mask.</summary>
    InvertedMask = 1 << 5,
}
