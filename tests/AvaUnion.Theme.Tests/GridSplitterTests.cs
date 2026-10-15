using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class GridSplitterTests
{
    [AvaloniaFact]
    public void Handle_Is_A_Thin_Split_View_Handle()
    {
        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("80,Auto,80"), Height = 40 };
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);
        var window = Shots.Show(grid, 200, 80);
        Shots.Capture(window, "splitter");

        var handle = splitter.GetVisualDescendants().OfType<Presenters.UnionBox>().Single();
        var handleNode = Union.GetNode(handle)!;
        Assert.True(handleNode.ChainText.Contains("splitview.horizontal > handle"), handleNode.ChainText);
        Assert.Equal(1, handle.Bounds.Width);
        window.Close();
    }
}
