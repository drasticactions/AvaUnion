using Avalonia.Media;

namespace AvaUnion.Icons;

internal abstract class SvgNode
{
    public abstract Drawing? Build(Color? tint);
}
