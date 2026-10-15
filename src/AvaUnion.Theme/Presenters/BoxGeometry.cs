using System;
using Avalonia;
using Avalonia.Media;

namespace AvaUnion.Presenters;

/// <summary>Rounded rectangles with per-corner radii, rings and ring segments.</summary>
public static class BoxGeometry
{
    /// <summary>Limits the radii to half the smaller side.</summary>
    public static CornerRadius Clamp(CornerRadius r, Size size)
    {
        var max = Math.Max(0, Math.Min(size.Width, size.Height) / 2);
        return new CornerRadius(Math.Min(r.TopLeft, max), Math.Min(r.TopRight, max), Math.Min(r.BottomRight, max), Math.Min(r.BottomLeft, max));
    }

    /// <summary>Adds an amount for each side to the radii. A radius is never less than zero.</summary>
    public static CornerRadius Offset(CornerRadius r, Thickness by, double sign) => new(
        Math.Max(0, r.TopLeft + sign * Math.Max(by.Left, by.Top)),
        Math.Max(0, r.TopRight + sign * Math.Max(by.Right, by.Top)),
        Math.Max(0, r.BottomRight + sign * Math.Max(by.Right, by.Bottom)),
        Math.Max(0, r.BottomLeft + sign * Math.Max(by.Left, by.Bottom)));

    /// <summary>A rounded rectangle figure.</summary>
    public static void AddRoundedRect(StreamGeometryContext ctx, Rect rect, CornerRadius r)
    {
        var tl = r.TopLeft;
        var tr = r.TopRight;
        var br = r.BottomRight;
        var bl = r.BottomLeft;
        ctx.BeginFigure(new Point(rect.Left + tl, rect.Top), true);
        ctx.LineTo(new Point(rect.Right - tr, rect.Top));
        if (tr > 0)
        {
            ctx.ArcTo(new Point(rect.Right, rect.Top + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise);
        }

        ctx.LineTo(new Point(rect.Right, rect.Bottom - br));
        if (br > 0)
        {
            ctx.ArcTo(new Point(rect.Right - br, rect.Bottom), new Size(br, br), 0, false, SweepDirection.Clockwise);
        }

        ctx.LineTo(new Point(rect.Left + bl, rect.Bottom));
        if (bl > 0)
        {
            ctx.ArcTo(new Point(rect.Left, rect.Bottom - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise);
        }

        ctx.LineTo(new Point(rect.Left, rect.Top + tl));
        if (tl > 0)
        {
            ctx.ArcTo(new Point(rect.Left + tl, rect.Top), new Size(tl, tl), 0, false, SweepDirection.Clockwise);
        }

        ctx.EndFigure(true);
    }

    /// <summary>A filled rounded rectangle.</summary>
    public static StreamGeometry RoundedRect(Rect rect, CornerRadius r)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            AddRoundedRect(ctx, rect, r);
        }

        return g;
    }

    /// <summary>The ring between two rounded rectangles, with even-odd fill.</summary>
    public static StreamGeometry Ring(Rect outer, CornerRadius outerRadii, Rect inner, CornerRadius innerRadii)
    {
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            AddRoundedRect(ctx, outer, outerRadii);
            if (inner.Width > 0 && inner.Height > 0)
            {
                AddRoundedRect(ctx, inner, innerRadii);
            }
        }

        return g;
    }

    /// <summary>
    /// Divides a ring into the straight part of each side and the square at
    /// each corner, as Union draws a border: left, top, right, bottom, then
    /// top-left, top-right, bottom-right, bottom-left. A corner square spans
    /// its radius and the widths of its two sides.
    /// </summary>
    public static Rect[] RingPieces(Rect outer, Thickness widths, CornerRadius radii)
    {
        var tl = new Size(radii.TopLeft + widths.Left, radii.TopLeft + widths.Top);
        var tr = new Size(radii.TopRight + widths.Right, radii.TopRight + widths.Top);
        var br = new Size(radii.BottomRight + widths.Right, radii.BottomRight + widths.Bottom);
        var bl = new Size(radii.BottomLeft + widths.Left, radii.BottomLeft + widths.Bottom);
        Rect Span(double x, double y, double w, double h) => new(x, y, Math.Max(0, w), Math.Max(0, h));
        return
        [
            Span(outer.Left, outer.Top + tl.Height, widths.Left, outer.Height - tl.Height - bl.Height),
            Span(outer.Left + tl.Width, outer.Top, outer.Width - tl.Width - tr.Width, widths.Top),
            Span(outer.Right - widths.Right, outer.Top + tr.Height, widths.Right, outer.Height - tr.Height - br.Height),
            Span(outer.Left + bl.Width, outer.Bottom - widths.Bottom, outer.Width - bl.Width - br.Width, widths.Bottom),
            Span(outer.Left, outer.Top, tl.Width, tl.Height),
            Span(outer.Right - tr.Width, outer.Top, tr.Width, tr.Height),
            Span(outer.Right - br.Width, outer.Bottom - br.Height, br.Width, br.Height),
            Span(outer.Left, outer.Bottom - bl.Height, bl.Width, bl.Height),
        ];
    }
}
