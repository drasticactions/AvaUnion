using System;
using AvaUnion.Engine;

namespace AvaUnion.Presenters.Layout;

/// <summary>The item, content or background container. It has four buckets side by side.</summary>
public sealed class LayoutContainer
{
    /// <summary>The position.</summary>
    public LayoutSize Position { get; set; }

    /// <summary>The size.</summary>
    public LayoutSize Size { get; set; }

    /// <summary>The implicit size.</summary>
    public LayoutSize ImplicitSize { get; set; }

    /// <summary>The spacing.</summary>
    public double Spacing { get; set; }

    /// <summary>Start-aligned items.</summary>
    public LayoutBucket Start { get; } = new();

    /// <summary>Centered items.</summary>
    public LayoutBucket Center { get; } = new();

    /// <summary>End-aligned items.</summary>
    public LayoutBucket End { get; } = new();

    /// <summary>Filling items.</summary>
    public LayoutBucket Fill { get; } = new();

    /// <summary>The buckets, in the order start, center, end and fill.</summary>
    public LayoutBucket[] Buckets => [Start, Center, End, Fill];

    /// <summary>True when all buckets are empty.</summary>
    public bool IsEmpty => Start.IsEmpty && Center.IsEmpty && End.IsEmpty && Fill.IsEmpty;

    /// <summary>Sets the spacing on the container and on all buckets.</summary>
    public void SetSpacing(double spacing)
    {
        Spacing = spacing;
        foreach (var b in Buckets)
        {
            b.Spacing = spacing;
        }
    }

    /// <summary>Sorts all buckets.</summary>
    public void Sort()
    {
        foreach (var b in Buckets)
        {
            b.Sort();
        }
    }

    /// <summary>Lays out the buckets and their items.</summary>
    public void Layout()
    {
        foreach (var b in Buckets)
        {
            b.Layout();
        }

        var height = Math.Max(Math.Max(Math.Max(Size.Height, Start.ImplicitSize.Height), Math.Max(Center.ImplicitSize.Height, End.ImplicitSize.Height)), Fill.ImplicitSize.Height);
        foreach (var b in Buckets)
        {
            b.Size = new LayoutSize(b.ImplicitSize.Width, height);
        }

        var maxImplicitCenter = new LayoutSize(Math.Max(Center.ImplicitSize.Width, Fill.ImplicitSize.Width), Math.Max(Center.ImplicitSize.Height, Fill.ImplicitSize.Height));

        ImplicitSize = new LayoutSize(
            LayoutHelpers.SpacedSize(Spacing, Start.ImplicitSize.Width, maxImplicitCenter.Width, End.ImplicitSize.Width),
            Math.Max(Math.Max(Start.ImplicitSize.Height, maxImplicitCenter.Height), End.ImplicitSize.Height));

        if (Size.IsEmpty)
        {
            Size = ImplicitSize;
        }

        Start.Position = Position;
        End.Position = new LayoutSize(Position.Width + Size.Width - End.ImplicitSize.Width, Position.Height);

        var startSpace = Start.Size.Width > 0 ? Start.Size.Width + Spacing : 0.0;
        var endSpace = End.Size.Width > 0 ? End.Size.Width + Spacing : 0.0;

        var centerPosition = Position + new LayoutSize(startSpace, 0.0);
        var remainingSize = Size - new LayoutSize(startSpace + endSpace, 0.0);

        Fill.Position = centerPosition;
        Fill.Size = remainingSize;

        Center.Size = new LayoutSize(
            Math.Max(Math.Min(remainingSize.Width, Center.Size.Width), Center.MinimumSize.Width),
            Math.Max(Math.Min(remainingSize.Height, Center.Size.Height), Center.MinimumSize.Height));
        Center.Position = centerPosition + new LayoutSize((remainingSize.Width - Center.Size.Width) / 2.0, (remainingSize.Height - Center.Size.Height) / 2.0);

        foreach (var bucket in Buckets)
        {
            var stackedY = bucket.StackCenter ? Math.Max(0.0, (bucket.Size.Height - bucket.ImplicitSize.Height) / 2.0) : 0.0;
            var stackFillHeight = bucket.Size.Height / bucket.Items.Count;

            var fillX = bucket.Position.Width;
            var fillWidth = bucket.Size.Width / bucket.Items.Count;

            foreach (var item in bucket.Items)
            {
                var m = item.Margins;
                item.Position = bucket.Position + item.Position;

                var constrainedWidth = Math.Min(item.ImplicitSize.Width, bucket.Size.Width);
                var constrainedHeight = Math.Min(item.ImplicitSize.Height, bucket.Size.Height);
                item.Size = new LayoutSize(Math.Max(constrainedWidth, item.MinimumSize.Width), Math.Max(constrainedHeight, item.MinimumSize.Height));

                if (ReferenceEquals(bucket, Fill))
                {
                    fillX += m.Left;
                    item.Position = item.Position with { Width = fillX };
                    item.Size = item.Size with { Width = fillWidth - m.Left - m.Right };
                    fillX += fillWidth;
                }

                var top = bucket.Position.Height + m.Top;

                switch (item.VerticalAlignment)
                {
                    case UnionAlignment.Unspecified:
                    case UnionAlignment.Start:
                        item.Position = item.Position with { Height = top };
                        break;
                    case UnionAlignment.Center:
                        item.Position = item.Position with { Height = top + (bucket.Size.Height - m.Top - m.Bottom - item.Size.Height) / 2 };
                        break;
                    case UnionAlignment.End:
                        item.Position = item.Position with { Height = bucket.Position.Height + (bucket.Size.Height - item.Size.Height - m.Bottom) };
                        break;
                    case UnionAlignment.Fill:
                        item.Position = item.Position with { Height = top };
                        item.Size = item.Size with { Height = bucket.Size.Height - m.Top - m.Bottom };
                        break;
                    case UnionAlignment.StackCenter:
                        stackedY += m.Top;
                        item.Size = item.Size with { Width = Math.Min(bucket.Size.Width, item.Size.Width) };
                        item.Position = new LayoutSize(bucket.Position.Width + (bucket.Size.Width - item.Size.Width) / 2, bucket.Position.Height + stackedY);
                        stackedY += item.Size.Height + m.Bottom;
                        break;
                    case UnionAlignment.StackFill:
                        stackedY += m.Top;
                        item.Position = item.Position with { Height = bucket.Position.Height + stackedY };
                        item.Size = new LayoutSize(bucket.Size.Width, stackFillHeight - m.Top - m.Bottom);
                        stackedY += stackFillHeight + m.Bottom;
                        break;
                }
            }
        }
    }
}
