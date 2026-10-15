using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

using DataGrid = Avalonia.Controls.DataGrid;

public class DataGridShotTests
{
    public sealed record Person(string Name, string City, int Age);

    private static DataGrid Grid(bool rowHeaders = false, bool groups = false)
    {
        List<Person> people =
        [
            new("Ada", "London", 36), new("Grace", "New York", 85), new("Linus", "Helsinki", 54),
            new("Margaret", "Boston", 87), new("Ken", "New York", 81), new("Barbara", "Boston", 85),
        ];
        var view = new DataGridCollectionView(people);
        if (groups)
        {
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Person.City)));
        }

        var grid = new DataGrid
        {
            ItemsSource = view,
            Width = 380,
            Height = 260,
            HeadersVisibility = rowHeaders ? DataGridHeadersVisibility.All : DataGridHeadersVisibility.Column,
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(Person.Name)), Width = new DataGridLength(140) });
        grid.Columns.Add(new DataGridTextColumn { Header = "City", Binding = new Binding(nameof(Person.City)), Width = new DataGridLength(120) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Age", Binding = new Binding(nameof(Person.Age)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        return grid;
    }

    [AvaloniaFact]
    public void Grid_Rows_And_Headers_Are_Table_Elements()
    {
        var grid = Grid(rowHeaders: true);
        var window = Shots.Show(grid, 420, 300);
        grid.SelectedIndex = 1;
        grid.Columns[0].Sort(System.ComponentModel.ListSortDirection.Ascending);
        Shots.Settle();
        Shots.Capture(window, "datagrid");

        var gridNode = Union.GetNode(grid)!;
        Assert.Equal("tableview", gridNode.Type);
        Assert.True(gridNode.Resolved.Background.Color is { A: > 0 }, "the framed table view has a background");

        var rows = grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(r => r.Index).ToList();
        Assert.True(rows.Count >= 6);
        var selected = rows.Single(r => r.IsSelected);
        var selectedNode = Union.GetNode(selected)!;
        Assert.Equal("tableviewdelegate", selectedNode.Type);
        Assert.Contains("selected", selectedNode.Hints);
        Assert.NotEqual(Union.GetNode(rows.First(r => !r.IsSelected && r.Index % 2 == 0))!.Resolved.Background.Color, selectedNode.Resolved.Background.Color);
        Assert.Contains("alternating-colors", Union.GetNode(rows.First(r => r.Index % 2 == 1 && !r.IsSelected))!.Hints);

        var headers = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Where(h => h.Name != "PART_TopLeftCornerHeader").ToList();
        var sorted = Union.GetNode(headers.Single(h => (string?)h.Content == "Name"))!;
        Assert.Equal("headerviewdelegate", sorted.Type);
        Assert.Contains("horizontal", sorted.Hints);
        Assert.Contains("sort-ascending", sorted.Hints);
        var indicator = headers.Single(h => (string?)h.Content == "Name").GetVisualDescendants().OfType<UnionIcon>().Single(i => i.Name == "PART_SortIndicator");
        Assert.Equal("arrow-up-symbolic", Union.GetNode(indicator)!.Resolved.Icon.Name);

        var rowHeader = Union.GetNode(grid.GetVisualDescendants().OfType<DataGridRowHeader>().First())!;
        Assert.Equal("headerviewdelegate", rowHeader.Type);
        Assert.Contains("vertical", rowHeader.Hints);
    }

    [AvaloniaFact]
    public void Groups_Are_Tree_Delegates_With_An_Indicator()
    {
        var grid = Grid(groups: true);
        var window = Shots.Show(grid, 420, 300);
        Shots.Capture(window, "datagrid-groups");
        var group = grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>().First();
        var node = Union.GetNode(group)!;
        Assert.Equal("treeviewdelegate", node.Type);
        Assert.Contains("expanded", node.Hints);
        var expander = group.GetVisualDescendants().OfType<ToggleButton>().Single();
        Assert.True(Union.GetNode(expander)!.ChainText.EndsWith("treeviewdelegate.expanded > indicator"), Union.GetNode(expander)!.ChainText);
    }
}
