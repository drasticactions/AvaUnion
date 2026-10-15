namespace AvaUnion.Engine;

/// <summary>The style engine that resolves Union queries.</summary>
public enum UnionEngineKind
{
    /// <summary>Native when the shim loads and matches the installed Union. Managed in all other cases.</summary>
    Auto,

    /// <summary>
    /// Union through libavaunion-native. If the shim is not available,
    /// initialization fails with an exception.
    /// </summary>
    Native,

    /// <summary>Managed code with the bundled Breeze snapshot.</summary>
    Managed,
}
