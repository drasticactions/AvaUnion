namespace AvaUnion.Engine;

/// <summary>The session's fonts.</summary>
/// <param name="General">The general font.</param>
/// <param name="Fixed">The fixed-width font.</param>
/// <param name="Small">The smallest readable font.</param>
/// <param name="Menu">The menu font.</param>
/// <param name="Toolbar">The toolbar font.</param>
/// <param name="Title">The window title font.</param>
public sealed record UnionFonts(UnionFontInfo General, UnionFontInfo Fixed, UnionFontInfo Small, UnionFontInfo Menu, UnionFontInfo Toolbar, UnionFontInfo Title);
