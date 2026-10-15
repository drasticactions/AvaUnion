using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine.Managed;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DarkTextTests
{
    [AvaloniaFact]
    public void No_Control_Falls_Back_To_Black_Text()
    {
        var panel = new WrapPanel();
        foreach (var type in typeof(Button).Assembly.GetTypes().Where(t =>
                     typeof(TemplatedControl).IsAssignableFrom(t) && t is { IsAbstract: false, IsPublic: true }
                     && t.GetConstructor(Type.EmptyTypes) is not null
                     && !typeof(TopLevel).IsAssignableFrom(t) && !typeof(PopupRoot).IsAssignableFrom(t)))
        {
            try
            {
                panel.Children.Add((Control)Activator.CreateInstance(type)!);
            }
            catch (Exception)
            {
            }
        }

        var window = Shots.Show(new ScrollViewer { Content = panel }, 1200, 900);
        var engine = (ManagedEngine)UnionEnvironment.Engine;
        try
        {
            engine.SetColorScheme("BreezeDark");
            UnionEnvironment.Flush();
            window.UpdateLayout();
            var black = panel.Children.OfType<TemplatedControl>()
                .Where(c => c.Foreground is ISolidColorBrush { Color: var color } && color == Colors.Black)
                .Select(c => c.GetType().Name)
                .ToList();
            Assert.True(black.Count == 0, "black text: " + string.Join(", ", black));
        }
        finally
        {
            engine.SetColorScheme(null);
            UnionEnvironment.Flush();
            window.Close();
        }
    }
}
