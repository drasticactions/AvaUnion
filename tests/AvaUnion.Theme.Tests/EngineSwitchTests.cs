using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Elements;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class EngineSwitchTests
{
    private static Control Page()
    {
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(12) };
        panel.Children.Add(new Button { Content = "Button" });
        panel.Children.Add(new Button { Content = "Flat", Classes = { "flat" } });
        panel.Children.Add(new ToggleButton { Content = "Toggle", IsChecked = true });
        panel.Children.Add(new CheckBox { Content = "Check", IsChecked = true });
        panel.Children.Add(new RadioButton { Content = "Radio" });
        panel.Children.Add(new ToggleSwitch { Content = "Switch" });
        panel.Children.Add(new TextBox { Text = "Text", Width = 200 });
        panel.Children.Add(new ComboBox { ItemsSource = new[] { "One", "Two" }, SelectedIndex = 0, Width = 200 });
        panel.Children.Add(new NumericUpDown { Value = 3, Width = 160 });
        panel.Children.Add(new Slider { Width = 200, Value = 40 });
        panel.Children.Add(new ProgressBar { Width = 200, Value = 60 });
        panel.Children.Add(new ListBox { ItemsSource = new[] { "Alpha", "Beta", "Gamma" }, Height = 110 });
        panel.Children.Add(new TabControl { ItemsSource = new[] { new TabItem { Header = "One" }, new TabItem { Header = "Two" } } });
        panel.Children.Add(new Expander { Header = "Expander", IsExpanded = true, Content = "Body" });
        return panel;
    }

    private static List<(string Name, Rect Bounds)> Layout(Window window, Control root) =>
        root.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.IsEffectivelyVisible)
            .Select(c => ($"{c.GetType().Name}#{c.Name}", c.TranslatePoint(default, window) is { } p ? new Rect(p, c.Bounds.Size) : default))
            .ToList();

    [AvaloniaFact]
    public void Native_And_Managed_Lay_Out_The_Same()
    {
        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var managed = UnionEnvironment.Engine;
        UnionEnvironment.Flush();
        var page = Page();
        var window = Shots.Show(page, 480, 900);
        var expected = Layout(window, page);
        window.Close();

        var native = new ThreadedEngine();
        UnionEnvironment.Install(native);
        try
        {
            UnionEnvironment.Cache.Clear();
            page = Page();
            window = Shots.Show(page, 480, 900);
            var actual = Layout(window, page);
            window.Close();

            Assert.True(native.Queries > 50, $"{native.Queries} native queries");
            Assert.Equal(expected.Count, actual.Count);
            var off = expected.Zip(actual)
                .Where(p => p.First.Name != p.Second.Name
                    || Math.Abs(p.First.Bounds.X - p.Second.Bounds.X) > 1 || Math.Abs(p.First.Bounds.Y - p.Second.Bounds.Y) > 1
                    || Math.Abs(p.First.Bounds.Width - p.Second.Bounds.Width) > 1 || Math.Abs(p.First.Bounds.Height - p.Second.Bounds.Height) > 1)
                .Select(p => $"{p.First.Name} {p.First.Bounds} ≠ {p.Second.Name} {p.Second.Bounds}")
                .ToList();
            Assert.True(off.Count == 0, string.Join("\n", off.Take(20)));
        }
        finally
        {
            UnionEnvironment.Install(managed);
            UnionEnvironment.Cache.Clear();
        }
    }
}
