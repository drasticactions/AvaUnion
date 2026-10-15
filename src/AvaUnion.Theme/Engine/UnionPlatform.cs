namespace AvaUnion.Engine;

/// <summary>Session values from the platform plugin of Union.</summary>
/// <param name="SmoothScroll">True when smooth scrolling is on.</param>
/// <param name="AnimationSpeed">The animation duration factor. 0 turns animations off.</param>
/// <param name="IsRightToLeft">True when the layout direction of the session is right to left.</param>
/// <param name="IconTheme">The icon theme name, or null.</param>
public sealed record UnionPlatform(bool SmoothScroll, double AnimationSpeed, bool IsRightToLeft, string? IconTheme);
