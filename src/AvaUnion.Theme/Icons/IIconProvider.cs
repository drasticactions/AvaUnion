using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Icons;

/// <summary>Finds icons by freedesktop name.</summary>
public interface IIconProvider
{
    /// <summary>Gets the icon, or null when the name is unknown. A symbolic icon gets the color <paramref name="tint"/>.</summary>
    /// <param name="name">The icon name, for example <c>arrow-down</c>.</param>
    /// <param name="tint">The color for symbolic icons, or null.</param>
    /// <param name="size">The logical size.</param>
    /// <param name="scale">The render scaling.</param>
    IImage? GetIcon(string name, Color? tint, Size size, double scale);
}
