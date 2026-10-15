using System.Collections.Generic;
using AvaUnion.Gallery.Pages;

namespace AvaUnion.Gallery;

public static class GalleryPages
{
    public static IReadOnlyList<GalleryPage> All { get; } =
    [
        new("Buttons", () => new ButtonsPage()),
        new("Toggles", () => new TogglesPage()),
        new("Text", () => new TextPage()),
        new("Lists & Trees", () => new ListsPage()),
        new("Data Grid", () => new DataGridPage()),
        new("Menus & Popups", () => new MenusPage()),
        new("Ranges", () => new RangesPage()),
        new("Tabs & Pages", () => new TabsPage()),
        new("Calendar & Pickers", () => new PickersPage()),
        new("Notifications & Bars", () => new NotificationsPage()),
        new("Typography & Icons", () => new TypographyPage()),
        new("Scheme", () => new SchemePage()),
    ];
}
