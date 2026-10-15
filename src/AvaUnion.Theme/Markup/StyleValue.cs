namespace AvaUnion.Markup;

internal sealed record StyleValue(Engine.ResolvedStyle Style, StyleAccessor Accessor, string? Key, bool RightToLeft = false);
