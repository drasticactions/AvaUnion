using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using AvaUnion.Controls.Primitives;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TableRowHeadersTests
{
    [AvaloniaFact]
    public void Rows_Show_Their_Number()
    {
        var table = new TableView { Height = 200, ItemsSource = new[] { "cat", "dog", "bird" } };
        table.Columns.Add(new TableViewColumn { Header = "1", Width = new GridLength(100) });
        TableRowHeaders.SetIsVisible(table, true);
        var window = Shots.Show(table, 300, 260);
        var headers = table.GetVisualDescendants().OfType<TableRowHeader>().Where(h => h.IsHitTestVisible).ToList();
        Assert.Equal(new[] { "1", "2", "3" }, headers.Select(h => (string)h.Content!));
        Assert.Contains("headerviewdelegate.vertical", Union.GetNode(headers[0])!.ChainText);
        Assert.Equal(32, headers[0].Bounds.Width);

        var column = table.GetVisualDescendants().OfType<TableViewColumnHeader>().Single();
        Assert.Equal(headers[0].TranslatePoint(new Point(headers[0].Bounds.Width, 0), table)!.Value.X, column.TranslatePoint(default, table)!.Value.X, 1.0);
        Shots.Capture(window, "tablerowheaders");

        table.IsEnabled = false;
        Shots.Settle();
        Assert.True(Union.GetNode(headers[0])!.States.HasFlag(AvaUnion.Engine.UnionStates.Disabled));
        Assert.Equal(Union.GetNode(column)!.Resolved.Text.Color, Union.GetNode(headers[0])!.Resolved.Text.Color);
        window.Close();
    }
}
