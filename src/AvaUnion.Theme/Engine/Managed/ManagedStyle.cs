using System.Collections.Generic;

namespace AvaUnion.Engine.Managed;

/// <summary>The rules of a style package, in the order of the style.</summary>
/// <param name="Id">The style id.</param>
/// <param name="Rules">The rules.</param>
public sealed record ManagedStyle(string Id, IReadOnlyList<ManagedRule> Rules);
