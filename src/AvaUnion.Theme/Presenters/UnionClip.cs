using Avalonia;
using Avalonia.Controls;
using AvaUnion.Engine;

namespace AvaUnion.Presenters;

/// <summary>
/// Clips its child to the inside of the border of the nearest element, with
/// the rounded corners of that border. Put it in the box of the element, with
/// the border width as its margin, so that content such as a header does not
/// cover the corners of the frame.
/// </summary>
public class UnionClip : Decorator
{
    static UnionClip()
    {
        ClipToBoundsProperty.OverrideDefaultValue<UnionClip>(false);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Union.StyleProperty)
        {
            UpdateClip(Bounds.Size);
        }
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateClip(size);
        return size;
    }

    private void UpdateClip(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            Clip = null;
            return;
        }

        var style = Union.GetStyle(this) ?? ResolvedStyle.Unmatched;
        var border = style.Border.DrawnWidths();
        var outer = new Size(size.Width + border.Left + border.Right, size.Height + border.Top + border.Bottom);
        var radii = BoxGeometry.Offset(BoxGeometry.Clamp(style.Corners.Radii(), outer), border, -1);
        Clip = BoxGeometry.RoundedRect(new Rect(size), radii);
    }
}
