using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Markup;
using AvaUnion.Presenters;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class RightToLeftTests
{
    public static TheoryData<string> Engines => new() { "managed", "native" };

    private static void On(string engine, Action body)
    {
        if (engine == "managed")
        {
            body();
            return;
        }

        Assert.SkipWhen(NativeHost.DataMismatch is not null, NativeHost.DataMismatch ?? string.Empty);
        var managed = UnionEnvironment.Engine;
        var native = new ThreadedEngine();
        Assert.SkipUnless(native.DefaultStyle.Id is "breeze" or "breeze-rtl", $"the session style is {native.DefaultStyle.Id}");
        UnionEnvironment.Install(native);
        UnionEnvironment.Cache.Clear();
        try
        {
            body();
        }
        finally
        {
            UnionEnvironment.Install(managed);
            UnionEnvironment.Cache.Clear();
        }
    }

    private static Window Show(Control content, FlowDirection direction, double width = 300, double height = 200)
    {
        var window = new Window { Width = width, Height = height, Content = content, FlowDirection = direction };
        window.Show();
        Shots.Settle();
        return window;
    }

    private static Border ScrollView(string? styleId = null)
    {
        var view = new Border { Width = 100, Height = 100 };
        Union.SetType(view, "scrollview");
        Union.SetHints(view, "vertical-scroll");
        Union.SetStyleId(view, styleId);
        view.Bind(Decorator.PaddingProperty, StyleExtension.Create("Layout.Padding"));
        return view;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Right_To_Left_Uses_Breeze_Rtl(string engine) => On(engine, () =>
    {
        var bar = new ScrollBar { Orientation = Orientation.Vertical, Height = 100, Visibility = ScrollBarVisibility.Visible };
        var window = Show(bar, FlowDirection.RightToLeft);
        var border = Union.GetNode(bar)!.Resolved.Border;
        Assert.True(border.Right.Size > 0, $"right {border.Right.Size}");
        Assert.True(border.Left.Size is null or 0, $"left {border.Left.Size}");

        window.FlowDirection = FlowDirection.LeftToRight;
        Shots.Settle();
        border = Union.GetNode(bar)!.Resolved.Border;
        Assert.True(border.Left.Size > 0, $"left {border.Left.Size}");
        Assert.True(border.Right.Size is null or 0, $"right {border.Right.Size}");
        window.Close();
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void A_Pinned_Style_Stays_In_Right_To_Left(string engine) => On(engine, () =>
    {
        var view = ScrollView("breeze");
        var window = Show(view, FlowDirection.RightToLeft);
        var padding = Union.GetNode(view)!.Resolved.Layout.Padding;
        Assert.True(padding.Right > 0, $"right {padding.Right}");
        Assert.True(padding.Left is null or 0, $"left {padding.Left}");
        window.Close();
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Style_Padding_Keeps_Its_Side_On_Screen(string engine) => On(engine, () =>
    {
        var view = ScrollView();
        var window = Show(view, FlowDirection.RightToLeft);
        var size = Union.GetNode(view)!.Resolved.Layout.Padding.Left;
        Assert.True(size > 0, $"breeze-rtl padding-left {size}");

        // Avalonia mirrors a thickness in a right-to-left layout, so the
        // binding swaps the sides to keep Union's padding-left on the left.
        Assert.Equal(new Thickness(0, 0, size!.Value, 0), view.Padding);

        window.FlowDirection = FlowDirection.LeftToRight;
        Shots.Settle();
        Assert.Equal(new Thickness(0, 0, size.Value, 0), view.Padding);
        window.Close();
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Popups_Follow_The_Direction_Of_Their_Owner(string engine) => On(engine, () =>
    {
        var sub = new MenuItem { Header = "Sub", Items = { new MenuItem { Header = "Leaf" } } };
        var flyout = new MenuFlyout { Items = { sub } };
        var button = new Button { Content = "Menu", Flyout = flyout, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(button, FlowDirection.RightToLeft, 400, 300);
        flyout.ShowAt(button);
        Shots.Settle();

        var arrow = sub.GetVisualDescendants().OfType<UnionIcon>().Single(i => i.Name == "PART_Arrow" || Union.GetType(i) == "arrow");
        Assert.Equal("arrow-left-symbolic", arrow.EffectiveName);
        flyout.Hide();
        window.Close();
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Button_Menus_Open_At_The_Right_Edge(string engine) => On(engine, () =>
    {
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "First item of a long menu" }, new MenuItem { Header = "Second" } } };
        var button = new Button { Content = "Menu", Flyout = flyout, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(120, 40, 0, 0) };
        var window = Show(new Panel { Children = { button } }, FlowDirection.RightToLeft, 600, 300);
        flyout.ShowAt(button);
        Shots.Settle();

        var presenter = (Control)flyout.Popup.Child!;
        var right = presenter.TranslatePoint(new Point(0, 0), window)!.Value.X;
        var buttonRight = button.TranslatePoint(new Point(0, 0), window)!.Value.X;
        Assert.Equal(buttonRight, right, 1);
        flyout.Hide();
        window.Close();
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void A_Button_Menu_Arrow_Is_At_The_Start(string engine) => On(engine, () =>
    {
        double ArrowX(FlowDirection direction)
        {
            var button = new Button { Content = "With menu", Flyout = new MenuFlyout(), HorizontalAlignment = HorizontalAlignment.Center };
            var window = Show(button, direction);
            var arrow = button.GetVisualDescendants().OfType<UnionIcon>().First(i => i.EffectiveName is not null && i.IsEffectivelyVisible);
            var bounds = OnScreen(button, window);
            var x = (OnScreen(arrow, window).Center.X - bounds.X) / bounds.Width;
            window.Close();
            return x;
        }

        Assert.True(ArrowX(FlowDirection.LeftToRight) > 0.5);
        Assert.True(ArrowX(FlowDirection.RightToLeft) < 0.5);
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Check_Marks_Are_Not_Mirrored(string engine) => On(engine, () =>
    {
        byte[] Mark(FlowDirection direction)
        {
            var box = new CheckBox { IsChecked = true, Content = "Checked", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            var window = Show(box, direction, 160, 60);
            var indicator = box.GetVisualDescendants().OfType<UnionBox>().Single(b => Union.GetType(b) == "indicator");
            var rect = OnScreen(indicator, window);
            var pixels = Crop(Shots.Capture(window)!, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            window.Close();
            return pixels;
        }

        var ltr = Mark(FlowDirection.LeftToRight);
        var rtl = Mark(FlowDirection.RightToLeft);
        Assert.Equal(ltr, rtl);
    });

    [AvaloniaTheory]
    [MemberData(nameof(Engines))]
    public void Text_Fields_Align_To_The_Start(string engine) => On(engine, () =>
    {
        var box = new TextBox { Text = "Editable", Width = 200 };
        var window = Show(box, FlowDirection.RightToLeft);
        Assert.Equal(TextAlignment.Start, box.TextAlignment);
        window.Close();
    });

    [AvaloniaFact]
    public void Directional_Icons_Can_Mirror()
    {
        var plain = new UnionIcon { IconName = "go-next", Width = 16, Height = 16 };
        var mirrored = new UnionIcon { IconName = "go-next", Width = 16, Height = 16, MirrorInRightToLeft = true };
        var window = Show(new StackPanel { Children = { plain, mirrored } }, FlowDirection.RightToLeft, 100, 60);
        var frame = Shots.Capture(window)!;
        var a = OnScreen(plain, window);
        var b = OnScreen(mirrored, window);
        var plainPixels = Crop(frame, (int)a.X, (int)a.Y, 16, 16);
        var mirroredPixels = Crop(frame, (int)b.X, (int)b.Y, 16, 16);
        var toFlipped = Distance(Flip(plainPixels, 16), mirroredPixels);
        var toPlain = Distance(plainPixels, mirroredPixels);
        Assert.True(toFlipped < toPlain / 4, $"{toFlipped} from the flipped icon, {toPlain} from the plain one");
        window.Close();
    }

    private static byte[] Flip(byte[] pixels, int width)
    {
        var result = new byte[pixels.Length];
        for (var i = 0; i < pixels.Length / 4; i++)
        {
            var (row, column) = Math.DivRem(i, width);
            Array.Copy(pixels, i * 4, result, ((row * width) + (width - 1 - column)) * 4, 4);
        }

        return result;
    }

    private static long Distance(byte[] a, byte[] b)
    {
        long sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += Math.Abs(a[i] - b[i]);
        }

        return sum;
    }

    private static Rect OnScreen(Visual visual, Visual window)
    {
        var a = visual.TranslatePoint(default, window)!.Value;
        var b = visual.TranslatePoint(new Point(visual.Bounds.Width, visual.Bounds.Height), window)!.Value;
        return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
    }

    private static byte[] Crop(WriteableBitmap frame, int x, int y, int width, int height)
    {
        using var buffer = frame.Lock();
        var result = new byte[width * height * 4];
        unsafe
        {
            var source = (byte*)buffer.Address;
            for (var row = 0; row < height; row++)
            {
                new ReadOnlySpan<byte>(source + ((y + row) * buffer.RowBytes) + (x * 4), width * 4).CopyTo(result.AsSpan(row * width * 4));
            }
        }

        return result;
    }
}
