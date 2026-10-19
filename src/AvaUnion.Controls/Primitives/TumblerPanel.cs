using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace AvaUnion.Controls.Primitives;

internal sealed class TumblerPanel : Panel
{
    private const int PathItemCount = 5;

    private Tumbler? _tumbler;

    public TumblerPanel() => ClipToBounds = true;

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        _tumbler = this.FindLogicalAncestorOfType<Tumbler>();
        if (_tumbler is not null)
        {
            _tumbler.PropertyChanged += OnTumblerPropertyChanged;
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (_tumbler is not null)
        {
            _tumbler.PropertyChanged -= OnTumblerPropertyChanged;
            _tumbler = null;
        }

        base.OnDetachedFromLogicalTree(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var count = _tumbler?.VisibleItemCount ?? 5;
        var itemHeight = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height / count;
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, itemHeight));
            width = Math.Max(width, child.DesiredSize.Width);
        }

        return new Size(width, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = Children.Count;
        var visible = _tumbler?.VisibleItemCount ?? 5;
        var position = _tumbler?.Position ?? 0.0;
        var wraps = _tumbler?.Wraps ?? false;
        var itemHeight = finalSize.Height / visible;
        var spacing = (visible + 1) * itemHeight / PathItemCount;
        var top = (finalSize.Height - itemHeight) / 2;
        for (var i = 0; i < count; i++)
        {
            var offset = i - position;
            if (wraps)
            {
                offset = ((offset % count) + count) % count;
                if (offset > count / 2.0)
                {
                    offset -= count;
                }
            }

            Children[i].Arrange(new Rect(0, top + (offset * spacing), finalSize.Width, itemHeight));
        }

        return finalSize;
    }

    private void OnTumblerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Tumbler.PositionProperty || e.Property == Tumbler.VisibleItemCountProperty || e.Property == Tumbler.IsWrapEnabledProperty)
        {
            InvalidateArrange();
        }
    }
}
