using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using AvaUnion.Controls;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DelayButtonTests
{
    private static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void Tick()
    {
        Thread.Sleep(20);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    [AvaloniaFact]
    public void Holding_For_The_Delay_Checks_And_Activates()
    {
        var button = new DelayButton { Content = "Delay button", Delay = TimeSpan.Zero };
        var activated = 0;
        button.Activated += (_, _) => activated++;
        var window = Shots.Show(button);
        Assert.Contains("delaybutton[display=text-beside-icon]", Union.GetNode(button)!.ChainText);

        window.MouseDown(Center(button, window), MouseButton.Left);
        Assert.False(button.IsChecked);
        Assert.Equal(1, button.Progress);
        Assert.Equal(1, activated);
        window.MouseUp(Center(button, window), MouseButton.Left);
        Assert.True(button.IsChecked);

        window.MouseDown(Center(button, window), MouseButton.Left);
        window.MouseUp(Center(button, window), MouseButton.Left);
        Assert.False(button.IsChecked);
        Assert.Equal(1, activated);
        window.Close();
    }

    [AvaloniaFact]
    public void Releasing_Early_Leaves_It_Unchecked()
    {
        var button = new DelayButton { Content = new UnionIcon { IconName = "document-save" }, Delay = TimeSpan.FromSeconds(10) };
        var window = Shots.Show(button);
        Assert.Contains("delaybutton.with-icon[display=icon-only]", Union.GetNode(button)!.ChainText);

        window.MouseDown(Center(button, window), MouseButton.Left);
        for (var i = 0; i < 5; i++)
        {
            Tick();
        }

        Assert.InRange(button.Progress, 0.0001, 0.5);
        window.MouseUp(Center(button, window), MouseButton.Left);
        Assert.False(button.IsChecked);
        for (var i = 0; i < 20 && button.Progress > 0; i++)
        {
            Tick();
        }

        Assert.Equal(0, button.Progress);
        window.Close();
    }
}
