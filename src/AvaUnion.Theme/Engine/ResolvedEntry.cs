using System;
using System.Collections.Generic;

namespace AvaUnion.Engine;

internal sealed class ResolvedEntry
{
    private readonly object?[] _metrics;
    private readonly int _metricsHash;

    public ResolvedEntry(ResolvedStyle style)
    {
        Style = style;
        var kinds = UnionLeafInfo.Kinds;
        var leaves = new List<object?>(kinds.Length);
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

        _metrics = [.. leaves];
        _metricsHash = hash.ToHashCode();
    }

    public ResolvedStyle Style { get; }

    public bool SameMetrics(ResolvedEntry other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_metricsHash != other._metricsHash || Style.IsMatch != other.Style.IsMatch)
        {
            return false;
        }

        for (var i = 0; i < _metrics.Length; i++)
        {
            if (!Equals(_metrics[i], other._metrics[i]))
            {
                return false;
            }
        }

        return true;
    }
}
