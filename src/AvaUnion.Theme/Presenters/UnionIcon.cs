using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaUnion.Elements;
using AvaUnion.Engine;
using AvaUnion.Icons;

namespace AvaUnion.Presenters;

/// <summary>
/// Shows the icon of the nearest element, at the icon size and with the icon
/// color of its style. A typed icon uses <c>layout.width</c> and
/// <c>layout.height</c> as its size.
/// </summary>
public class UnionIcon : Control
{
    private readonly SlotRepaint _repaint;

    /// <summary>An icon name that overrides the style's.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<UnionIcon, string?>(nameof(IconName));

    /// <summary>A tint that overrides the style's.</summary>
    public static readonly StyledProperty<Color?> ColorProperty =
        AvaloniaProperty.Register<UnionIcon, Color?>(nameof(Color));

    /// <summary>
    /// When true, the icon is drawn mirrored in a right-to-left layout, for
    /// example an arrow that points to the next page.
    /// </summary>
    public static readonly StyledProperty<bool> MirrorInRightToLeftProperty =
        AvaloniaProperty.Register<UnionIcon, bool>(nameof(MirrorInRightToLeft));

    /// <summary>Creates the presenter.</summary>
    public UnionIcon()
    {
        _repaint = new SlotRepaint(this);
    }

    static UnionIcon()
    {
        AffectsMeasure<UnionIcon>(IconNameProperty);
        AffectsRender<UnionIcon>(Union.StyleProperty, IconNameProperty, ColorProperty);
    }

    /// <summary>An icon name that overrides the style's.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>A tint that overrides the style's.</summary>
    public Color? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>
    /// When true, the icon is drawn mirrored in a right-to-left layout, for
    /// example an arrow that points to the next page.
    /// </summary>
    public bool MirrorInRightToLeft
    {
        get => GetValue(MirrorInRightToLeftProperty);
        set => SetValue(MirrorInRightToLeftProperty, value);
    }

    /// <inheritdoc/>
    protected override bool BypassFlowDirectionPolicies => !MirrorInRightToLeft;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MirrorInRightToLeftProperty)
        {
            InvalidateMirrorTransform();
        }
        else if (change.Property == Union.StyleProperty)
        {
            _repaint.Update();
            if (!ResolvedStyle.SameMetrics(change.GetOldValue<Elements.UnionStyleSlot?>()?.Current, change.GetNewValue<Elements.UnionStyleSlot?>()?.Current))
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
        IconProviders.Invalidated += InvalidateVisual;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _repaint.SetAttached(false);
        IconProviders.Invalidated -= InvalidateVisual;
        base.OnDetachedFromVisualTree(e);
    }

    private ResolvedStyle Style => Union.GetStyle(this) ?? ResolvedStyle.Unmatched;

    /// <summary>The icon name in effect.</summary>
    public string? EffectiveName
    {
        get
        {
            var style = Style;
            var name = IconName ?? style.Icon.Name;
            if (string.IsNullOrEmpty(name))
            {
                name = style.Icon.Source;
            }

            return string.IsNullOrEmpty(name) ? null : name;
        }
    }

    internal Color? EffectiveColor => Color ?? Style.Icon.Color ?? (UnionEnvironment.IsReady
        ? UnionEnvironment.Engine.SchemeColor(ColorGroup.Active, ColorSet.Window, ColorRoleKind.Foreground, (int)ForegroundRole.Text)
        : null);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var style = Style;
        if (Union.GetType(this) is not null)
        {
            return new Size(style.Layout.Width ?? 0.0, style.Layout.Height ?? 0.0);
        }

        if (EffectiveName is null)
        {
            return default;
        }

        return new Size(style.Icon.Width ?? 16.0, style.Icon.Height ?? 16.0);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var name = EffectiveName;
        if (name is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var style = Style;
        var width = double.IsNaN(Width) ? style.Icon.Width ?? Bounds.Width : Bounds.Width;
        var height = double.IsNaN(Height) ? style.Icon.Height ?? Bounds.Height : Bounds.Height;
        var size = new Size(System.Math.Min(width, Bounds.Width), System.Math.Min(height, Bounds.Height));
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var image = IconProviders.Get(name, EffectiveColor, size, scale);
        if (image is null)
        {
            return;
        }

        var rect = new Rect((Bounds.Width - size.Width) / 2, (Bounds.Height - size.Height) / 2, size.Width, size.Height);
        context.DrawImage(image, rect);
    }
}
