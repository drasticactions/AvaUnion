using Avalonia;
using Avalonia.Controls;
using AvaUnion.Engine;

namespace AvaUnion.Presenters;

/// <summary>
/// The indentation of a tree row. Its width is <see cref="Level"/> times the
/// <c>layout.width</c> of the style.
/// </summary>
public class IndentationSpacer : Control
{
    /// <summary>The nesting level, 0 for top-level rows.</summary>
    public static readonly StyledProperty<int> LevelProperty =
        AvaloniaProperty.Register<IndentationSpacer, int>(nameof(Level));

    static IndentationSpacer()
    {
        AffectsMeasure<IndentationSpacer>(LevelProperty);
        Union.StyleProperty.Changed.AddClassHandler<IndentationSpacer>((spacer, e) =>
        {
            if (!ResolvedStyle.SameMetrics(e.GetOldValue<Elements.UnionStyleSlot?>()?.Current, e.GetNewValue<Elements.UnionStyleSlot?>()?.Current))
            {
                spacer.InvalidateMeasure();
            }
        });
    }

    /// <summary>The nesting level, 0 for top-level rows.</summary>
    public int Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var step = (Union.GetStyle(this) ?? ResolvedStyle.Unmatched).Layout.Width ?? 0.0;
        return new Size(System.Math.Max(0, Level) * step, 0);
    }
}
