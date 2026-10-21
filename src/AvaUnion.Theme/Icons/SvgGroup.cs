using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

internal sealed class SvgGroup : SvgNode
{
    public Matrix Transform { get; init; } = Matrix.Identity;

    public double Opacity { get; init; } = 1;

    public Geometry? Clip { get; init; }

    public SvgNode? Mask { get; init; }

    public Rect MaskBounds { get; init; }

    public IEffect? Effect { get; init; }

    public Geometry? EffectClip { get; init; }

    public List<SvgNode> Children { get; } = [];

    public override Drawing? Build(Color? tint)
    {
        var group = new DrawingGroup();
        foreach (var child in Children)
        {
            if (child.Build(tint) is { } drawing)
            {
                group.Children.Add(drawing);
            }
        }

        if (group.Children.Count == 0)
        {
            return null;
        }

        if (Effect is not null)
        {
            group.Effect = Effect;
            if (EffectClip is not null)
            {
                group = new DrawingGroup { Children = { group }, ClipGeometry = EffectClip };
            }
        }

        if (Clip is null && Mask is null && Opacity >= 1 && Transform.IsIdentity && Effect is null && group.Children.Count == 1)
        {
            return group.Children[0];
        }

        if (Clip is not null || Mask is not null || Effect is not null)
        {
            group = new DrawingGroup { Children = { group } };
        }

        group.ClipGeometry = Clip;
        group.Opacity = Opacity;
        if (!Transform.IsIdentity)
        {
            group.Transform = new MatrixTransform(Transform);
        }

        if (Mask?.Build(tint) is { } mask)
        {
            group.OpacityMask = new DrawingBrush(new DrawingGroup
            {
                Children = { new GeometryDrawing { Geometry = new RectangleGeometry(MaskBounds), Brush = Brushes.Transparent }, mask },
            })
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                SourceRect = new RelativeRect(MaskBounds, RelativeUnit.Absolute),
                DestinationRect = new RelativeRect(MaskBounds, RelativeUnit.Absolute),
            };
        }
        else if (Mask is not null)
        {
            return null;
        }

        return group;
    }
}
