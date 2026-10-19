using System.Collections.Generic;
using System.Linq;
using AvaUnion.Gallery.Pages;

namespace AvaUnion.Gallery;

public static class GalleryPages
{
    public static IReadOnlyList<GallerySection> Sections { get; } =
    [
        new("Union QML gallery", "qml",
        [
            new("Buttons", () => new QmlButtonsPage()),
            new("Text", () => new QmlTextPage()),
            new("Checkable Controls", () => new QmlCheckablePage()),
            new("Delegates", () => new QmlDelegatesPage()),
            new("Progress Bar & Slider", () => new QmlProgressSliderPage()),
            new("Dialogs", () => new QmlDialogsPage()),
            new("Menus", () => new QmlMenusPage()),
            new("Tables", () => new QmlTablesPage()),
            new("Tabs", () => new QmlTabsPage()),
            new("TreeView", () => new QmlTreeViewPage()),
            new("SwipeView", () => new QmlSwipeViewPage()),
            new("Calendar", () => new QmlCalendarPage()),
        ]),
        new("Union widgets gallery", "widgets",
        [
            new("Buttons", () => new WidgetsButtonsPage()),
            new("Text", () => new WidgetsTextPage()),
            new("Numeric Editors", () => new WidgetsNumberPage()),
            new("Checkable", () => new WidgetsCheckablePage()),
            new("Lists", () => new WidgetsListPage()),
        ]),
        new("Avalonia", "avalonia",
        [
            new("Buttons", () => new ButtonsPage()),
            new("Data Grid", () => new DataGridPage()),
            new("Pickers", () => new PickersPage()),
            new("Containers", () => new ContainersPage()),
            new("Popups", () => new PopupsPage()),
            new("Notifications & Bars", () => new NotificationsPage()),
            new("Typography & Icons", () => new TypographyPage()),
            new("Scheme", () => new SchemePage()),
        ]),
    ];

    public static IEnumerable<(GallerySection Section, GalleryPage Page)> All =>
        Sections.SelectMany(s => s.Pages.Select(p => (s, p)));
}

public sealed record GallerySection(string Title, string Key, IReadOnlyList<GalleryPage> Pages);
