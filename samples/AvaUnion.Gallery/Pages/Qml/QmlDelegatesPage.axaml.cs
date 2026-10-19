using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaUnion.Controls;

namespace AvaUnion.Gallery.Pages;

public partial class QmlDelegatesPage : UserControl
{
    public QmlDelegatesPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Tumbler>("Hours")!.ItemsSource = Enumerable.Range(0, 12).ToList();
        this.FindControl<Tumbler>("Minutes")!.ItemsSource = Enumerable.Range(0, 60).ToList();
        this.FindControl<Tumbler>("AmPm")!.ItemsSource = new[] { "AM", "PM" };
    }
}
