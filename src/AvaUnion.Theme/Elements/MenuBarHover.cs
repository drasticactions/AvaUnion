using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaUnion.Elements;

internal static class MenuBarHover
{
    private static readonly AttachedProperty<bool> BrowsingProperty =
        AvaloniaProperty.RegisterAttached<Menu, Menu, bool>("Browsing");

    public static void Register()
    {
        MenuItem.SubmenuOpenedEvent.AddClassHandler<Menu>(OnSubmenuOpened);
        MenuBase.ClosedEvent.AddClassHandler<Menu>((menu, _) => menu.SetValue(BrowsingProperty, false));
        MenuItem.PointerEnteredItemEvent.AddClassHandler<Menu>(OnPointerEnteredItem);
    }

    private static void OnSubmenuOpened(Menu menu, RoutedEventArgs e)
    {
        if (e.Source is MenuItem { IsTopLevel: true })
        {
            menu.SetValue(BrowsingProperty, true);
        }
    }

    private static void OnPointerEnteredItem(Menu menu, RoutedEventArgs e)
    {
        if (e.Source is not MenuItem { IsTopLevel: true, HasSubMenu: true, IsSubMenuOpen: false } item
            || !menu.IsOpen || !menu.GetValue(BrowsingProperty)
            || menu.Items.OfType<MenuItem>().Any(sibling => sibling.IsSubMenuOpen))
        {
            return;
        }

        item.Open();
    }
}
