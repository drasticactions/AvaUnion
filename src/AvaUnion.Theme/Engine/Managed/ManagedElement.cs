using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AvaUnion.Engine.Managed;

/// <summary>The managed copy of a <c>Union::Element</c>.</summary>
public sealed class ManagedElement : ElementHandle
{
    private readonly SortedDictionary<string, object> _attributes = new(StringComparer.Ordinal);
    private string[] _hints = [];
    private string? _cacheKey;

    /// <summary>The type.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>The id.</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>The states.</summary>
    public UnionStates States { get; private set; }

    /// <summary>The hints, in the order they were set.</summary>
    public IReadOnlyList<string> Hints => _hints;

    /// <summary>The attributes.</summary>
    public IReadOnlyDictionary<string, object> Attributes => _attributes;

    /// <inheritdoc/>
    public override void SetType(string? type)
    {
        Type = type ?? string.Empty;
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void SetId(string? id)
    {
        Id = id ?? string.Empty;
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void SetStates(UnionStates states)
    {
        States = states;
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void SetHints(IReadOnlyList<string> hints)
    {
        _hints = [.. hints];
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void SetAttribute(string name, string value)
    {
        _attributes[name] = value;
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void SetAttribute(string name, double value)
    {
        _attributes[name] = value;
        _cacheKey = null;
    }

    /// <inheritdoc/>
    public override void ClearAttributes()
    {
        _attributes.Clear();
        _cacheKey = null;
    }

    /// <summary>Finds a hint. The comparison ignores case.</summary>
    public bool HasHint(string name)
    {
        foreach (var hint in _hints)
        {
            if (string.Equals(hint, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Finds an attribute. The comparison uses case.</summary>
    public bool HasAttribute(string name) => _attributes.ContainsKey(name);

    /// <summary>Gets an attribute value. The comparison ignores case. It is null when the attribute does not exist.</summary>
    public object? Attribute(string name)
    {
        foreach (var (key, value) in _attributes)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>The cache key. It contains the type, id, states, hints and attributes.</summary>
    public string CacheKey => _cacheKey ??= BuildKey();

    private string BuildKey()
    {
        var sb = new StringBuilder(64);
        sb.Append(Type).Append('\u001f').Append(Id).Append('\u001f').Append((int)States).Append('\u001f');
        foreach (var hint in _hints)
        {
            sb.Append(hint).Append('\u001e');
        }

        sb.Append('\u001f');
        foreach (var (key, value) in _attributes)
        {
            sb.Append(key).Append('=').Append(value is double d ? "n" + d.ToString("R", CultureInfo.InvariantCulture) : "s" + value).Append('\u001e');
        }

        return sb.ToString();
    }

    /// <summary>CSS-like text, for diagnostics.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Type.Length == 0 ? "*" : Type);
        if (Id.Length > 0)
        {
            sb.Append('#').Append(Id);
        }

        foreach (var hint in _hints)
        {
            sb.Append('.').Append(hint);
        }

        foreach (var (key, value) in _attributes)
        {
            sb.Append('[').Append(key).Append('=').Append(Selector.VariantString(value)).Append(']');
        }

        foreach (var state in Enum.GetValues<UnionStates>())
        {
            if (state != UnionStates.None && (States & state) != 0)
            {
                sb.Append(':').Append(UnionStateNames.Css(state));
            }
        }

        return sb.ToString();
    }
}
