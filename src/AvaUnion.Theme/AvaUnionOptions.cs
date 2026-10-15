using System.Collections.Generic;
using AvaUnion.Engine;

namespace AvaUnion;

/// <summary>Options for the process. Set them with <see cref="AppBuilderExtensions.WithAvaUnion"/>.</summary>
public sealed class AvaUnionOptions
{
    /// <summary>The engine to use. <see cref="UnionEngineKind.Auto"/> uses the native engine when it can.</summary>
    public UnionEngineKind Engine { get; set; } = UnionEngineKind.Auto;

    /// <summary>True when the app enables the GLib main loop of Avalonia on X11 or Wayland.</summary>
    public bool HostRunsGLibMainLoop { get; set; }

    /// <summary>
    /// The style packages that the application ships for its own use. They
    /// come before the packages that discovery finds. If two packages have
    /// the same id, the first one is used. Changes after the engine starts
    /// have no effect.
    /// </summary>
    public IList<UnionStylePackage> StylePackages { get; } = new List<UnionStylePackage>();

    /// <summary>
    /// True adds the packages that <see cref="UnionStylePackage.Discover(System.Reflection.Assembly)"/>
    /// finds for the entry assembly to <see cref="StylePackages"/>.
    /// </summary>
    public bool DiscoverStylePackages { get; set; } = true;
}
