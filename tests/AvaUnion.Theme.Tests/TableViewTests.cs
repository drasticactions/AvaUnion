using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TableViewTests
{
    [AvaloniaFact]
    public void Odd_Rows_Alternate_Unless_Turned_Off()
    {
        var table = new TableView { Height = 200, ItemsSource = new[] { "a", "b", "c", "d" } };
        table.Columns.Add(new TableViewColumn { Header = "Text" });
        var window = Shots.Show(table, 300, 260);
        var rows = Enumerable.Range(0, 4).Select(i => (TableViewRow)table.ContainerFromIndex(i)!).ToList();
        Assert.Equal([false, true, false, true], rows.Select(r => Union.GetNode(r)!.Hints.Contains("alternating-colors")));
        table.Classes.Add("no-alternating-colors");
        Shots.Settle();
        Assert.DoesNotContain(rows, r => Union.GetNode(r)!.Hints.Contains("alternating-colors"));
        window.Close();
    }
}
