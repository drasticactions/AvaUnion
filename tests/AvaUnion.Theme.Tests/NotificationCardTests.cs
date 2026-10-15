using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class NotificationCardTests
{
    [AvaloniaFact]
    public void Error_Card_Has_Its_Own_Background()
    {
        var error = new NotificationCard { NotificationType = NotificationType.Error, Content = "Could not open the file." };
        var info = new NotificationCard { NotificationType = NotificationType.Information, Content = "The backup finished." };
        var window = Shots.Show(new StackPanel { Spacing = 10, Children = { info, error } }, 420, 200);
        Shots.Capture(window, "notifications");

        var errorNode = Union.GetNode(error)!;
        Assert.Equal("inlinemessage", errorNode.Type);
        Assert.True(errorNode.ChainText.Contains("[type=error]"), errorNode.ChainText);
        Assert.NotEqual(Union.GetNode(info)!.Resolved.Background.Color, errorNode.Resolved.Background.Color);
        window.Close();
    }
}
