using Avalonia.Controls;

namespace AvaUnion;

/// <summary>Sets a different Union style for a subtree.</summary>
public class UnionScope : Decorator
{
    /// <summary>The style package for the subtree. If it is null, the subtree inherits the style.</summary>
    public string? StyleId
    {
        get => Union.GetStyleId(this);
        set => Union.SetStyleId(this, value);
    }
}
