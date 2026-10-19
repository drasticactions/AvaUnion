using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaUnion.Gallery.Pages;

public partial class WidgetsTextPage : UserControl
{
    public WidgetsTextPage()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<TextBox>("TextEdit")!.Text =
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\n"
            + "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.\n"
            + "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.\n"
            + "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.\n"
            + "\n\n\n\n\n\n\n\n\n\n\n\n"
            + "Hello";
        foreach (var name in new[] { "Combo", "ComboEditable", "ComboNoFrame", "ComboNoFrameEditable" })
        {
            this.FindControl<ComboBox>(name)!.ItemsSource = new object[]
            {
                "Red",
                "Yellow",
                "Kitchen utensil",
                new IconLabel { Icon = "document-save", Text = "One day I went to the park. The park had many pine trees." },
            };
        }
    }
}
