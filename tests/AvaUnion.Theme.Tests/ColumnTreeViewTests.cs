using System.Collections;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class ColumnTreeViewTests
{
    private sealed record File(string Name, string Size, params File[] Files);

    private static readonly File[] s_files =
    [
        new("Documents", "—", new File("Resume.pdf", "24 KB"), new File("Reports", "2 MB", new File("Q2_Report.docx", "850 KB"))),
        new("Pictures", "—", new File("Vacation.jpg", "3.5 MB")),
    ];

    private static ColumnTreeView Tree() => new()
    {
        Height = 300,
        ItemsSource = s_files,
        ChildrenSelector = item => ((File)item).Files.Length > 0 ? (IEnumerable)((File)item).Files : null,
        Columns =
        {
            new ColumnTreeViewColumn { Header = "Name", Binding = new Binding(nameof(File.Name)) },
            new ColumnTreeViewColumn { Header = "Size", Binding = new Binding(nameof(File.Size)) },
        },
    };

    [AvaloniaFact]
    public void Rows_Expand_With_Aligned_Columns()
    {
        var tree = Tree();
        var window = Shots.Show(tree, 400, 340);
        Assert.Equal("scrollview", Union.GetNode(tree)!.Type);
        Assert.Equal(new[] { "Documents", "Pictures" }, tree.VisibleItems.Select(i => ((File)i).Name));

        tree.ExpandAll();
        Shots.Settle();
        Assert.Equal(6, tree.VisibleItems.Count);
        var rows = tree.GetVisualDescendants().OfType<ColumnTreeViewRow>().OrderBy(r => r.Index).ToList();
        Assert.Contains("treeviewdelegate.hover-enabled.expanded", Union.GetNode(rows[0])!.ChainText);
        Assert.Contains("alternating-colors", Union.GetNode(rows[1])!.ChainText);
        Assert.Equal(2, rows[3].Level);

        var sizes = rows.Select(r => r.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text is "—" or "24 KB" or "2 MB" or "850 KB" or "3.5 MB")).ToList();
        var x = sizes[0].TranslatePoint(default, tree)!.Value.X;
        Assert.All(sizes, s => Assert.Equal(x, s.TranslatePoint(default, tree)!.Value.X, 0.5));
        var header = tree.GetVisualDescendants().OfType<ColumnTreeViewHeader>().ToList();
        Assert.Equal(2, header.Count);
        Assert.Contains("headerviewdelegate.horizontal", Union.GetNode(header[1])!.ChainText);
        Assert.Equal(x, header[1].TranslatePoint(default, tree)!.Value.X, 8.0);
        Shots.Capture(window, "columntreeview");
        Assert.Equal(AutomationControlType.Tree, ControlAutomationPeer.CreatePeerForElement(tree).GetAutomationControlType());
        window.Close();
    }

    [AvaloniaFact]
    public void Pointer_And_Keys_Select_And_Expand()
    {
        var tree = Tree();
        var window = Shots.Show(tree, 400, 340);
        var first = tree.GetVisualDescendants().OfType<ColumnTreeViewRow>().First();
        var point = first.TranslatePoint(new Point(first.Bounds.Width - 20, 5), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Assert.Same(s_files[0], tree.SelectedItem);
        Assert.Contains("selected", Union.GetNode(first)!.ChainText);

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Assert.True(tree.IsExpanded(s_files[0]));
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
        Assert.Equal("Resume.pdf", ((File)tree.SelectedItem!).Name);
        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.Same(s_files[0], tree.SelectedItem);
        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);
        Assert.False(tree.IsExpanded(s_files[0]));

        Shots.Settle();
        var row = tree.GetVisualDescendants().OfType<ColumnTreeViewRow>().First();
        var expand = ControlAutomationPeer.CreatePeerForElement(row).GetProvider<IExpandCollapseProvider>()!;
        expand.Expand();
        Assert.True(tree.IsExpanded(s_files[0]));
        window.Close();
    }
}
