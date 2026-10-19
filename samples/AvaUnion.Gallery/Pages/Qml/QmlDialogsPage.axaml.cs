using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaUnion.Controls;

namespace AvaUnion.Gallery.Pages;

public partial class QmlDialogsPage : UserControl
{
    public QmlDialogsPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Button>("ShowDialog")!.Click += async (_, _) =>
        {
            var dialog = new Dialog { Title = "Title", StandardButtons = DialogButtons.Ok | DialogButtons.Cancel };
            await dialog.ShowAsync(this);
        };
    }
}
