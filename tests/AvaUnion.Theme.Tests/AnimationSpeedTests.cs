using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaUnion.Elements;
using AvaUnion.Engine.Managed;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class AnimationSpeedTests
{
    [AvaloniaFact]
    public void Durations_Follow_The_Animation_Speed()
    {
        var toggle = new ToggleSwitch();
        var window = Shots.Show(toggle, 200, 100);
        var engine = (ManagedEngine)UnionEnvironment.Engine;
        TimeSpan Knob() => toggle.KnobTransitions!.OfType<TransitionBase>().Single().Duration;

        try
        {
            Assert.Equal(TimeSpan.FromSeconds(0.15), Knob());

            engine.SetAnimationSpeed(0.5);
            UnionEnvironment.Flush();
            Assert.Equal(TimeSpan.FromSeconds(0.075), Knob());

            engine.SetAnimationSpeed(0);
            UnionEnvironment.Flush();
            Assert.Equal(TimeSpan.Zero, Knob());
        }
        finally
        {
            engine.SetAnimationSpeed(1);
            UnionEnvironment.Flush();
            window.Close();
        }

        Assert.Equal(TimeSpan.FromSeconds(0.15), Knob());
    }
}
