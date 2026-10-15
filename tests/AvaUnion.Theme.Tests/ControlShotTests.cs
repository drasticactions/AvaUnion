using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ControlShotTests
{
    [AvaloniaFact]
    public void Inputs_Render()
    {
        var panel = new StackPanel { Spacing = 10, Margin = new Thickness(16) };
        panel.Children.Add(new TextBox { Text = "Some text" });
        panel.Children.Add(new TextBox { PlaceholderText = "Placeholder" });
        panel.Children.Add(new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, SelectedIndex = 1 });
        panel.Children.Add(new Slider { Value = 40, Width = 200, HorizontalAlignment = HorizontalAlignment.Left });
        panel.Children.Add(new ProgressBar { Value = 60, Width = 200, HorizontalAlignment = HorizontalAlignment.Left });
        panel.Children.Add(new ProgressBar { IsIndeterminate = true, Width = 200, HorizontalAlignment = HorizontalAlignment.Left });
        var window = Shots.Show(panel, 320, 330);
        Shots.Capture(window, "inputs");
        Assert.Equal("textfield", AvaUnion.Union.GetNode((Control)panel.Children[0])!.Type);
        Assert.True(((Control)panel.Children[0]).Bounds.Height >= 32);
    }

    [AvaloniaFact]
    public void Lists_Render()
    {
        var list = new ListBox { ItemsSource = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToArray(), SelectedIndex = 2, Height = 200, Width = 220 };
        var panel = new StackPanel { Spacing = 10, Margin = new Thickness(16) };
        panel.Children.Add(list);
        var window = Shots.Show(panel, 300, 260);
        Shots.Capture(window, "lists");
        var item = list.ContainerFromIndex(2)!;
        var node = AvaUnion.Union.GetNode(item)!;
        Assert.Equal("itemdelegate", node.Type);
        Assert.Contains("inside-list", node.Hints);
        Assert.True((node.States & Engine.UnionStates.Highlighted) != 0);
        Assert.Contains("framed-background", AvaUnion.Union.GetNode(list)!.Hints);
    }

    [AvaloniaFact]
    public void Menus_Render()
    {
        var menu = new Menu();
        var file = new MenuItem { Header = "_File" };
        file.Items.Add(new MenuItem { Header = "Open" });
        menu.Items.Add(file);
        menu.Items.Add(new MenuItem { Header = "_Edit" });
        var target = new Border { Height = 200 };
        var context = new ContextMenu();
        context.Items.Add(new MenuItem { Header = "Cut", InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.X, Avalonia.Input.KeyModifiers.Control) });
        context.Items.Add(new MenuItem { Header = "Check", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true });
        context.Items.Add(new Separator());
        var sub = new MenuItem { Header = "More" };
        sub.Items.Add(new MenuItem { Header = "Inner" });
        context.Items.Add(sub);
        target.ContextMenu = context;
        var dock = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        dock.Children.Add(menu);
        dock.Children.Add(target);
        var window = Shots.Show(dock, 360, 260);
        context.Open(target);
        Shots.Settle();
        Shots.Capture(window, "menus");
        Assert.Equal("menubar", AvaUnion.Union.GetNode(menu)!.Type);
        Assert.Equal("menubaritem", AvaUnion.Union.GetNode(file)!.Type);
        Assert.Equal("menu", AvaUnion.Union.GetNode(context)!.Type);
        var item = (MenuItem)context.Items[0]!;
        var chain = AvaUnion.Union.GetNode(item)!.ChainText;
        Assert.StartsWith("applicationwindow", chain);
        Assert.Contains("menu > menuitem", chain);
        Assert.Equal("menuseparator", AvaUnion.Union.GetNode((Separator)context.Items[2]!)!.Type);
        context.Close();
    }
}
