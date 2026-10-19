using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaUnion.Presenters;

namespace AvaUnion.Gallery;

public sealed class IconLabel : StackPanel
{
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<IconLabel, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconLabel, string?>(nameof(Text));

    private readonly UnionIcon _icon = new();
    private readonly TextBlock _text = new() { VerticalAlignment = VerticalAlignment.Center };

    public IconLabel()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 4;
        Children.Add(_icon);
        Children.Add(_text);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
        {
            _icon.IconName = Icon;
        }
        else if (change.Property == TextProperty)
        {
            _text.Text = Text;
        }
        else if (change.Property == OrientationProperty)
        {
            Spacing = Orientation == Orientation.Vertical ? 0 : 4;
            _text.HorizontalAlignment = Orientation == Orientation.Vertical ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            _icon.HorizontalAlignment = Orientation == Orientation.Vertical ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        }
    }
}
