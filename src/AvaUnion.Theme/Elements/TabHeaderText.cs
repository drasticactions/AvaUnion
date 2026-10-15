using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaUnion.Elements;

internal static class TabHeaderText
{
    public static void Register()
    {
        LayoutTransformControl.LayoutTransformProperty.Changed.AddClassHandler<LayoutTransformControl>((header, _) => Update(header));
    }

    private static void Update(LayoutTransformControl header)
    {
        if (header.Name != "PART_HeaderTransform" || header.TemplatedParent is not TabItem)
        {
            return;
        }

        var mode = header.LayoutTransform is null ? TextRenderingMode.Unspecified : TextRenderingMode.Antialias;
        TextOptions.SetTextRenderingMode(header, mode);
    }
}
