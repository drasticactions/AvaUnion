using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using AvaUnion.Elements;
using AvaUnion.Engine.Managed;
using Xunit;

namespace AvaUnion.Theme.Tests;

[Collection(PerformanceCollection.Name)]
public class PerformanceTests
{
#if DEBUG
    private const double ReleaseBudget = 15.0;
#else
    private const double ReleaseBudget = 5.0;
#endif

    private static readonly double CiFactor =
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase) ? 3 : 1;

    private static readonly double SchemeBudget = ReleaseBudget * CiFactor;

    private static readonly double HoverBudget = 0.2 * CiFactor;

    [AvaloniaFact]
    public void Hover_And_Scheme_Changes_Are_Cheap()
    {
        var panel = new WrapPanel();
        for (var i = 0; i < 1000; i++)
        {
            panel.Children.Add(new Button { Content = "b" });
        }

        var window = Shots.Show(new ScrollViewer { Content = panel }, 800, 600);

        foreach (var stale in UnionEnvironment.LiveNodes.Where(n => n.Owner is not Avalonia.Visual v || Avalonia.Controls.TopLevel.GetTopLevel(v) != window).ToList())
        {
            stale.Detach();
        }

        Assert.True(UnionEnvironment.LiveNodeCount >= 2000, $"{UnionEnvironment.LiveNodeCount} nodes");

        var button = (Button)panel.Children[500];
        for (var i = 0; i < 20; i++)
        {
            ((IPseudoClasses)button.Classes).Set(":pointerover", i % 2 == 0);
            UnionEnvironment.Flush();
        }

        var hover = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            ((IPseudoClasses)button.Classes).Set(":pointerover", i % 2 == 0);
            UnionEnvironment.Flush();
        }

        hover.Stop();
        var perHover = hover.Elapsed.TotalMilliseconds / 100;

        var engine = (ManagedEngine)UnionEnvironment.Engine;
        engine.SetColorScheme("BreezeDark");
        UnionEnvironment.Flush();
        engine.SetColorScheme(null);
        UnionEnvironment.Flush();
        for (var i = 0; i < 3; i++)
        {
            engine.SetColorScheme("BreezeDark");
            UnionEnvironment.Flush();
            engine.SetColorScheme(null);
            UnionEnvironment.Flush();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var times = new double[5];
        for (var i = 0; i < times.Length; i++)
        {
            var watch = Stopwatch.StartNew();
            engine.SetColorScheme("BreezeDark");
            UnionEnvironment.Flush();
            times[i] = watch.Elapsed.TotalMilliseconds;
            engine.SetColorScheme(null);
            UnionEnvironment.Flush();
        }

        Array.Sort(times);
        var scheme = times[times.Length / 2];

        TestContext.Current.SendDiagnosticMessage($"hover {perHover:F3} ms, scheme {scheme:F1} ms, {UnionEnvironment.LiveNodeCount} nodes");
        Assert.True(perHover < HoverBudget, $"hover took {perHover:F3} ms");
        Assert.True(scheme < SchemeBudget, $"scheme change took {scheme:F1} ms (median of five), {UnionEnvironment.LiveNodeCount} live nodes");
        window.Close();
    }
}
