namespace AvaUnion.Engine.Managed;

/// <summary>One style rule. It has a selector list and the leaves that it sets.</summary>
/// <param name="Selectors">The selector list, left to right.</param>
/// <param name="Weight">The sum of the selector weights.</param>
/// <param name="Leaves">The values by leaf index, in <see cref="UnionLeafInfo"/> order.</param>
public sealed record ManagedRule(Selector[] Selectors, int Weight, (int Leaf, object Value)[] Leaves);
