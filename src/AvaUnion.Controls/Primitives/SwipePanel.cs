using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class SwipePanel : Panel
{
    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<SwipePanel, double>(nameof(Position));

    public static readonly StyledProperty<Control?> FrontProperty =
        AvaloniaProperty.Register<SwipePanel, Control?>(nameof(Front));

    static SwipePanel() => AffectsArrange<SwipePanel>(PositionProperty);

    public SwipePanel() => ClipToBounds = true;

    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public Control? Front
    {
        get => GetValue(FrontProperty);
        set => SetValue(FrontProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var x = ReferenceEquals(child, Front) ? Position * finalSize.Width : 0.0;
            child.Arrange(new Rect(x, 0, finalSize.Width, finalSize.Height));
        }

        return finalSize;
    }
}
