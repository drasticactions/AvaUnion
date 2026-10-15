using System;
using System.Collections.Generic;
using Avalonia.Animation;
using AvaUnion.Elements;
using AvaUnion.Engine;

namespace AvaUnion.Markup;

internal static class Scaled
{
    private static readonly List<(WeakReference<object> Target, TimeSpan Duration)> s_targets = [];
    private static bool s_subscribed;

    public static double Speed => UnionEnvironment.IsReady ? Math.Max(0.0, UnionEnvironment.Engine.Platform.AnimationSpeed) : 1.0;

    public static TimeSpan Scale(TimeSpan duration) => TimeSpan.FromTicks((long)Math.Round(duration.Ticks * Speed));

    public static bool CanSet(object target) => target is TransitionBase or Animation or PageSlide or CrossFade;

    public static void Track(object target, TimeSpan duration)
    {
        if (!s_subscribed)
        {
            s_subscribed = true;
            UnionEnvironment.Changed += change =>
            {
                if ((change & UnionChange.Animation) != 0)
                {
                    Rescale();
                }
            };
        }

        s_targets.Add((new WeakReference<object>(target), duration));
    }

    public static void Rescale()
    {
        s_targets.RemoveAll(t => !t.Target.TryGetTarget(out _));
        foreach (var (reference, duration) in s_targets)
        {
            if (reference.TryGetTarget(out var target))
            {
                Set(target, Scale(duration));
            }
        }
    }

    private static void Set(object target, TimeSpan duration)
    {
        switch (target)
        {
            case TransitionBase transition:
                transition.Duration = duration;
                break;
            case Animation animation:
                animation.Duration = duration;
                break;
            case PageSlide slide:
                slide.Duration = duration;
                break;
            case CrossFade fade:
                fade.Duration = duration;
                break;
        }
    }
}
