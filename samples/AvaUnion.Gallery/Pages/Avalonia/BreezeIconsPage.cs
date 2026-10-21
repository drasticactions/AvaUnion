using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.BreezeIcons;

namespace AvaUnion.Gallery.Pages;

public sealed class BreezeIconsPage : UserControl
{
    private const double CellWidth = 136;

    private static readonly BreezeIconProvider s_provider = new();
    private static readonly string[] s_names = [.. s_provider.Names.Order(StringComparer.Ordinal)];
    private static readonly int[] s_sizes = [16, 22, 24, 32, 48, 64, 128];

    private readonly TextBox _filter = new() { PlaceholderText = "Filter by name", Width = 240 };
    private readonly ComboBox _size = new() { ItemsSource = s_sizes, SelectedIndex = 1 };
    private readonly CheckBox _tint = new() { Content = "Tint with the text color", IsChecked = true };
    private readonly TextBlock _count = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ItemsControl _rows = new() { ItemsPanel = new FuncTemplate<Panel?>(() => new VirtualizingStackPanel()) };
    private readonly ScrollViewer _scroll;
    private string[] _matches = s_names;
    private int _columns;

    public BreezeIconsPage()
    {
        _rows.ItemTemplate = new FuncDataTemplate<IconRow>((row, _) => row is null ? null : Row(row));
        _scroll = new ScrollViewer { Content = _rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };

        var bar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 0, 0, 12) };
        bar.Children.Add(_filter);
        bar.Children.Add(new Label { Content = "Size:", VerticalAlignment = VerticalAlignment.Center });
        bar.Children.Add(_size);
        bar.Children.Add(_tint);
        bar.Children.Add(_count);

        var root = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        root.Children.Add(bar);
        root.Children.Add(_scroll);
        Content = root;

        _filter.TextChanged += (_, _) => Filter();
        _size.SelectionChanged += (_, _) => Refresh();
        _tint.IsCheckedChanged += (_, _) => Refresh();
        _scroll.SizeChanged += (_, _) =>
        {
            if (Columns() != _columns)
            {
                Refresh();
            }
        };
        Filter();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextElement.ForegroundProperty)
        {
            Refresh();
        }
    }

    private void Filter()
    {
        var text = _filter.Text?.Trim();
        _matches = string.IsNullOrEmpty(text) ? s_names : [.. s_names.Where(n => n.Contains(text, StringComparison.OrdinalIgnoreCase))];
        _count.Text = $"{_matches.Length} of {s_names.Length} icons";
        Refresh();
    }

    private int Columns() => Math.Max(1, (int)((_scroll.Bounds.Width > 0 ? _scroll.Bounds.Width : 900) / CellWidth));

    private void Refresh()
    {
        if (_scroll is null)
        {
            return;
        }

        _columns = Columns();
        var size = _size.SelectedItem is int s ? s : 22;
        Color? tint = _tint.IsChecked == true && GetValue(TextElement.ForegroundProperty) is ISolidColorBrush brush ? brush.Color : null;
        var rows = new List<IconRow>();
        for (var i = 0; i < _matches.Length; i += _columns)
        {
            rows.Add(new IconRow(_matches[i..Math.Min(i + _columns, _matches.Length)], size, tint));
        }

        _rows.ItemsSource = rows;
    }

    private static Control Row(IconRow row)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var name in row.Names)
        {
            var cell = new StackPanel { Width = CellWidth, Spacing = 4 };
            cell.Children.Add(new Image
            {
                Width = row.Size,
                Height = row.Size,
                Source = s_provider.GetIcon(name, row.Tint, new Size(row.Size, row.Size), 1.0),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            cell.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(4, 0),
            });
            ToolTip.SetTip(cell, name);
            panel.Children.Add(cell);
        }

        return panel;
    }

    private sealed record IconRow(string[] Names, int Size, Color? Tint);
}
