using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;

namespace AvaUnion.Presenters;

internal sealed class TextCentering
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TextCentering, Visual, bool>("IsEnabled");

    private static readonly AttachedProperty<Thickness> SpaceProperty =
        AvaloniaProperty.RegisterAttached<TextCentering, Visual, Thickness>("Space");

    private static readonly Dictionary<(GlyphTypeface Typeface, double Size), (double Above, double Height)> s_caps = [];
    private static readonly Dictionary<GlyphTypeface, (double Ascent, double Descent)?> s_qtMetrics = [];

    static TextCentering()
    {
        IsEnabledProperty.Changed.AddClassHandler<Visual>((visual, _) => Update(visual));
        Visual.BoundsProperty.Changed.AddClassHandler<TextBlock>((visual, _) => Update(visual));
        Visual.BoundsProperty.Changed.AddClassHandler<TextPresenter>((visual, _) => Update(visual));
    }

    private TextCentering()
    {
    }

    public static Thickness Space(TextLayout layout)
    {
        if (layout.TextLines.Count == 0)
        {
            return default;
        }

        var line = layout.TextLines[0];
        if (line.TextRuns.OfType<ShapedTextRun>().FirstOrDefault() is not { } run)
        {
            return default;
        }

        var glyphs = run.GlyphRun;
        var scale = glyphs.FontRenderingEmSize / glyphs.GlyphTypeface.Metrics.DesignEmHeight;
        var (ascent, descent) = QtMetrics(glyphs.GlyphTypeface) is { } qt
            ? (qt.Ascent * scale, qt.Descent * scale)
            : (line.Baseline, line.Height - line.Baseline);
        var top = ascent - line.Baseline;
        var bottom = descent - (line.Height - line.Baseline);

        if (Caps(glyphs) is { } caps)
        {
            var offset = Math.Truncate(((ascent + descent) / 2) - (ascent - caps.Above + (caps.Height / 2)));
            top += Math.Max(0, 2 * offset);
            bottom += Math.Max(0, -2 * offset);
        }

        return new Thickness(0, Snap(top), 0, Snap(bottom));
    }

    private static double Snap(double value) => Math.Abs(value) < 0.01 ? 0 : value;

    private static void Update(Visual visual)
    {
        var layout = visual.GetValue(IsEnabledProperty) ? visual switch
        {
            TextBlock text => text.TextLayout,
            TextPresenter presenter => presenter.TextLayout,
            _ => null,
        } : null;
        var space = layout is null ? default : Space(layout);
        if (space == visual.GetValue(SpaceProperty))
        {
            return;
        }

        visual.SetValue(SpaceProperty, space);
        visual.SetValue(visual is TextBlock ? TextBlock.PaddingProperty : Layoutable.MarginProperty, space, BindingPriority.Style);
    }

    private static (double Ascent, double Descent)? QtMetrics(GlyphTypeface typeface)
    {
        if (s_qtMetrics.TryGetValue(typeface, out var cached))
        {
            return cached;
        }

        (double, double)? metrics = null;
        if (typeface.PlatformTypeface.TryGetTable(OpenTypeTag.Parse("hhea"), out var hhea) && hhea.Length >= 10)
        {
            var span = hhea.Span;
            var ascent = BinaryPrimitives.ReadInt16BigEndian(span[4..]);
            var descent = BinaryPrimitives.ReadInt16BigEndian(span[6..]);
            if (ascent != 0 || descent != 0)
            {
                metrics = (ascent, -descent);
            }
        }

        if (typeface.PlatformTypeface.TryGetTable(OpenTypeTag.Parse("OS/2"), out var os2) && os2.Length >= 78)
        {
            var span = os2.Span;
            var useTypoMetrics = (BinaryPrimitives.ReadUInt16BigEndian(span[62..]) & 0x80) != 0;
            var typoAscent = BinaryPrimitives.ReadInt16BigEndian(span[68..]);
            var typoDescent = BinaryPrimitives.ReadInt16BigEndian(span[70..]);
            var winAscent = BinaryPrimitives.ReadUInt16BigEndian(span[74..]);
            var winDescent = BinaryPrimitives.ReadUInt16BigEndian(span[76..]);
            if (useTypoMetrics && (typoAscent != 0 || typoDescent != 0))
            {
                metrics = (typoAscent, -typoDescent);
            }
            else if (!useTypoMetrics && (winAscent != 0 || winDescent != 0))
            {
                metrics = (winAscent, winDescent);
            }
        }

        s_qtMetrics[typeface] = metrics;
        return metrics;
    }

    private static (double Above, double Height)? Caps(GlyphRun sample)
    {
        var key = (sample.GlyphTypeface, sample.FontRenderingEmSize);
        if (s_caps.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (!sample.GlyphTypeface.CharacterToGlyphMap.TryGetGlyph('H', out var glyph))
        {
            return null;
        }

        var run = new GlyphRun(sample.GlyphTypeface, sample.FontRenderingEmSize, "H".AsMemory(), new[] { glyph });
        var ink = run.InkBounds;
        if (ink.Height <= 0)
        {
            return null;
        }

        var caps = (run.BaselineOrigin.Y - ink.Top, ink.Height);
        s_caps[key] = caps;
        return caps;
    }
}
