using System;
using System.Collections.Generic;

namespace AvaUnion.Engine;

/// <summary>One Union element.</summary>
public abstract class ElementHandle : IDisposable
{
    /// <summary>Sets the lowercase type name.</summary>
    public abstract void SetType(string? type);

    /// <summary>Sets the id.</summary>
    public abstract void SetId(string? id);

    /// <summary>Sets the state bits.</summary>
    public abstract void SetStates(UnionStates states);

    /// <summary>Replaces the hints.</summary>
    public abstract void SetHints(IReadOnlyList<string> hints);

    /// <summary>Sets a string attribute.</summary>
    public abstract void SetAttribute(string name, string value);

    /// <summary>Sets a numeric attribute.</summary>
    public abstract void SetAttribute(string name, double value);

    /// <summary>Removes every attribute.</summary>
    public abstract void ClearAttributes();

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the element.</summary>
    protected virtual void Dispose(bool disposing)
    {
    }
}
