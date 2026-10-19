using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using AvaUnion.Elements;

namespace AvaUnion.Controls;

/// <summary>A text for titles and section headers. The level sets the size.</summary>
public class Heading : Label
{
    /// <summary>Defines the <see cref="Level"/> property.</summary>
    public static readonly StyledProperty<int> LevelProperty =
        AvaloniaProperty.Register<Heading, int>(nameof(Level), 1, coerce: (_, value) => System.Math.Clamp(value, 1, 5));

    /// <summary>Defines the <see cref="Type"/> property.</summary>
    public static readonly StyledProperty<HeadingType> TypeProperty =
        AvaloniaProperty.Register<Heading, HeadingType>(nameof(Type));

    static Heading()
    {
        UnionHintProviders.Register<Heading>(
            static (element, _, hints, _) =>
            {
                var heading = (Heading)element;
                hints.Add("level-" + heading.Level);
                if (heading.Type == HeadingType.Primary)
                {
                    hints.Add("primary");
                }
                else if (heading.Type == HeadingType.Secondary)
                {
                    hints.Add("secondary");
                }
            },
            LevelProperty,
            TypeProperty);
        AutomationProperties.HeadingLevelProperty.OverrideDefaultValue<Heading>(1);
    }

    /// <summary>The level, from 1 (largest) to 5 (smallest). The default is 1.</summary>
    public int Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <summary>How prominent the heading is. The default is <see cref="HeadingType.Normal"/>.</summary>
    public HeadingType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LevelProperty)
        {
            AutomationProperties.SetHeadingLevel(this, Level);
        }
    }
}
