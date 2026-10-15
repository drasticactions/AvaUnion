namespace AvaUnion.Engine;

/// <summary>One primitive of a style image that is kept as geometry.</summary>
/// <param name="Kind">0 = path, 1 = circle, 2 = rectangle.</param>
/// <param name="Fill">True when the shape has a fill.</param>
/// <param name="Stroke">True when the shape has a stroke.</param>
/// <param name="StrokeWidth">The stroke width in view box units.</param>
/// <param name="LineCap">0 = butt, 1 = round, 2 = square.</param>
/// <param name="LineJoin">0 = miter, 1 = round, 2 = bevel.</param>
/// <param name="PathData">The SVG path data of a path.</param>
/// <param name="X">The x of the circle center, or the x of the rectangle.</param>
/// <param name="Y">The y of the circle center, or the y of the rectangle.</param>
/// <param name="Width">The radius of the circle, or the width of the rectangle.</param>
/// <param name="Height">The height of the rectangle.</param>
public sealed record UnionImageShape(
    int Kind,
    bool Fill,
    bool Stroke,
    double StrokeWidth,
    int LineCap,
    int LineJoin,
    string? PathData,
    double X,
    double Y,
    double Width,
    double Height);
