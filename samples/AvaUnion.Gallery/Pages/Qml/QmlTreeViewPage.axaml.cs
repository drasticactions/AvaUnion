using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaUnion.Controls;

namespace AvaUnion.Gallery.Pages;

public sealed record TreeFile(bool Checked, string Size, string Type, string Name, string LastModified, params TreeFile[] Rows)
{
    public string CheckedText => Checked ? "true" : "false";
}

public partial class QmlTreeViewPage : UserControl
{
    private static readonly TreeFile[] s_rows =
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
        var tree = this.FindControl<ColumnTreeView>("Tree")!;
        tree.ChildrenSelector = item => ((TreeFile)item).Rows.Length > 0 ? ((TreeFile)item).Rows : null;
        tree.ItemsSource = s_rows;
        tree.ExpandAll();
    }
}
