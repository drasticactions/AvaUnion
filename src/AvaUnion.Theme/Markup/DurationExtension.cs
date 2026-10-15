using System;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Markup;

/// <summary>
/// A duration that the animation speed of the session scales. On a transition, an animation or a page transition, it
/// follows changes of the speed. At speed 0, the animations stop immediately.
/// </summary>
public sealed class DurationExtension : MarkupExtension
{
    /// <summary>Creates the extension.</summary>
    public DurationExtension()
    {
    }

    /// <summary>Creates the extension.</summary>
    public DurationExtension(TimeSpan duration)
    {
        Duration = duration;
    }

    /// <summary>The duration at normal speed.</summary>
    public TimeSpan Duration { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget { TargetObject: { } target } && Scaled.CanSet(target))
        {
            Scaled.Track(target, Duration);
        }

        return Scaled.Scale(Duration);
    }
}
