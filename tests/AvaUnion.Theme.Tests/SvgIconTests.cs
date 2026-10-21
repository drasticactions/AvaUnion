using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaUnion.Icons;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class SvgIconTests
{
    private const string Head = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='20' height='20'>";

    private sealed class Picture(byte[] data, int width)
    {
        public Color this[int x, int y]
        {
            get
            {
                var i = ((y * width) + x) * 4;
                return Color.FromArgb(data[i + 3], data[i + 2], data[i + 1], data[i]);
            }
        }
    }

    private static Picture Render(string body, Color? tint = null, int size = 20)
    {
        var icon = SvgIcon.Read(new MemoryStream(Encoding.UTF8.GetBytes(Head + body + "</svg>")));
        Assert.NotNull(icon);
        using var target = new RenderTargetBitmap(new PixelSize(size, size));
        using (var context = target.CreateDrawingContext())
        {
            context.DrawImage(icon.ToImage(tint), new Rect(0, 0, size, size));
        }

        var data = new byte[size * size * 4];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(0, 0, size, size), handle.AddrOfPinnedObject(), data.Length, size * 4);
        }
        finally
        {
            handle.Free();
        }

        return new Picture(data, size);
    }

    private static void Near(Color expected, Color actual, int tolerance = 8)
    {
        if (expected.A == 0)
        {
            Assert.True(actual.A <= tolerance, $"expected transparent, got {actual}");
            return;
        }

        Assert.True(
            System.Math.Abs(expected.A - actual.A) <= tolerance && System.Math.Abs(expected.R - actual.R) <= tolerance
            && System.Math.Abs(expected.G - actual.G) <= tolerance && System.Math.Abs(expected.B - actual.B) <= tolerance,
            $"expected {expected}, got {actual}");
    }

    [AvaloniaFact]
    public void A_Luminance_Mask_Hides_Where_It_Is_Black()
    {
        var p = Render("<mask id='m' maskUnits='userSpaceOnUse'><rect width='10' height='20' fill='white'/><rect x='10' width='10' height='20' fill='black'/></mask>"
            + "<rect width='20' height='20' fill='red' mask='url(#m)'/>");
        Near(Colors.Red, p[5, 10]);
        Near(Colors.Transparent, p[15, 10]);
    }

    [AvaloniaFact]
    public void A_Mask_Follows_The_Transforms_Of_Its_Content_And_Target()
    {
        var p = Render("<linearGradient id='g' gradientUnits='userSpaceOnUse' x1='-20' x2='-10'><stop offset='0' stop-color='white'/><stop offset='1' stop-color='black'/></linearGradient>"
            + "<mask id='m' maskUnits='userSpaceOnUse'><rect x='-20' y='-20' width='10' height='40' fill='url(#g)' transform='scale(-1,1)'/></mask>"
            + "<g transform='translate(10,10) rotate(90) translate(-10,-10)' mask='url(#m)'><rect width='20' height='20' fill='red'/></g>");
        Near(Colors.Transparent, p[10, 3]);
        Near(Color.FromArgb(255, 255, 0, 0), p[10, 18], 40);
    }

    [AvaloniaFact]
    public void Dashes_Are_In_User_Units_And_Start_At_The_Path_Start()
    {
        var p = Render("<path d='M0 10H20' stroke='black' stroke-width='2' stroke-dasharray='4 2'/>");
        Near(Colors.Black, p[2, 10]);
        Near(Colors.Transparent, p[5, 10]);
        Near(Colors.Black, p[8, 10]);
        Near(Colors.Transparent, p[11, 10]);
        Near(Colors.Black, p[13, 10]);
    }

    [AvaloniaFact]
    public void Transforms_Read_Numbers_Without_Separators()
    {
        var p = Render("<rect y='1010' width='20' height='10' fill='blue' transform='translate(0-1000)'/>");
        Near(Colors.Blue, p[10, 15]);
        Near(Colors.Transparent, p[10, 5]);
    }

    [AvaloniaFact]
    public void Linear_Gradients_Run_Between_Their_End_Points()
    {
        var p = Render("<linearGradient id='g' x1='0' x2='20' gradientUnits='userSpaceOnUse'><stop offset='0' stop-color='#000'/><stop offset='1' stop-color='#fff'/></linearGradient>"
            + "<rect width='20' height='20' fill='url(#g)'/>");
        Assert.True(p[2, 10].R < 50, p[2, 10].ToString());
        Assert.True(p[17, 10].R > 200, p[17, 10].ToString());
    }

    [AvaloniaFact]
    public void Bounding_Box_Gradients_Follow_The_Shape()
    {
        var p = Render("<linearGradient id='g' x2='0' y2='1'><stop offset='0.5' stop-color='red'/><stop offset='0.5' stop-color='blue'/></linearGradient>"
            + "<rect x='10' width='10' height='20' fill='url(#g)'/>");
        Near(Colors.Red, p[15, 4]);
        Near(Colors.Blue, p[15, 16]);
        Near(Colors.Transparent, p[5, 4]);
    }

    [AvaloniaFact]
    public void Radial_Gradients_Spread_From_The_Centre()
    {
        var p = Render("<radialGradient id='g' cx='10' cy='10' r='10' gradientUnits='userSpaceOnUse'><stop offset='0' stop-color='#fff'/><stop offset='1' stop-color='#000'/></radialGradient>"
            + "<rect width='20' height='20' fill='url(#g)'/>");
        Assert.True(p[10, 10].R > 220, p[10, 10].ToString());
        Assert.True(p[1, 10].R < 40, p[1, 10].ToString());
    }

    [AvaloniaFact]
    public void Strokes_Scale_With_The_Transform()
    {
        var p = Render("<g transform='scale(2)'><path d='M0 5H10' stroke='black' stroke-width='2'/></g>");
        Near(Colors.Black, p[10, 8]);
        Near(Colors.Black, p[10, 11]);
        Near(Colors.Transparent, p[10, 14]);
    }

    [AvaloniaFact]
    public void Use_Draws_The_Referenced_Element_At_Its_Offset_With_Inherited_Paint()
    {
        var p = Render("<defs><rect id='r' width='5' height='5'/></defs><use xlink:href='#r' x='10' y='10' fill='green'/>");
        Near(Color.Parse("#008000"), p[12, 12]);
        Near(Colors.Transparent, p[2, 2]);
    }

    [AvaloniaFact]
    public void Clip_Paths_Cut_The_Element()
    {
        var p = Render("<clipPath id='c'><circle cx='10' cy='10' r='5'/></clipPath><rect width='20' height='20' fill='red' clip-path='url(#c)'/>");
        Near(Colors.Red, p[10, 10]);
        Near(Colors.Transparent, p[1, 1]);
    }

    [AvaloniaFact]
    public void A_Clip_Path_That_Does_Not_Exist_Is_Ignored()
    {
        var p = Render("<rect width='20' height='20' fill='red' clip-path='url(#missing)'/>");
        Near(Colors.Red, p[1, 1]);
    }

    [AvaloniaFact]
    public void Even_Odd_Leaves_The_Inner_Ring_Empty()
    {
        var p = Render("<path fill-rule='evenodd' d='M0 0H20V20H0ZM5 5H15V15H5Z'/>");
        Near(Colors.Black, p[2, 2]);
        Near(Colors.Transparent, p[10, 10]);
    }

    [AvaloniaFact]
    public void An_Invalid_Fill_Is_Ignored()
    {
        var p = Render("<rect width='20' height='20' fill='currentColor;stroke:none'/>");
        Near(Colors.Black, p[10, 10]);
    }

    [AvaloniaFact]
    public void A_Blur_Spreads_Past_The_Shape_Edge()
    {
        var sharp = Render("<rect x='5' y='5' width='10' height='10'/>");
        var blurred = Render("<filter id='f'><feGaussianBlur stdDeviation='1.5'/></filter><rect x='5' y='5' width='10' height='10' filter='url(#f)'/>");
        Assert.Equal(0, sharp[4, 10].A);
        Assert.InRange(blurred[4, 10].A, 5, 200);
        Assert.Equal(0, blurred[2, 10].A);
        Assert.True(blurred[10, 10].A > 200, blurred[10, 10].ToString());
    }

    [AvaloniaFact]
    public void Text_Class_Parts_Take_The_Tint_And_Other_Classes_Keep_Their_Color()
    {
        var body = "<style>.ColorScheme-Text{color:#232629}.ColorScheme-NegativeText{color:#da4453}</style>"
            + "<rect width='10' height='20' class='ColorScheme-Text' fill='currentColor'/>"
            + "<rect x='10' width='10' height='20' class='ColorScheme-NegativeText' fill='currentColor'/>";
        var plain = Render(body);
        Near(Color.Parse("#232629"), plain[5, 10]);
        var tinted = Render(body, Colors.Lime);
        Near(Colors.Lime, tinted[5, 10]);
        Near(Color.Parse("#da4453"), tinted[15, 10]);
    }

    [AvaloniaFact]
    public void Stroke_Order_Can_Put_The_Fill_On_Top()
    {
        var p = Render("<rect x='5' y='5' width='10' height='10' fill='red' stroke='blue' stroke-width='4' paint-order='stroke'/>");
        Near(Colors.Red, p[6, 10]);
        Near(Colors.Blue, p[4, 10]);
    }
}
