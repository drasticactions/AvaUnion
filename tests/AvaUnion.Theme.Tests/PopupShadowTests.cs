using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System.Linq;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class PopupShadowTests
{
    [AvaloniaTheory]
    [InlineData(40, false)]
    [InlineData(250, true)]
    public void Menus_Have_Room_For_Their_Shadow_And_Stay_In_Place(double top, bool flips)
    {
        var target = new Border { Width = 100, Height = 30, Margin = new Thickness(60, top, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var menu = new ContextMenu { Placement = PlacementMode.BottomEdgeAlignedLeft, Items = { new MenuItem { Header = "Cut" }, new MenuItem { Header = "Copy" } } };
        target.ContextMenu = menu;
        var window = Shots.Show(new Panel { Children = { target } }, 400, 300);
        menu.Open(target);
        Shots.Settle();
        Shots.Capture(window, flips ? "popup-shadow-flipped" : "popup-shadow");

        var shadow = Union.GetStyle(menu)!.Shadow;
        Assert.True(shadow.Blur > 0);
        var extents = Elements.PopupShadowSpace.Extents(Union.GetStyle(menu));
        Assert.Equal(new Thickness(extents.Left, 0, extents.Right, extents.Bottom), menu.Margin);
        Assert.True(menu.Margin.Bottom > 0);

        var origin = menu.TranslatePoint(default, window)!.Value;
        var targetOrigin = target.TranslatePoint(default, window)!.Value;
        Assert.Equal(targetOrigin.X, origin.X, 1);
        if (flips)
        {
            Assert.Equal(targetOrigin.Y, origin.Y + menu.Bounds.Height, 1);
        }
        else
        {
            Assert.Equal(targetOrigin.Y + target.Bounds.Height, origin.Y, 1);
        }

        menu.Close();
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("button")]
    [InlineData("split")]
    [InlineData("placed")]
    public void Button_Menus_Open_At_The_Left_Edge(string kind)
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "First item of a long menu" }, new MenuItem { Header = "Second" } } };
        if (kind == "placed")
        {
            flyout.Placement = PlacementMode.BottomEdgeAlignedRight;
        }

        ContentControl button = kind == "split" ? new SplitButton { Content = "Split", Flyout = flyout } : new Button { Content = "Menu", Flyout = flyout };
        button.HorizontalAlignment = HorizontalAlignment.Left;
        button.VerticalAlignment = VerticalAlignment.Top;
        button.Margin = new Thickness(kind == "placed" ? 340 : 120, 40, 0, 0);
        var window = Shots.Show(new Panel { Children = { button } }, 600, 300);
        flyout.ShowAt(button);
        Shots.Settle();

        var presenter = (Control)flyout.Popup.Child!;
        var origin = presenter.TranslatePoint(default, window)!.Value;
        var buttonOrigin = button.TranslatePoint(default, window)!.Value;
        if (kind == "placed")
        {
            Assert.Equal(buttonOrigin.X + button.Bounds.Width, origin.X + presenter.Bounds.Width, 1);
        }
        else
        {
            Assert.Equal(buttonOrigin.X, origin.X, 1);
        }

        flyout.Hide();
        window.Close();
    }

    [AvaloniaFact]
    public void A_Combo_Box_List_Is_As_Wide_As_The_Box_And_Below_It()
    {
        var combo = new ComboBox { Width = 200, ItemsSource = new[] { "One", "Two" }, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(40) };
        var window = Shots.Show(combo, 400, 300);
        combo.IsDropDownOpen = true;
        Shots.Settle();
        var list = combo.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().Single().Child!;
        Assert.True(list.Margin.Bottom > 0, "the list has room for its shadow");
        Assert.Equal(combo.Bounds.Width, list.Bounds.Width, 1);
        var origin = list.TranslatePoint(default, window)!.Value;
        var comboOrigin = combo.TranslatePoint(default, window)!.Value;
        Assert.Equal(comboOrigin.X, origin.X, 1);
        Assert.Equal(comboOrigin.Y + combo.Bounds.Height, origin.Y, 1);
        combo.IsDropDownOpen = false;
        window.Close();
    }

    [AvaloniaFact]
    public void A_Menu_Bar_Popup_Leaves_The_Bar_Free_And_Closes_From_Its_Shadow()
    {
        var file = new MenuItem { Header = "File", Items = { new MenuItem { Header = "New" }, new MenuItem { Header = "Open" } } };
        var bar = new Menu { Items = { file, new MenuItem { Header = "View" } }, VerticalAlignment = VerticalAlignment.Top };
        var window = Shots.Show(bar, 400, 300);
        var header = file.TranslatePoint(new Point(file.Bounds.Width / 2, file.Bounds.Height / 2), window)!.Value;
        window.MouseDown(header, MouseButton.Left);
        window.MouseUp(header, MouseButton.Left);
        Shots.Settle();
        Assert.True(file.IsSubMenuOpen);

        var popup = file.GetVisualDescendants().OfType<Popup>().Single();
        var content = (Control)popup.Child!;
        Assert.Equal(0, content.Margin.Top);
        Assert.True(content.Margin.Left > 0);

        var root = TopLevel.GetTopLevel(content)!;
        var shadow = new Point(content.Margin.Left / 2, content.Bounds.Height / 2);
        root.MouseDown(shadow, MouseButton.Left);
        root.MouseUp(shadow, MouseButton.Left);
        Shots.Settle();

        Assert.False(bar.IsOpen);
        Assert.False(file.IsSubMenuOpen);
        Assert.False(file.IsSelected);
        Assert.DoesNotContain(":pressed", file.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Menu_Bar_Keeps_Opening_Menus_After_Passing_An_Empty_Item()
    {
        var file = new MenuItem { Header = "File", Items = { new MenuItem { Header = "New" } } };
        var view = new MenuItem { Header = "View", Items = { new MenuItem { Header = "Zoom" } } };
        var help = new MenuItem { Header = "Help" };
        var bar = new Menu { Items = { file, view, help }, VerticalAlignment = VerticalAlignment.Top };
        var window = Shots.Show(bar, 400, 300);
        Point Center(MenuItem item) => item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;

        window.MouseDown(Center(file), MouseButton.Left);
        window.MouseUp(Center(file), MouseButton.Left);
        window.MouseMove(Center(help));
        window.MouseMove(Center(view));
        Shots.Settle();
        Assert.True(view.IsSubMenuOpen);

        window.MouseDown(new Point(200, 200), MouseButton.Left);
        window.MouseUp(new Point(200, 200), MouseButton.Left);
        window.MouseMove(Center(file));
        Shots.Settle();
        Assert.False(file.IsSubMenuOpen);
        window.Close();
    }
}
