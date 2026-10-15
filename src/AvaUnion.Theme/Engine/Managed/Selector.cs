using System;
using System.Globalization;

namespace AvaUnion.Engine.Managed;

/// <summary>One part of a selector list.</summary>
public sealed class Selector
{
    /// <summary>Creates a selector.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="value">The type, id, hint, or attribute name.</param>
    /// <param name="state">For <see cref="SelectorKind.State"/>, the state bits.</param>
    /// <param name="attributeValue">For attribute matches, the value. It is a string or a double.</param>
    public Selector(SelectorKind kind, string? value = null, UnionStates state = UnionStates.None, object? attributeValue = null)
    {
        Kind = kind;
        Value = value ?? string.Empty;
        State = state;
        AttributeValue = attributeValue;
    }

    /// <summary>The kind.</summary>
    public SelectorKind Kind { get; }

    /// <summary>The string data.</summary>
    public string Value { get; }

    /// <summary>The state bits.</summary>
    public UnionStates State { get; }

    /// <summary>The attribute value.</summary>
    public object? AttributeValue { get; }

    /// <summary>True when this is a combinator.</summary>
    public bool IsCombinator => Kind is SelectorKind.ChildCombinator or SelectorKind.DescendantCombinator;

    /// <summary>The weight. A type is 1, an id is 100, and a state, hint or attribute is 10. Other kinds are 0.</summary>
    public int Weight => Kind switch
    {
        SelectorKind.Type => 1,
        SelectorKind.Id => 100,
        SelectorKind.State or SelectorKind.Hint or SelectorKind.AttributeExists or SelectorKind.AttributeEquals or SelectorKind.AttributeSubstringMatch => 10,
        _ => 0,
    };

    /// <summary>Returns true when this selector matches the element.</summary>
    public bool Matches(ManagedElement element)
    {
        switch (Kind)
        {
            case SelectorKind.Type:
                return Value.Length > 0 && string.Equals(element.Type, Value, StringComparison.OrdinalIgnoreCase);
            case SelectorKind.Id:
                return Value.Length > 0 && string.Equals(element.Id, Value, StringComparison.OrdinalIgnoreCase);
            case SelectorKind.State:
                return State != UnionStates.None && (element.States & State) != 0;
            case SelectorKind.Hint:
                return Value.Length > 0 && element.HasHint(Value);
            case SelectorKind.AttributeExists:
                return Value.Length > 0 && element.HasAttribute(Value);
            case SelectorKind.AttributeEquals:
            {
                if (Value.Length == 0 || AttributeValue is null)
                {
                    return false;
                }

                var actual = element.Attribute(Value);
                if (actual is null)
                {
                    return false;
                }

                if (actual is string text)
                {
                    return string.Equals(text, VariantString(AttributeValue), StringComparison.OrdinalIgnoreCase);
                }

                return actual is double number && AttributeValue is double expected && number == expected;
            }

            case SelectorKind.AttributeSubstringMatch:
            {
                if (Value.Length == 0 || AttributeValue is null || !element.HasAttribute(Value))
                {
                    return false;
                }

                var actual = element.Attribute(Value);
                return VariantString(actual).Contains(VariantString(AttributeValue), StringComparison.OrdinalIgnoreCase);
            }

            case SelectorKind.AnyElement:
                return true;
            default:
                return false;
        }
    }

    internal static string VariantString(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    /// <summary>Returns true when a root-first chain matches a selector list.</summary>
    public static bool Matches(Selector[] selectors, ReadOnlySpan<ManagedElement> chain)
    {
        if (chain.Length == 0)
        {
            return false;
        }

        var s = selectors.Length - 1;
        var e = chain.Length - 1;
        while (s >= 0)
        {
            if (!selectors[s].Matches(chain[e]))
            {
                return false;
            }

            s--;
            if (s >= 0 && selectors[s].IsCombinator)
            {
                switch (selectors[s].Kind)
                {
                    case SelectorKind.ChildCombinator:
                        s--;
                        e--;
                        break;
                    case SelectorKind.DescendantCombinator:
                        s--;
                        e--;
                        while (e >= 0 && s >= 0 && !selectors[s].Matches(chain[e]))
                        {
                            e--;
                        }

                        break;
                }

                if (e < 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Kind switch
    {
        SelectorKind.State => $"State({State})",
        SelectorKind.AttributeEquals or SelectorKind.AttributeSubstringMatch => $"{Kind}({Value}={VariantString(AttributeValue)})",
        _ => Value.Length > 0 ? $"{Kind}({Value})" : Kind.ToString(),
    };
}
