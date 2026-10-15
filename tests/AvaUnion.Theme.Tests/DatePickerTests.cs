using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class DatePickerTests
{
    [AvaloniaFact]
    public void Date_Picker_Flyout_Has_Tumblers()
    {
        var picker = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero) };
        var window = Shots.Show(picker, 360, 460);
        var popup = picker.GetVisualDescendants().OfType<Popup>().Single();
        popup.IsOpen = true;
        Shots.Settle();
        var presenter = (DatePickerPresenter)popup.Child!;
        var tumblers = presenter.GetVisualDescendants().Where(v => Union.GetType((StyledElement)v) == "tumbler").ToList();
        Assert.Equal(3, tumblers.Count);
        var text = presenter.GetVisualDescendants().OfType<ListBoxItem>().First();
        Assert.Contains("popup.combobox > tumbler > text", Union.GetNode(text)!.ChainText);
        Shots.Capture(window, "picker-flyout");
        popup.IsOpen = false;
    }

    [AvaloniaFact]
    public void Date_Picker_Shows_The_Selected_Row()
    {
        var picker = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        var window = Shots.Show(picker, 360, 460);
        var face = picker.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_FlyoutButton");
        Click(window, face, face.Bounds.Center);
        var presenter = (DatePickerPresenter)picker.GetVisualDescendants().OfType<Popup>().Single().Child!;

        var day = presenter.GetVisualDescendants().OfType<ListBoxItem>().First(i => i.Content?.ToString() == "17");
        Click(window, day, new Point(10, 10));
        for (var i = 0; i < 30; i++)
        {
            Shots.Settle();
        }

        var selected = presenter.GetVisualDescendants().OfType<ListBoxItem>().Where(i => i.IsSelected).ToList();
        var seventeen = Assert.Single(selected, i => i.Content?.ToString() == "17");
        var other = presenter.GetVisualDescendants().OfType<ListBoxItem>().First(i => !i.IsSelected && i.IsEffectivelyVisible);
        Assert.NotEqual(((ISolidColorBrush)other.Foreground!).Color, ((ISolidColorBrush)seventeen.Foreground!).Color);

        var frame = Shots.Capture(window, "picker-selected")!;
        var center = seventeen.TranslatePoint(new Point(4, seventeen.Bounds.Height / 2), window)!.Value;
        var band = (ISolidColorBrush)presenter.FindResource("Union.Active.Selection.Background.NormalBrush")!;
        Assert.Equal(band.Color, Pixel(frame, (int)center.X, (int)center.Y));
        picker.GetVisualDescendants().OfType<Popup>().Single().IsOpen = false;
    }

    private static void Click(Window window, Visual target, Point at)
    {
        var p = target.TranslatePoint(at, window)!.Value;
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Shots.Settle();
    }

    private static Color Pixel(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        unsafe
        {
            var p = (byte*)buffer.Address + (y * buffer.RowBytes) + (x * 4);
            return Color.FromArgb(p[3], p[0], p[1], p[2]);
        }
    }
}
