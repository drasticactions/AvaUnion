namespace AvaUnion.Engine;

/// <summary>Where a style comes from.</summary>
public enum UnionStyleOrigin
{
    /// <summary>A style package that the application ships for its own use.</summary>
    Application,

    /// <summary>A style package that is installed on the system.</summary>
    Installed,

    /// <summary>A style from the Breeze snapshot.</summary>
    Snapshot,
}
