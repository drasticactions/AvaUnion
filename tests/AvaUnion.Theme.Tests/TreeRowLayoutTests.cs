using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TreeRowLayoutTests
{
    private sealed record Person(string Name, string City);

    private sealed class ByCity : DataGridGroupDescription
    {
        public override object GroupKeyFromItem(object item, int level, System.Globalization.CultureInfo culture) => ((Person)item).City;
    }

    private static double X(Visual v, Visual root) => v.TranslatePoint(default, root)!.Value.X;

    [AvaloniaFact]
    public void Rows_Alternate_After_A_First_Expansion()
    {
        var archive = new TreeViewItem { Header = "Archive", Items = { new TreeViewItem { Header = "old.txt" } } };
        var documents = new TreeViewItem { Header = "Documents", IsExpanded = true, Items = { new TreeViewItem { Header = "report.odt" }, archive } };
        var music = new TreeViewItem { Header = "Music" };
        var pictures = new TreeViewItem { Header = "Pictures" };
        var tree = new TreeView { Width = 260, Height = 260, Items = { documents, music, pictures } };
        var window = Shots.Show(tree, 400, 340);

        archive.IsExpanded = true;
        Shots.Settle();

        string Rows() => string.Join(" ", tree.GetVisualDescendants()
            .OfType<Control>()
            .Where(c => c.TemplatedParent is TreeViewItem && Union.GetNode(c) is { Type: "treeviewdelegate" })
            .Select(c => (((TreeViewItem)c.TemplatedParent!).Header as string) + (Union.GetNode(c)!.Hints.Contains("alternating-colors") ? "*" : string.Empty)));
        Assert.Equal("Documents report.odt* Archive old.txt* Music Pictures*", Rows());
        window.Close();
    }

    [AvaloniaFact]
    public void Tree_Row_Places_Indicator_At_The_Edge()
    {
        var leaf = new TreeViewItem { Header = "report.odt" };
        var node = new TreeViewItem { Header = "Documents", IsExpanded = true, Items = { leaf } };
        var tree = new TreeView { Width = 260, Height = 180, Items = { node } };
        var window = Shots.Show(tree, 400, 300);

        foreach (var (item, indent, hasIndicator) in new[] { (node, 0.0, true), (leaf, 24.0, false) })
        {
            var parts = item.GetVisualDescendants().OfType<Control>().ToList();
            var row = parts.First(c => c.Name == "PART_LayoutRoot");
            var header = parts.First(c => c.Name == "PART_HeaderPresenter");
            var origin = X(row, tree);
            Assert.Equal(System.Math.Max(24, header.Bounds.Height + 8), row.Bounds.Height, 1);
            Assert.Equal(origin + indent + 4 + (hasIndicator ? 24 : 0), X(header, tree));
            if (hasIndicator)
            {
                var indicator = parts.First(c => c.Name == "PART_ExpandCollapseChevron");
                Assert.Equal(origin + indent, X(indicator, tree));
                Assert.Equal(24, indicator.Bounds.Width);
            }
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Group_Header_Is_Laid_Out_As_A_Tree_Row()
    {
        var view = new DataGridCollectionView(new[] { new Person("Ada Lovelace", "London") });
        view.GroupDescriptions.Add(new ByCity());
        var grid = new Avalonia.Controls.DataGrid { ItemsSource = view, Width = 400, Height = 200, AutoGenerateColumns = true };
        var tree = new TreeView { Items = { new TreeViewItem { Header = "London", Items = { new TreeViewItem { Header = "x" } } } } };
        var window = Shots.Show(new StackPanel { Children = { grid, tree } }, 500, 400);
        var treeRow = tree.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_LayoutRoot");
        var header = grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>().First();
        var parts = header.GetVisualDescendants().OfType<Control>().ToList();
        var expander = parts.First(c => c.Name == "PART_ExpanderButton");
        var key = parts.OfType<TextBlock>().First(t => t.Text == "London");

        Assert.Equal(treeRow.Bounds.Height, header.Bounds.Height, 1);
        Assert.Equal(24, expander.Bounds.Width);
        Assert.Equal(X(expander, grid) + 24 + 4, X(key, grid));
        Assert.Equal(Avalonia.Media.FontWeight.Normal, key.FontWeight);
        window.Close();
    }

    [AvaloniaFact]
    public void Selected_Row_Is_A_Selected_Tree_Delegate()
    {
        var root = new TreeViewItem { Header = "Documents", IsExpanded = true };
        root.Items.Add(new TreeViewItem { Header = "report.odt" });
        var nested = new TreeViewItem { Header = "Archive" };
        nested.Items.Add(new TreeViewItem { Header = "old.txt" });
        root.Items.Add(nested);
        var tree = new TreeView { Height = 140, Width = 260 };
        tree.Items.Add(root);
        tree.Items.Add(new TreeViewItem { Header = "Music" });
        tree.SelectedItem = root.Items[0];
        var window = Shots.Show(tree, 300, 180);
        Shots.Capture(window, "tree");

        var rows = tree.GetVisualDescendants().Where(v => Union.GetType((StyledElement)v) == "treeviewdelegate").ToList();
        Assert.True(rows.Count >= 4);
        var selected = rows.Select(r => Union.GetNode((StyledElement)r)!).Single(n => n.Hints.Contains("selected"));
        Assert.EndsWith("scrollview.framed-background[colorset=view] > treeviewdelegate.hover-enabled.alternating-colors.selected", selected.ChainText);
        window.Close();
    }
}
