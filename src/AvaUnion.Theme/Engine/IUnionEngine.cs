using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Engine;

/// <summary>A Union style engine.</summary>
public interface IUnionEngine
{
    /// <summary>The kind of this engine.</summary>
    UnionEngineKind Kind { get; }

    /// <summary>A short description for diagnostics, for example "Native · Union 6.7.91 · breeze".</summary>
    string Description { get; }

    /// <summary>The default style of the session. It follows changes.</summary>
    UnionStyleHandle DefaultStyle { get; }

    /// <summary>
    /// The styles that <see cref="GetStyle"/> can return. The application's
    /// style packages come first. The installed styles come after them. The
    /// list does not contain hidden packages. Each id is in the list one time.
    /// </summary>
    IReadOnlyList<UnionStyleMetadata> Styles { get; }

    /// <summary>Gets a style by id. Returns null when the style does not exist.</summary>
    UnionStyleHandle? GetStyle(string id);

    /// <summary>Creates an element.</summary>
    ElementHandle CreateElement();

    /// <summary>Resolves an element chain. The root is first.</summary>
    ResolvedStyle Query(UnionStyleHandle style, ReadOnlySpan<ElementHandle> chain);

    /// <summary>Gets one KColorScheme color.</summary>
    Color SchemeColor(ColorGroup group, ColorSet set, ColorRoleKind kind, int role);

    /// <summary>
    /// Renders an icon by name. When <paramref name="tint"/> is set, the icon
    /// gets this tint. Returns null when the icon does not exist.
    /// </summary>
    IconRaster? RenderIcon(string name, Color? tint, PixelSize size, double scale);

    /// <summary>Renders a style image at a size. When a mask color is given, the image is masked with it.</summary>
    IconRaster? RenderImage(string source, Color? maskColor, bool invertedMask, PixelSize size, double scale);

    /// <summary>
    /// Gets a style image as vector geometry. Returns null when the engine does
    /// not keep the image as geometry.
    /// </summary>
    UnionImageGeometry? GetImageGeometry(string source);

    /// <summary>The session fonts.</summary>
    UnionFonts Fonts { get; }

    /// <summary>The platform values.</summary>
    UnionPlatform Platform { get; }

    /// <summary>
    /// The path or name of the color scheme in use. Null is the color scheme of
    /// the session.
    /// </summary>
    string? ColorScheme { get; }

    /// <summary>Applies a color scheme to this application. Null applies the color scheme of the session.</summary>
    void SetColorScheme(string? pathOrName);

    /// <summary>Occurs on the UI thread when the session settings change.</summary>
    event Action<UnionChange>? Changed;

    /// <summary>Increases when the colors or the style change.</summary>
    int Epoch { get; }
}
