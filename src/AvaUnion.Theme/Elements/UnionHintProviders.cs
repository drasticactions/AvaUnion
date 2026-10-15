using System;
using System.Collections.Generic;
using Avalonia;

namespace AvaUnion.Elements;

/// <summary>
/// Adds the hints and attributes of an element.
/// </summary>
/// <param name="element">The element.</param>
/// <param name="type">The Union type of the element.</param>
/// <param name="hints">The current hints. Add to them.</param>
/// <param name="attributes">The current attributes. Add to them.</param>
public delegate void UnionHintProvider(StyledElement element, string type, IList<string> hints, IList<KeyValuePair<string, object>> attributes);

/// <summary>
/// Lets control libraries describe their controls to Union. A library can give
/// hints that come from properties, and pseudo-classes that are hints, not
/// states. Register one time, before the controls show.
/// </summary>
public static class UnionHintProviders
{
    private static readonly object s_lock = new();
    private static (Type Owner, UnionHintProvider Provider)[] s_providers = [];
    private static (Type Owner, string PseudoClass, string Hint)[] s_pseudoClasses = [];
    private static HashSet<AvaloniaProperty> s_watched = [];

    /// <summary>
    /// Adds <paramref name="provider"/> for elements of type
    /// <typeparamref name="T"/>. The provider runs again when one of
    /// <paramref name="watched"/> changes on the element.
    /// </summary>
    public static void Register<T>(UnionHintProvider provider, params AvaloniaProperty[] watched)
        where T : StyledElement
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (s_lock)
        {
            s_providers = [.. s_providers, (typeof(T), provider)];
            s_watched = [.. s_watched, .. watched];
        }
    }

    /// <summary>
    /// Makes <paramref name="pseudoClass"/> including its colon, on elements of
    /// type <typeparamref name="T"/> the hint <paramref name="hint"/>, not a
    /// state.
    /// </summary>
    public static void MapPseudoClass<T>(string pseudoClass, string hint)
        where T : StyledElement
    {
        ArgumentException.ThrowIfNullOrEmpty(pseudoClass);
        ArgumentException.ThrowIfNullOrEmpty(hint);
        lock (s_lock)
        {
            s_pseudoClasses = [.. s_pseudoClasses, (typeof(T), pseudoClass, hint)];
        }
    }

    internal static bool IsWatched(AvaloniaProperty property) => s_watched.Contains(property);

    internal static string? HintFor(StyledElement source, string pseudoClass)
    {
        foreach (var (owner, pc, hint) in s_pseudoClasses)
        {
            if (pc == pseudoClass && owner.IsInstanceOfType(source))
            {
                return hint;
            }
        }

        return null;
    }

    internal static void Apply(StyledElement element, string type, List<string> hints, List<KeyValuePair<string, object>> attributes)
    {
        foreach (var (owner, provider) in s_providers)
        {
            if (owner.IsInstanceOfType(element))
            {
                provider(element, type, hints, attributes);
            }
        }
    }
}
