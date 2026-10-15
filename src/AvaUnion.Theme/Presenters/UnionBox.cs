using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Markup;

namespace AvaUnion.Presenters;

/// <summary>
/// Draws the resolved style of one element. The outline is outside the bounds.
/// The box draws its own node, or the nearest node above it.
/// </summary>
public class UnionBox : Control
{
    private readonly SlotRepaint _repaint;

    private (Size Size, double Scale, ResolvedStyle.CornersGroup Corners, Thickness Border, Thickness Outline) _geometryKey;
    private Geometry? _background;
    private Geometry? _borderRing;
    private Geometry? _outlineRing;
    private Rect[]? _borderPieces;
    private Rect[]? _outlinePieces;
    private CornerRadius _radii;
    private Rect _rect;

    /// <summary>
    /// When true, the box measures to <c>layout.width × layout.height</c> of
    /// its style. A box with its own <c>Union.Type</c> always uses this size.
    /// </summary>
    public static readonly StyledProperty<bool> ImplicitSizeProperty =
        AvaloniaProperty.Register<UnionBox, bool>(nameof(ImplicitSize));

    /// <summary>Creates the presenter.</summary>
    public UnionBox()
    {
        _repaint = new SlotRepaint(this);
    }

    static UnionBox()
    {
        AffectsMeasure<UnionBox>(ImplicitSizeProperty);
        AffectsRender<UnionBox>(Union.StyleProperty);
    }

