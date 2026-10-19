using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public sealed record TableAnimal(string Name, string Color, string Testing);

public partial class QmlTablesPage : UserControl
{
    public QmlTablesPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TableView>("Table")!.ItemsSource = new TableAnimal[]
        {
            new("cat", "black", "A"),
            new("dog", "brownnnnnnnnnnnnnnnnnnnnnnnnnn", "B"),
            new("bird", "white", "CCCCCCCCCCCC"),
            new("lizard", "green", "DDDDDD"),
            new("fishiesssssssss", "blue", "EEEEE"),
        };
    }
}
