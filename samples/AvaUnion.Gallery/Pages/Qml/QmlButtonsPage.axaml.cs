using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public partial class QmlButtonsPage : UserControl
{
    public QmlButtonsPage()
    {
        AvaloniaXamlLoader.Load(this);
        foreach (var combo in this.GetLogicalDescendants())
        {
            if (combo is ComboBox { Tag: string tag } box)
            {
                var first = tag == "letters" ? "AAAAAAAA" : "I am not enabled!";
                box.ItemsSource = new[] { first, "BBBBBBBBBBB", "CCCCCCCCCCCCCC" };
                if (!box.IsEditable)
                {
                    box.SelectedIndex = 1;
                }
            }
        }
    }
}