    /// <inheritdoc/>
    protected override bool BypassFlowDirectionPolicies => true;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Union.StyleProperty)
        {
            _repaint.Update();
            if (!ResolvedStyle.SameMetrics(change.GetOldValue<UnionStyleSlot?>()?.Current, change.GetNewValue<UnionStyleSlot?>()?.Current))
            {
                InvalidateMeasure();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _repaint.SetAttached(true);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _repaint.SetAttached(false);
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>True when the box measures to the layout size of its style.</summary>
    public bool ImplicitSize
    {
        get => GetValue(ImplicitSizeProperty);
        set => SetValue(ImplicitSizeProperty, value);
    }

    /// <summary>The style of the node of the box, or of the nearest node above it.</summary>
    public ResolvedStyle Style => Union.GetStyle(this) ?? ResolvedStyle.Unmatched;

    /// <summary>The extents of the outline outside the bounds.</summary>
    public Thickness OutlineExtents => Style.Outline.DrawnWidths();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Union.GetType(this) is null && !ImplicitSize)
        {
            return default;
        }

        var style = Style;
        return new Size(style.Layout.Width ?? 0.0, style.Layout.Height ?? 0.0);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var style = Style;
        if (!style.IsMatch || !style.HasDrawable || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var border = SnapWidths(style.Border.DrawnWidths(), scale);
        var outline = SnapWidths(style.Outline.DrawnWidths(), scale);
        EnsureGeometry(Bounds.Size, scale, style.Corners, border, outline);

        if (style.Shadow.HasAnyValue && style.Shadow.Color is { A: > 0 } shadowColor)
        {
            var size = style.Shadow.Size ?? 0.0;
            var blur = style.Shadow.Blur ?? 0.0;
            var offset = style.Shadow.Offset.ToVector() * scale;
            var shadow = new BoxShadow
            {
                OffsetX = offset.X,
                OffsetY = offset.Y,
                Blur = ShadowBlur(blur),
                Spread = size,
                Color = shadowColor,
            };
            var rr = new RoundedRect(_rect, _radii.TopLeft, _radii.TopRight, _radii.BottomRight, _radii.BottomLeft);
            context.DrawRectangle(null, null, rr, new BoxShadows(shadow));
        }

        if (style.Background.Color is { A: > 0 } background && _background is not null)
        {
            context.DrawGeometry(BrushCache.Get(background), null, _background);
        }

        DrawImage(context, style, scale);

        DrawRing(context, style.Outline.Left.Color, style.Outline.Top.Color, style.Outline.Right.Color, style.Outline.Bottom.Color, outline, _outlineRing, _outlinePieces, null);
        DrawRing(context, style.Border.Left.Color, style.Border.Top.Color, style.Border.Right.Color, style.Border.Bottom.Color, border, _borderRing, _borderPieces, style.Background.Color);
    }

    private static void DrawRing(DrawingContext context, Color? left, Color? top, Color? right, Color? bottom, Thickness widths, Geometry? ring, Rect[]? pieces, Color? fill)
    {
        if (ring is null)
        {
            return;
        }

        Color?[] colors = [widths.Left > 0 ? left : null, widths.Top > 0 ? top : null, widths.Right > 0 ? right : null, widths.Bottom > 0 ? bottom : null];
        if (colors.All(c => c is null))
        {
            return;
        }

        if (colors.All(c => c is not null) && colors.Distinct().Count() == 1)
        {
            if (colors[0]!.Value.A > 0)
            {
                context.DrawGeometry(BrushCache.Get(colors[0]!.Value), null, ring);
            }

            return;
        }

        if (pieces is null)
        {
            return;
        }

        for (var i = 0; i < 4; i++)
        {
            if (colors[i] is { A: > 0 } c && pieces[i] is { Width: > 0, Height: > 0 } side)
            {
                using (context.PushClip(side))
                {
                    context.DrawGeometry(BrushCache.Get(c), null, ring);
                }
            }
        }

        DrawCorner(context, ring, pieces[4], colors[0], colors[1], fill, pieces[4].BottomLeft, pieces[4].TopRight);
        DrawCorner(context, ring, pieces[5], colors[1], colors[2], fill, pieces[5].TopLeft, pieces[5].BottomRight);
        DrawCorner(context, ring, pieces[6], colors[2], colors[3], fill, pieces[6].TopRight, pieces[6].BottomLeft);
        DrawCorner(context, ring, pieces[7], colors[3], colors[0], fill, pieces[7].BottomRight, pieces[7].TopLeft);
    }

    private static void DrawCorner(DrawingContext context, Geometry ring, Rect corner, Color? from, Color? to, Color? fill, Point start, Point end)
    {
        if (corner.Width <= 0 || corner.Height <= 0 || (from is null && to is null))
        {
            return;
        }

        if (fill is { } f && from is { } x && to is { } y && x != y)
        {
            if (x == f)
            {
                from = y;
            }
            else if (y == f)
            {
                to = x;
            }
        }

        var a = from ?? Colors.Transparent;
        var b = to ?? Colors.Transparent;
        IBrush brush = a == b
            ? BrushCache.Get(a)
            : new LinearGradientBrush
            {
                StartPoint = new RelativePoint(start, RelativeUnit.Absolute),
                EndPoint = new RelativePoint(end, RelativeUnit.Absolute),
                GradientStops = { new GradientStop(a, 0), new GradientStop(b, 1) },
            };
        using (context.PushClip(corner))
        {
            context.DrawGeometry(brush, null, ring);
        }
    }

    private void DrawImage(DrawingContext context, ResolvedStyle style, double scale)
    {
        var image = style.Background.Image;
        if (string.IsNullOrEmpty(image.Source) || _background is null)
        {
            return;
        }

        var flags = image.Flags ?? UnionImageFlags.None;
        var masked = (flags & (UnionImageFlags.Mask | UnionImageFlags.InvertedMask)) != 0;
        var mask = masked ? image.MaskColor ?? UnionRgbaTransparent : (Color?)null;
        var engine = UnionEnvironment.Engine;

        using (context.PushGeometryClip(_background))
        {
            if (engine.GetImageGeometry(image.Source) is { } geometry)
            {
                if (mask is not { A: > 0 } fill)
                {
                    return;
                }

                var box = geometry.ViewBox;
                var shapes = UnionImageCache.Geometry(geometry, image.Source, 1.0);
                if (shapes is null || box.Width <= 0 || box.Height <= 0)
                {
                    return;
                }

                var transform = Matrix.CreateTranslation(-box.X, -box.Y) * Matrix.CreateScale(_rect.Width / box.Width, _rect.Height / box.Height) * Matrix.CreateTranslation(_rect.X, _rect.Y);
                using (context.PushTransform(transform))
                {
                    context.DrawGeometry(BrushCache.Get(fill), null, shapes);
                }

                return;
            }

            var px = new PixelSize((int)Math.Ceiling(_rect.Width), (int)Math.Ceiling(_rect.Height));
            var key = string.Create(CultureInfo.InvariantCulture, $"img|{image.Source}|{px.Width}x{px.Height}|{scale}|{mask?.ToUInt32()}|{(int)flags}|{engine.Epoch}");
            var bitmap = UnionImageCache.Bitmap(key, () => UnionImageCache.ToBitmap(
                engine.RenderImage(image.Source, mask, (flags & UnionImageFlags.InvertedMask) != 0, px, scale), scale));
            if (bitmap is not null)
            {
                context.DrawImage(bitmap, _rect);
            }
        }
    }

    private static readonly Color UnionRgbaTransparent = Color.FromArgb(0, 0, 0, 0);

    internal static double ShadowBlur(double blur)
    {
        if (blur <= 0)
        {
            return 0;
        }

        return Math.Max(1e-3, ((0.47 * blur) - 0.5) / 0.288675);
    }

    private void EnsureGeometry(Size size, double scale, ResolvedStyle.CornersGroup corners, Thickness border, Thickness outline)
    {
        var key = (size, scale, corners, border, outline);
        if (_background is not null && key.Equals(_geometryKey))
        {
            return;
        }

        _geometryKey = key;
        _rect = new Rect(size);
        _radii = BoxGeometry.Clamp(corners.Radii(), size);
        _background = BoxGeometry.RoundedRect(_rect, _radii);

        if (border != default)
        {
            var inner = _rect.Deflate(border);
            _borderRing = BoxGeometry.Ring(_rect, _radii, inner, BoxGeometry.Offset(_radii, border, -1));
            _borderPieces = BoxGeometry.RingPieces(_rect, border, _radii);
        }
        else
        {
            _borderRing = null;
            _borderPieces = null;
        }

        if (outline != default)
        {
            var outer = _rect.Inflate(outline);
            _outlineRing = BoxGeometry.Ring(outer, BoxGeometry.Offset(_radii, outline, 1), _rect, _radii);
            _outlinePieces = BoxGeometry.RingPieces(outer, outline, BoxGeometry.Offset(_radii, outline, 1));
        }
        else
        {
            _outlineRing = null;
            _outlinePieces = null;
        }
    }


    private static Thickness SnapWidths(Thickness t, double scale) => new(Snap(t.Left, scale), Snap(t.Top, scale), Snap(t.Right, scale), Snap(t.Bottom, scale));

    private static double Snap(double width, double scale)
    {
        if (width <= 0)
        {
            return 0;
        }

        return Math.Max(1, Math.Round(width * scale, MidpointRounding.AwayFromZero)) / scale;
    }
}
