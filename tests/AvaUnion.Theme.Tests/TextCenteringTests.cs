using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using AvaUnion.Controls;
using Xunit;

namespace AvaUnion.Theme.Tests;

public class TextCenteringTests
{
    private static (Button Button, TextBlock Text) Show(string family, out Window window)
    {
        var button = new Button { Content = "Button with Text", FontFamily = new FontFamily(family) };
        window = Shots.Show(new StackPanel { Children = { button } });
        return (button, button.GetVisualDescendants().OfType<TextBlock>().Single());
    }

    private static GlyphRun Sample(TextBlock text, string glyphs)
    {
        var typeface = text.TextLayout.TextLines[0].TextRuns.OfType<ShapedTextRun>().First().GlyphRun.GlyphTypeface;
        var ids = glyphs.Select(c => typeface.CharacterToGlyphMap.TryGetGlyph(c, out var id) ? id : (ushort)0).ToArray();
        return new GlyphRun(typeface, text.FontSize, glyphs.AsMemory(), ids);
    }

    private static double CapsCenter(TextBlock text, Visual relativeTo)
    {
        var line = text.TextLayout.TextLines[0];
        var run = Sample(text, "H");
        var top = text.Padding.Top + line.Baseline - (run.BaselineOrigin.Y - run.InkBounds.Top);
        return text.TranslatePoint(new Point(0, top + (run.InkBounds.Height / 2)), relativeTo)!.Value.Y;
    }

    private static void SkipWithoutNimbus() =>
        Assert.SkipUnless(FontManager.Current.SystemFonts.Any(f => f.Name == "Nimbus Sans"), "Nimbus Sans is not installed.");

    [AvaloniaFact]
    public void Noto_Sans_Is_Not_Moved()
    {
        var (_, text) = Show("Noto Sans", out var window);
        Assert.Equal(default, text.Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void Capitals_Of_A_Font_With_High_Capitals_Are_Centered()
    {
        SkipWithoutNimbus();
        var (button, text) = Show("Nimbus Sans", out var window);
        Assert.True(text.Padding.Top > 0, text.Padding.ToString());
        Assert.Equal(button.Bounds.Height / 2, CapsCenter(text, button), 1.0);

        text.Padding = new Thickness(1);
        Shots.Settle();
        Assert.Equal(new Thickness(1), text.Padding);
        window.Close();
    }

    [AvaloniaFact]
    public void Descenders_Stay_Inside_The_Text()
    {
        SkipWithoutNimbus();
        var heading = new Heading { Content = "Heading", FontFamily = new FontFamily("Nimbus Sans") };
        var window = Shots.Show(new StackPanel { Children = { heading } });
        var text = heading.GetVisualDescendants().OfType<TextBlock>().Single();
        var line = text.TextLayout.TextLines[0];
        var g = Sample(text, "g");
        var bottom = text.Padding.Top + line.Baseline + (g.InkBounds.Bottom - g.BaselineOrigin.Y);
        Assert.True(bottom <= text.Bounds.Height, $"g ends at {bottom}, the text is {text.Bounds.Height} high");
        Assert.True(text.TranslatePoint(new Point(0, bottom), heading)!.Value.Y <= heading.Bounds.Height);
        window.Close();
    }
}
