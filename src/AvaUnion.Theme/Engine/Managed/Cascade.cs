using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace AvaUnion.Engine.Managed;

/// <summary>Matches and merges the rules of a style for one chain.</summary>
public static class Cascade
{
    /// <summary>
    /// Collects the matching rules, the highest weight first. If two rules
    /// have the same weight, the later rule comes first.
    /// </summary>
    public static List<ManagedRule> Match(ManagedStyle style, ReadOnlySpan<ManagedElement> chain)
    {
        var result = new List<ManagedRule>();
        for (var i = style.Rules.Count - 1; i >= 0; i--)
        {
            var rule = style.Rules[i];
            if (Selector.Matches(rule.Selectors, chain))
            {
                result.Add(rule);
            }
        }

        StableSortByWeightDescending(result);
        return result;
    }

    /// <summary>Merges the matched rules. Each leaf gets the first value that is set.</summary>
    public static object?[] Merge(List<ManagedRule> rules)
    {
        var leaves = new object?[UnionLeafInfo.Count];
        foreach (var rule in rules)
        {
            foreach (var (leaf, value) in rule.Leaves)
            {
                leaves[leaf] ??= value;
            }
        }

        return leaves;
    }

    /// <summary>Resolves a chain against a style.</summary>
    public static ResolvedStyle Resolve(ManagedStyle style, ReadOnlySpan<ManagedElement> chain, Func<UnionColor, Color> resolveColor)
    {
        var rules = Match(style, chain);
        if (rules.Count == 0)
        {
            return ResolvedStyle.Unmatched;
        }

        return ResolvedStyle.FromLeaves(Merge(rules), resolveColor) with { IsMatch = true };
    }

    private static void StableSortByWeightDescending(List<ManagedRule> list)
    {
        for (var i = 1; i < list.Count; i++)
        {
            var item = list[i];
            var j = i - 1;
            while (j >= 0 && list[j].Weight < item.Weight)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = item;
        }
    }
}
