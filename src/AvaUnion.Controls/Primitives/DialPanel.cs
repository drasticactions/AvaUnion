using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaUnion.Controls.Primitives;

internal sealed class DialPanel : Panel
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<DialPanel, double>(nameof(Angle));

    public static readonly StyledProperty<Control?> BackgroundBoxProperty =
        AvaloniaProperty.Register<DialPanel, Control?>(nameof(BackgroundBox));

    public static readonly StyledProperty<Control?> HandleProperty =
        AvaloniaProperty.Register<DialPanel, Control?>(nameof(Handle));

    static DialPanel() => AffectsArrange<DialPanel>(AngleProperty);

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public Control? BackgroundBox
    {
        get => GetValue(BackgroundBoxProperty);
        set => SetValue(BackgroundBoxProperty, value);
    }

    public Control? Handle
    {
        get => GetValue(HandleProperty);
        set => SetValue(HandleProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = Math.Max(64.0, Math.Min(finalSize.Width, finalSize.Height));
        var box = new Rect((finalSize.Width - size) / 2, (finalSize.Height - size) / 2, size, size);
        BackgroundBox?.Arrange(box);
        if (Handle is { } handle)
        {
            var w = handle.DesiredSize.Width;
            var h = handle.DesiredSize.Height;
            var radius = (size * 0.45) - (h / 2);
            var radians = Angle * Math.PI / 180.0;
            var x = box.Center.X + (radius * Math.Sin(radians)) - (w / 2);
            var y = box.Center.Y - (radius * Math.Cos(radians)) - (h / 2);
            handle.Arrange(new Rect(x, y, w, h));
        }

        return finalSize;
    }
}
