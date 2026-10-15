using System;
using System.Collections.Generic;
using Avalonia;

namespace AvaUnion.Engine;

/// <summary>
/// The result of one Union query, with resolved colors. Each leaf is null
/// when it is not set.
/// </summary>
public sealed partial record ResolvedStyle
{
    /// <summary>The result of a query that matched no rule.</summary>
    public static readonly ResolvedStyle Unmatched = new();

    /// <summary>True when one or more rules matched.</summary>
    public bool IsMatch { get; init; }

    private static readonly Dictionary<ResolvedStyle, Metrics> s_metrics = new(ReferenceEqualityComparer.Instance);

    private static Metrics MetricsOf(ResolvedStyle style)
    {
        if (!s_metrics.TryGetValue(style, out var metrics))
        {
            if (s_metrics.Count > 4096)
            {
                s_metrics.Clear();
            }

            metrics = new Metrics(style);
            s_metrics[style] = metrics;
        }

        return metrics;
    }

    private sealed class Metrics
    {
        public readonly object?[] Leaves;
        public readonly int Hash;

        public Metrics(ResolvedStyle style)
        {
            var kinds = UnionLeafInfo.Kinds;
            var leaves = new System.Collections.Generic.List<object?>(kinds.Length);
            var hash = new HashCode();
            for (var i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] != UnionLeafKind.Color)
                {
                    var leaf = style.GetLeaf(i);
                    leaves.Add(leaf);
                    hash.Add(leaf);
                }
            }

            Leaves = [.. leaves];
            Hash = hash.ToHashCode();
        }
    }

    /// <summary>
    /// True when the two results are different only in colors.
    /// </summary>
    public static bool SameMetrics(ResolvedStyle? a, ResolvedStyle? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.IsMatch != b.IsMatch)
        {
            return false;
        }

        var ma = MetricsOf(a);
        var mb = MetricsOf(b);
        if (ma.Hash != mb.Hash)
        {
            return false;
        }

        for (var i = 0; i < ma.Leaves.Length; i++)
        {
            if (!Equals(ma.Leaves[i], mb.Leaves[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when a background, border, outline or shadow is set.</summary>
    public bool HasDrawable => Background.HasAnyValue || Border.HasAnyValue || Outline.HasAnyValue || Shadow.HasAnyValue;

    public sealed partial record SizeGroup
    {
        /// <summary>The sizes as a <see cref="Thickness"/>. Sides that are not set are 0.</summary>
        public Thickness ToThickness() => new(Left ?? 0.0, Top ?? 0.0, Right ?? 0.0, Bottom ?? 0.0);
    }

    public sealed partial record BorderGroup
    {
        /// <summary>The sizes of all borders. Sides that are not set are 0.</summary>
        public Thickness Sizes() => new(Left.Size ?? 0.0, Top.Size ?? 0.0, Right.Size ?? 0.0, Bottom.Size ?? 0.0);

        /// <summary>The widths that draw. A side counts only when its style is not None.</summary>
        public Thickness DrawnWidths() => new(LineGroup.Drawn(Left), LineGroup.Drawn(Top), LineGroup.Drawn(Right), LineGroup.Drawn(Bottom));
    }

    public sealed partial record OutlineGroup
    {
        /// <summary>The widths that draw. A side counts only when its style is not None.</summary>
        public Thickness DrawnWidths() => new(LineGroup.Drawn(Left), LineGroup.Drawn(Top), LineGroup.Drawn(Right), LineGroup.Drawn(Bottom));
    }

    public sealed partial record LineGroup
    {
        internal static double Drawn(LineGroup line) =>
            (line.Style ?? UnionLineStyle.None) != UnionLineStyle.None ? line.Size ?? 0.0 : 0.0;
    }

    public sealed partial record CornersGroup
    {
        /// <summary>The corner radii. Corners that are not set are 0.</summary>
        public CornerRadius Radii() => new(TopLeft.Radius ?? 0.0, TopRight.Radius ?? 0.0, BottomRight.Radius ?? 0.0, BottomLeft.Radius ?? 0.0);
    }

    public sealed partial record OffsetGroup
    {
        /// <summary>The offset as a vector. Parts that are not set are 0.</summary>
        public Vector ToVector() => new(Horizontal ?? 0.0, Vertical ?? 0.0);
    }
}
