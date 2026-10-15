namespace AvaUnion.Engine;

/// <summary>A style package that an engine knows, for example <c>breeze</c>.</summary>
public sealed class UnionStyleHandle
{
    internal UnionStyleHandle(string id, bool followsDefault, object payload, UnionStyleMetadata metadata)
    {
        Id = id;
        FollowsDefault = followsDefault;
        Payload = payload;
        Metadata = metadata;
    }

    /// <summary>The style id. This is the name of the package directory.</summary>
    public string Id { get; internal set; }

    /// <summary>True when this handle follows the default style of the session.</summary>
    public bool FollowsDefault { get; }

    /// <summary>The metadata of the style package.</summary>
    public UnionStyleMetadata Metadata { get; internal set; }

    internal object Payload { get; set; }

    /// <inheritdoc/>
    public override string ToString() => FollowsDefault ? $"{Id} (default)" : Id;
}
