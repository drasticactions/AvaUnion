using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaUnion.Controls.Themes;

internal partial class ControlStyles : Styles
{
    public ControlStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
