using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public partial class NotificationsPage : UserControl
{
    private WindowNotificationManager? _manager;

    public NotificationsPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<AutoCompleteBox>("Complete")!.ItemsSource = new[] { "Alabama", "Alaska", "Arizona", "Arkansas", "California", "Colorado" };
        this.FindControl<Button>("ShowToast")!.Click += (_, _) =>
        {
            _manager ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) { Position = NotificationPosition.BottomRight, MaxItems = 3 };
            _manager.Show(new Notification("AvaUnion", "This notification is an inline message.", NotificationType.Information));
        };
    }
}
