using Avalonia;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaUnion.Presenters;

internal static class VisualExtensions
{
    public static bool GetVisualParentIs(this Visual visual, Visual parent) => ReferenceEquals(visual.GetVisualParent(), parent);

    public static Layoutable? GetVisualParentAsLayoutable(this Visual visual)
    {
        var parent = visual.GetVisualParent();
        if (parent is PositionedGroup group)
        {
            parent = group.GetVisualParent();
        }

        return parent as Layoutable;
    }
}
