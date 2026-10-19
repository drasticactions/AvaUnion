using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public sealed record Dragon(string Type, string Wings, string FrontLegs, string HindLegs);

public partial class WidgetsListPage : UserControl
{
    private static readonly Dragon[] s_dragons =
    [
        new("Western Dragon", "Yes", "Yes", "Yes"),
        new("Eastern Dragon", "No", "Yes", "Yes"),
        new("Wyvern", "Yes", "No", "Yes"),
        new("Wyrm", "No", "No", "No"),
        new("Lindworm", "No", "Yes", "No"),
        new("Amphiptere", "Yes", "No", "No"),
    ];

    public WidgetsListPage()
    {
        AvaloniaXamlLoader.Load(this);
        foreach (var name in new[] { "Table", "TableNoGrid" })
        {
            var grid = this.FindControl<Avalonia.Controls.DataGrid>(name)!;
            grid.ItemsSource = s_dragons;
            grid.LoadingRow += (_, e) => e.Row.Header = e.Row.Index + 1;
        }
    }
}
