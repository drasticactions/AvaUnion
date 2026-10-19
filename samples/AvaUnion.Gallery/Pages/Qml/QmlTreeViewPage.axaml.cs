using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public partial class QmlTreeViewPage : UserControl
{
    private sealed record Row(bool Checked, string Size, string Type, string Name, string LastModified, params Row[] Rows);

    private static readonly Row[] s_rows =
    [
        new(false, "—", "folder", "Documents", "2025-07-01",
            new(true, "24 KB", "file", "Resume.pdf", "2025-06-20"),
            new(false, "2 MB", "folder", "Reports", "2025-06-10",
                new(true, "850 KB", "file", "Q2_Report.docx", "2025-06-15"),
                new(false, "1.2 MB", "file", "Q3_Plan.xlsx", "2025-06-18"))),
        new(false, "—", "folder", "Pictures", "2025-05-30",
            new(true, "3.5 MB", "file", "Vacation.jpg", "2025-05-15"),
            new(false, "2.1 MB", "file", "Family.png", "2025-05-20")),
    ];

    public QmlTreeViewPage()
    {
        AvaloniaXamlLoader.Load(this);
        var tree = this.FindControl<TreeView>("Tree")!;
        Grid.SetIsSharedSizeScope(tree, true);
        foreach (var row in s_rows)
        {
            tree.Items.Add(Item(row));
        }
    }

    private static TreeViewItem Item(Row row)
    {
        var cells = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        cells.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        string[] texts = [row.Checked ? "true" : "false", row.Size, row.Type, row.Name, row.LastModified];
        for (var i = 0; i < texts.Length; i++)
        {
            if (i > 0)
            {
                cells.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "Column" + i });
            }

            var text = new TextBlock { Text = texts[i], Margin = new Avalonia.Thickness(0, 0, 8, 0) };
            Grid.SetColumn(text, i);
            cells.Children.Add(text);
        }

        var item = new TreeViewItem { Header = cells, IsExpanded = true };
        foreach (var child in row.Rows)
        {
            item.Items.Add(Item(child));
        }

        return item;
    }
}
