using System;
using AvaUnion.Engine;

namespace AvaUnion.Elements;

/// <summary>
/// The resolved style of one node, as the value of
/// <see cref="Union.StyleProperty"/>. When only colors change, the node
/// updates this slot and raises <see cref="ColorsChanged"/>.
/// After all other changes, the node gets a new slot.
/// </summary>
public sealed class UnionStyleSlot
{
    internal UnionStyleSlot(ResolvedStyle current, string? key, bool rightToLeft = false)
    {
        Current = current;
        Key = key;
        IsRightToLeft = rightToLeft;
    }

    /// <summary>True when the node of this slot lays out right to left.</summary>
    public bool IsRightToLeft { get; }

    internal string? Key { get; private set; }

    /// <summary>The current style.</summary>
    public ResolvedStyle Current { get; private set; }

    /// <summary>Occurs on the UI thread when only the colors of <see cref="Current"/> change.</summary>
    public event EventHandler? ColorsChanged;

    internal void SetColors(ResolvedStyle style, string? key)
    {
        Current = style;
        Key = key;
        ColorsChanged?.Invoke(this, EventArgs.Empty);
    }
}
