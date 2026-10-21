using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Engine.Native;

namespace AvaUnion.Gallery;

public sealed class MainView : UserControl
{
    private readonly ContentControl _content = new() { Margin = new Thickness(16) };
    private readonly TextBlock _engine = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _variant = new() { VerticalAlignment = VerticalAlignment.Center };

    public MainView()
    {
        var theme = App.Instance.Theme;
        var engine = theme.EngineInstance;

        var styles = new List<StyleChoice> { new(null, "Session") };
        styles.AddRange(engine.Styles.Select(s => new StyleChoice(s.Id, (s.Name ?? s.Id) + (s.Origin == UnionStyleOrigin.Application ? " (app)" : string.Empty))));
        var style = new ComboBox { ItemsSource = styles, SelectedIndex = 0, MinWidth = 150 };
        style.SelectionChanged += (_, _) =>
        {
            theme.StyleId = (style.SelectedItem as StyleChoice)?.Id;
            Update();
        };

        var schemes = new List<string> { "Session", "BreezeLight", "BreezeDark" };
        if (engine.Kind == UnionEngineKind.Native)
        {
            schemes.AddRange(InstalledSchemes().Where(s => !schemes.Contains(s)));
        }

        var scheme = new ComboBox { ItemsSource = schemes, SelectedIndex = 0, MinWidth = 170 };
        scheme.SelectionChanged += (_, _) =>
        {
            var name = scheme.SelectedItem as string;
            theme.ColorScheme = name == "Session" ? null : name;
            Update();
        };

        var header = new DockPanel { Margin = new Thickness(12, 8), LastChildFill = true };
        var switches = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        switches.Children.Add(new Label { Content = "Style:" });
        switches.Children.Add(style);
        switches.Children.Add(new Label { Content = "Color scheme:" });
        switches.Children.Add(scheme);
        switches.Children.Add(_variant);
        DockPanel.SetDock(switches, Dock.Right);
        header.Children.Add(switches);
        header.Children.Add(_engine);

        var items = new List<ListBoxItem>();
        foreach (var section in GalleryPages.Sections)
        {
            items.Add(new ListBoxItem
            {
                Content = new TextBlock { Text = section.Title, FontWeight = FontWeight.Bold },
                Focusable = false,
                IsHitTestVisible = false,
                Margin = new Thickness(0, items.Count == 0 ? 0 : 8, 0, 0),
            });
            items.AddRange(section.Pages.Select(page => new ListBoxItem { Content = page.Title, Tag = page }));
        }

        var pages = new ListBox
        {
            ItemsSource = items,
            SelectedIndex = 1,
            Width = 210,
            Margin = new Thickness(12, 0, 0, 12),
        };
        pages.SelectionChanged += (_, _) =>
        {
            if (pages.SelectedItem is ListBoxItem { Tag: GalleryPage page })
            {
                _content.Content = page.Host();
            }
        };
        _content.Content = GalleryPages.Sections[0].Pages[0].Host();

        var root = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(pages, Dock.Left);
        root.Children.Add(header);
        root.Children.Add(new Separator());
        DockPanel.SetDock(root.Children[^1], Dock.Top);
        root.Children.Add(pages);
        root.Children.Add(_content);
        Content = root;

        UnionEnvironment.Changed += _ => Update();
        ActualThemeVariantChanged += (_, _) => Update();
        Update();
#if DEBUG
        DebugPanel.Attach(this);
#endif
    }

    private void Update()
    {
        var engine = UnionEnvironment.Engine;
        var description = engine.Description;
        var font = engine.Fonts.General;
        var platform = engine.Platform;
        var family = this.TryFindResource("UnionFontFamily", out var resource) && resource is FontFamily f ? f : new FontFamily(font.Family);
        var drawn = FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out var typeface) ? typeface.FamilyName : null;
        var fontText = drawn ?? font.Family;
        description += string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $" · {fontText} {font.SizeInDips:0.#} px · icons {platform.IconTheme ?? "none"} · animation {platform.AnimationSpeed:0.##}×");
        _engine.Text = description;
        _variant.Text = $"Variant: {ActualThemeVariant}";
    }

    private static IEnumerable<string> InstalledSchemes()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dirs = new[] { "/usr/share/color-schemes", Path.Combine(home, ".local/share/color-schemes") };
        return dirs.Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.colors"))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Distinct()
            .Order();
    }
}
