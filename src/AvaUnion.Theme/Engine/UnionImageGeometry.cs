using System.Collections.Generic;
using Avalonia;

namespace AvaUnion.Engine;

/// <summary>A style image as vector primitives in a view box.</summary>
/// <param name="ViewBox">The SVG view box.</param>
/// <param name="Shapes">The primitives, in document order.</param>
public sealed record UnionImageGeometry(Rect ViewBox, IReadOnlyList<UnionImageShape> Shapes);
