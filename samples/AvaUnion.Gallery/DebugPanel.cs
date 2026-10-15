#if DEBUG
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaUnion.Engine;

namespace AvaUnion.Gallery;

/// <summary>
/// Ctrl+Shift+U shows the Union element under the pointer: its chain as
/// CSS-like text and every set leaf of its resolved style.
/// </summary>
internal static class DebugPanel
{
    public static void Attach(UserControl view)
    {
        var text = new SelectableTextBlock { FontFamily = new FontFamily("monospace"), FontSize = 11 };
        var overlay = new Border
        {
            Child = new ScrollViewer { Content = text, MaxHeight = 420 },
            Padding = new Thickness(8),
            Width = 460,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(12),
            CornerRadius = new CornerRadius(5),
            IsHitTestVisible = false,
            IsVisible = false,
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("Union.Active.Tooltip.Background.NormalBrush"),
            [!TextBlock.ForegroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("Union.Active.Tooltip.Foreground.TextBrush"),
        };

        view.AttachedToVisualTree += (_, _) =>
        {
            var layer = AdornerLayer.GetAdornerLayer(view);
            layer?.Children.Add(overlay);
        };

        view.KeyDown += (_, e) =>
        {
            if (e.Key == Key.U && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                overlay.IsVisible = !overlay.IsVisible;
                e.Handled = true;
            }
        };

        view.PointerMoved += (_, e) =>
        {
            if (!overlay.IsVisible || TopLevel.GetTopLevel(view) is not { } root)
            {
                return;
            }

            var hit = root.InputHitTest(e.GetPosition(root)) as Visual;
            var node = hit is null ? null : FindNode(hit);
            text.Text = node is null ? "(no Union element)" : Describe(node);
        };
    }

    private static Elements.ElementNode? FindNode(Visual visual)
    {
        for (Visual? v = visual; v is not null; v = v.GetVisualParent())
        {
            if (Union.GetNode(v) is { } node)
            {
                return node;
            }
        }

        return null;
    }

    private static string Describe(Elements.ElementNode node)
    {
        var sb = new StringBuilder();
        sb.AppendLine(node.ChainText);
        sb.AppendLine();
        var engine = Elements.UnionEnvironment.Engine;
        var id = App.Instance.Theme.StyleId;
        var metadata = (id is null ? null : engine.GetStyle(id))?.Metadata ?? engine.DefaultStyle.Metadata;
        sb.Append(metadata.Name ?? metadata.Id).Append(" · ").Append(metadata.Id).Append(" · ").Append(metadata.Origin);
        if (metadata.Version is not null)
        {
            sb.Append(" · ").Append(metadata.Version);
        }

        if (metadata.License is not null)
        {
            sb.Append(" · ").Append(metadata.License);
        }

        sb.AppendLine();
        sb.AppendLine();
        var style = node.Resolved;
        for (var i = 0; i < UnionLeafInfo.Count; i++)
        {
            if (style.GetLeaf(i) is { } value)
            {
                sb.Append(UnionLeafInfo.Names[i].PadRight(34)).AppendLine(value.ToString());
            }
        }

        return sb.ToString();
    }
}
#endif
