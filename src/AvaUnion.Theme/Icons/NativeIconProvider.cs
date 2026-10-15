using Avalonia;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Presenters;

namespace AvaUnion.Icons;

/// <summary>
/// Finds icons through the <c>platformIcon</c> of Union.
/// </summary>
public sealed class NativeIconProvider : IIconProvider
{
    /// <inheritdoc/>
    public IImage? GetIcon(string name, Color? tint, Size size, double scale)
    {
        var px = new PixelSize((int)System.Math.Ceiling(size.Width), (int)System.Math.Ceiling(size.Height));
        if (px.Width <= 0 || px.Height <= 0)
        {
            return null;
        }

        return UnionImageCache.ToBitmap(UnionEnvironment.Engine.RenderIcon(name, tint, px, scale), scale);
    }
}
