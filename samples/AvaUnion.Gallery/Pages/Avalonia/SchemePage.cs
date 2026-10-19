using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaUnion.Engine;

namespace AvaUnion.Gallery.Pages;

public sealed class SchemePage : UserControl
{
    public SchemePage()
    {
        var root = new StackPanel { Spacing = 12 };
        foreach (var group in System.Enum.GetValues<ColorGroup>())
        {
            root.Children.Add(new Label { Content = $"{group} group", FontWeight = FontWeight.Bold });
            foreach (var set in System.Enum.GetValues<ColorSet>())
            {
                var row = new WrapPanel { ItemSpacing = 4, LineSpacing = 4 };
                row.Children.Add(new TextBlock { Text = set.ToString(), Width = 110, VerticalAlignment = VerticalAlignment.Center });
                foreach (var kind in System.Enum.GetValues<ColorRoleKind>())
                {
                    var roles = SchemeColorKey.Roles(kind);
                    for (var role = 0; role < roles.Length; role++)
                    {
                        var key = SchemeColorKey.ResourceKey(group, set, kind, role);
                        var swatch = new Border
                        {
                            Width = 26,
                            Height = 26,
                            CornerRadius = new CornerRadius(4),
                            BorderThickness = new Thickness(1),
                            [!Border.BorderBrushProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("Union.Active.Window.Decoration.FrameBrush"),
                            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(key + "Brush"),
                        };
                        ToolTip.SetTip(swatch, key);
                        row.Children.Add(swatch);
                    }
                }

                root.Children.Add(row);
            }
        }

        Content = root;
    }
}
