using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Controls.Themes;

internal partial class ControlThemes : ResourceDictionary
{
    public ControlThemes()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
