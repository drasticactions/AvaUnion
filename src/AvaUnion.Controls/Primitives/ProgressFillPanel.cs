using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class ProgressFillPanel : Panel
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressFillPanel, double>(nameof(Progress));

    static ProgressFillPanel() => AffectsArrange<ProgressFillPanel>(ProgressProperty);

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width * System.Math.Clamp(Progress, 0.0, 1.0);
        foreach (var child in Children)
        {
            child.Arrange(new Rect(0, 0, width, finalSize.Height));
        }

        return finalSize;
    }
}
