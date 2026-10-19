using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Engine;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ThemeTests
{
    public static IEnumerable<object[]> Controls() =>
    [
        [typeof(Button), "button"],
        [typeof(RepeatButton), "button"],
        [typeof(ToggleButton), "button"],
        [typeof(HyperlinkButton), "button"],
        [typeof(CheckBox), "checkbox"],
        [typeof(RadioButton), "radiobutton"],
        [typeof(TextBox), "textfield"],
        [typeof(ComboBox), "combobox"],
        [typeof(ListBox), "scrollview"],
        [typeof(ScrollViewer), "scrollview"],
        [typeof(ScrollBar), "scrollbar"],
        [typeof(Slider), "slider"],
        [typeof(ProgressBar), "progressbar"],
        [typeof(Menu), "menubar"],
        [typeof(Separator), "toolseparator"],
        [typeof(Label), "label"],
        [typeof(ToggleSwitch), "switch"],
        [typeof(GridSplitter), "splitview"],
        [typeof(PipsPager), "pageindicator"],
        [typeof(RefreshVisualizer), "busyindicator"],
        [typeof(CommandBar), "toolbar"],
        [typeof(CommandBarButton), "toolbutton"],
        [typeof(Avalonia.Controls.Notifications.NotificationCard), "inlinemessage"],
    ];

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void Control_Has_A_Template_And_Its_Element(Type type, string element)
    {
        var control = (TemplatedControl)Activator.CreateInstance(type)!;
        var window = Shots.Show(control, 300, 200);
        Assert.NotNull(control.Template);
        Assert.NotEmpty(control.GetVisualChildren());
        var node = Union.GetNode(control);
        Assert.NotNull(node);
        Assert.Equal(element, node!.Type);
        Assert.True(node.Resolved.IsMatch);
        Assert.StartsWith("applicationwindow", node.ChainText);
        window.Close();
    }

    private static readonly HashSet<Type> s_noTheme =
    [
        typeof(TemplatedControl), typeof(ContentControl), typeof(UserControl), typeof(WindowBase), typeof(Thumb),
        typeof(SelectingItemsControl), typeof(HeaderedItemsControl), typeof(HeaderedSelectingItemsControl),
        typeof(MaskedTextBox), typeof(NativeMenuBar), typeof(PageNavigationHost), typeof(Avalonia.Dialogs.AboutAvaloniaDialog),
    ];

    private static Type StyleKeyOf(Type type) =>
        type.Assembly == typeof(Controls.UnionControlsTheme).Assembly && Activator.CreateInstance(type) is StyledElement element ? element.StyleKey : type;

    [AvaloniaFact]
    public void Every_Stock_Templated_Control_Has_A_Theme()
    {
        _ = Shots.Show(new Panel());
        var assemblies = new[] { typeof(Button).Assembly, typeof(Avalonia.Dialogs.ManagedFileChooser).Assembly, typeof(DataGridRow).Assembly, typeof(Controls.UnionControlsTheme).Assembly };
        var missing = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(TemplatedControl).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && !s_noTheme.Contains(t))
            .Where(t => !Application.Current!.TryGetResource(StyleKeyOf(t), null, out var theme) || theme is not Avalonia.Styling.ControlTheme)
            .Select(t => t.FullName)
            .Order()
            .ToList();
        Assert.True(missing.Count == 0, string.Join(", ", missing));
    }

    [AvaloniaFact]
    public void No_Template_Part_Takes_Its_Parents_Theme()
    {
        _ = Shots.Show(new Panel());
        var assemblies = new[] { typeof(Button).Assembly, typeof(Avalonia.Dialogs.ManagedFileChooser).Assembly, typeof(DataGridRow).Assembly, typeof(Controls.UnionControlsTheme).Assembly };
        var reached = new SortedSet<string>();
        foreach (var type in assemblies.SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(TemplatedControl).IsAssignableFrom(t) && !typeof(TopLevel).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) is not null))
        {
            var control = (TemplatedControl)Activator.CreateInstance(type)!;
            if (control is ContentControl { Content: null } content)
            {
                content.Content = "x";
            }

            var window = Shots.Show(control, 300, 200);
            foreach (var part in control.GetVisualDescendants().OfType<StyledElement>())
            {
                if (part.TemplatedParent is not StyledElement parent)
                {
                    continue;
                }

                var theme = parent.Theme ?? (parent.TryFindResource(parent.StyleKey, out var found) ? found as Avalonia.Styling.ControlTheme : null);
                for (; theme is not null; theme = theme.BasedOn)
                {
                    if (theme.TargetType!.IsAssignableFrom(part.StyleKey) && theme.Setters.Count > 0)
                    {
                        reached.Add($"{parent.GetType().Name} > {part.GetType().Name}");
                    }
                }
            }

            window.Close();
        }

        Assert.True(reached.Count == 0, string.Join(", ", reached));
    }

    [AvaloniaFact]
    public void Spin_Boxes_Are_Constrained_Only_When_Text_And_Indicators_Do_Not_Fit()
    {
        var wide = new NumericUpDown { Value = 10, Minimum = 0, Maximum = 10, Width = 200 };
        var narrow = new NumericUpDown { Value = 5, Minimum = 0, Maximum = 1000000, Width = 80 };
        var free = new NumericUpDown { Value = 10, Minimum = 0, Maximum = 10 };
        var window = Shots.Show(new StackPanel { Children = { wide, narrow, free } });
        Assert.DoesNotContain("constrained", Union.GetNode(wide)!.Hints);
        Assert.Contains("constrained", Union.GetNode(narrow)!.Hints);
        Assert.DoesNotContain("constrained", Union.GetNode(free)!.Hints);
        var narrowField = narrow.GetVisualDescendants().OfType<TextBox>().Single();
        var freeField = free.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Equal(Union.GetStyle(narrow)!.Layout.Margins.Left, narrowField.TranslatePoint(default, narrow)!.Value.X);
        Assert.Equal(Avalonia.Media.TextAlignment.Start, narrowField.TextAlignment);
        Assert.Equal(Avalonia.Media.TextAlignment.Center, freeField.TextAlignment);
        Assert.Equal(Math.Ceiling(Elements.AutoHints.LimitTextWidth(narrow)), narrowField.Bounds.Width);
        narrow.Width = 300;
        Shots.Settle();
        Assert.DoesNotContain("constrained", Union.GetNode(narrow)!.Hints);
        window.Close();
    }

    [AvaloniaFact]
    public void Button_Spinners_Are_One_Spin_Box()
    {
        var spinner = new ButtonSpinner { Content = "12", Width = 120, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var numeric = new NumericUpDown { Value = 12, Width = 120, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var window = Shots.Show(new StackPanel { Spacing = 8, Margin = new Thickness(16), Children = { spinner, numeric } }, 200, 120);
        Shots.Capture(window, "buttonspinner");
        Assert.Empty(spinner.GetVisualDescendants().OfType<ButtonSpinner>());
        var increase = spinner.GetVisualDescendants().OfType<RepeatButton>().Last();
        Assert.EndsWith("> spinbox > indicator.increase", Union.GetNode(increase)!.ChainText);
        Assert.DoesNotContain("spinbox > spinbox", Union.GetNode(increase)!.ChainText);
        window.Close();
    }

    [AvaloniaFact]
    public void Scroll_Bar_Handles_Start_At_The_Top_And_Left()
    {
        var vertical = new ScrollBar { Orientation = Avalonia.Layout.Orientation.Vertical, Height = 200, Maximum = 100, ViewportSize = 30, Value = 0, AllowAutoHide = false };
        var horizontal = new ScrollBar { Orientation = Avalonia.Layout.Orientation.Horizontal, Width = 200, Maximum = 100, ViewportSize = 30, Value = 0, AllowAutoHide = false };
        var window = Shots.Show(new StackPanel { Children = { vertical, horizontal } });
        var vThumb = vertical.GetVisualDescendants().OfType<Thumb>().Single();
        var hThumb = horizontal.GetVisualDescendants().OfType<Thumb>().Single();
        Assert.True(vThumb.Bounds.Top < 20, $"vertical handle at {vThumb.Bounds}");
        Assert.True(hThumb.Bounds.Left < 20, $"horizontal handle at {hThumb.Bounds}");
        window.Close();
    }

    [AvaloniaFact]
    public void Controls_Use_The_Session_Font()
    {
        var button = new Button { Content = "x" };
        var check = new CheckBox { Content = "x" };
        var text = new TextBox { Text = "x" };
        var window = Shots.Show(new StackPanel { Children = { button, check, text } });
        var session = (double)Application.Current!.FindResource("UnionFontSize")!;
        Assert.Equal(session, window.FontSize);
        Assert.Equal(session, button.FontSize);
        Assert.Equal(session, check.FontSize);
        Assert.Equal(session, text.FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Pseudo_Classes_Map_To_States()
    {
        var button = new Button { Content = "x" };
        var window = Shots.Show(button);
        var node = Union.GetNode(button)!;
        Assert.Equal(UnionStates.None, node.States);

        ((IPseudoClasses)button.Classes).Set(":pointerover", true);
        Shots.Settle();
        Assert.Equal(UnionStates.Hovered, node.States);

        ((IPseudoClasses)button.Classes).Set(":pressed", true);
        Shots.Settle();
        Assert.Equal(UnionStates.Hovered | UnionStates.Pressed, node.States);

        button.IsEnabled = false;
        Shots.Settle();
        Assert.True((node.States & UnionStates.Disabled) != 0);

        Assert.NotEqual(Engine.Managed.UnionRgba.Transparent, node.Resolved.Text.Color);
        window.Close();
    }

    [AvaloniaFact]
    public void Classes_Become_Hints_And_Change_The_Result()
    {
        var button = new Button { Content = "x" };
        var window = Shots.Show(button);
        var node = Union.GetNode(button)!;
        var normal = node.Resolved;
        button.Classes.Add("flat");
        Shots.Settle();
        Assert.Contains("flat", node.Hints);
        Assert.NotEqual(normal.Background.Color, node.Resolved.Background.Color);
        window.Close();
    }

    [AvaloniaFact]
    public void Inactive_Window_Adds_The_Hint()
    {
        var button = new Button { Content = "x" };
        var window = Shots.Show(button);
        var root = Union.GetNode(window)!;
        Assert.Equal("applicationwindow", root.Type);
        Assert.Equal(!window.IsActive, root.Hints.Contains("inactive"));
        window.Close();
    }

    [AvaloniaFact]
    public void Part_States_Come_From_The_Templated_Parent()
    {
        var check = new CheckBox { Content = "x", IsChecked = true };
        var window = Shots.Show(check);
        var indicator = check.GetVisualDescendants().OfType<Presenters.UnionBox>().First(b => Union.GetType(b) == "indicator");
        var node = Union.GetNode(indicator)!;
        Assert.Equal(UnionStates.Checked, node.States);
        Assert.EndsWith(":checked > indicator:checked", node.ChainText);
        Assert.NotNull(node.Resolved.Background.Image.Source);
        window.Close();
    }

    [AvaloniaFact]
    public void Style_Id_Switches_A_Subtree()
    {
        var inner = new Button { Content = "x" };
        var scope = new UnionScope { StyleId = "breeze-mobile", Child = inner };
        var outer = new Button { Content = "y" };
        var panel = new StackPanel();
        panel.Children.Add(outer);
        panel.Children.Add(scope);
        var window = Shots.Show(panel);
        Assert.Equal("breeze-mobile", Union.GetStyleId(inner));
        Assert.Null(Union.GetStyleId(outer));
        window.Close();
    }

    [AvaloniaFact]
    public void Managed_Scheme_Follows_The_Variant()
    {
        var button = new Button { Content = "x" };
        var window = Shots.Show(button);
        var light = Union.GetNode(button)!.Resolved.Background.Color;
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        Shots.Settle();
        var dark = Union.GetNode(button)!.Resolved.Background.Color;
        Assert.NotEqual(light, dark);
        Application.Current!.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
        Shots.Settle();
        Assert.Equal(light, Union.GetNode(button)!.Resolved.Background.Color);
        window.Close();
    }

    [AvaloniaFact]
    public void Plain_Content_Control_Shows_Its_Content()
    {
        var plain = new ContentControl { Content = "Plain content control" };
        var window = Shots.Show(plain, 300, 80);
        Assert.NotEmpty(plain.GetVisualChildren());
        window.Close();
    }
}
