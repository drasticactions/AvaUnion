using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaUnion.Controls;

namespace AvaUnion.Gallery.Pages;

public partial class QmlTextPage : UserControl
{
    public QmlTextPage()
    {
        AvaloniaXamlLoader.Load(this);
        var password = this.FindControl<ActionTextField>("Password")!;
        var reveal = password.RightActions[0];
        reveal.Triggered += (_, _) =>
        {
            password.RevealPassword = !password.RevealPassword;
            reveal.IconName = password.RevealPassword ? "password-show-off" : "password-show-on";
            reveal.Text = password.RevealPassword ? "Hide Password" : "Show Password";
        };

        var actions = this.FindControl<ActionTextField>("Actions")!;
        var confirm = actions.RightActions[1];
        actions.TextChanged += (_, _) => confirm.IsVisible = !string.IsNullOrEmpty(actions.Text);
    }
}
