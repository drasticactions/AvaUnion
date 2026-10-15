using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Media;
using AvaUnion.Elements;

namespace AvaUnion.Markup;

internal sealed class StyleAccessor : IPropertyAccessor
{
    private static readonly ConditionalWeakTable<AvaloniaObject, Subscribers> s_subscribers = new();

    private static readonly Dictionary<(string Key, Func<Engine.ResolvedStyle, object?> Leaf), SolidColorBrush> s_brushes = [];
    private SolidColorBrush? _ownBrush;
    private string? _brushKey;
    private SolidColorBrush? _keyBrush;
    private readonly WeakReference<object?> _target;
    private readonly Func<Engine.ResolvedStyle, object?>? _leaf;
    private Action<object?>? _listener;
    private object? _lastLeaf;
    private bool _lastRightToLeft;
    private bool _hasLast;

    static StyleAccessor()
    {
        Union.StyleProperty.Changed.AddClassHandler<AvaloniaObject>(static (o, _) =>
        {
            if (s_subscribers.TryGetValue(o, out var subscribers))
            {
                subscribers.Watch(o.GetValue(Union.StyleProperty));
                subscribers.PublishAll();
            }
        });
    }

    private StyleAccessor(WeakReference<object?> target, Func<Engine.ResolvedStyle, object?>? leaf)
    {
        _target = target;
        _leaf = leaf;
    }

    public static Func<WeakReference<object?>, IPropertyInfo, IPropertyAccessor> For(Func<Engine.ResolvedStyle, object?>? leaf) =>
        (target, _) => new StyleAccessor(target, leaf);

    private bool? _colorDependent;

    private static bool IsColorDependent(object? leaf) => leaf switch
    {
        null => true,
        Color => true,
        double or float or int or bool or string or Enum => false,
        Engine.ResolvedStyle.SizeGroup or Engine.ResolvedStyle.CornersGroup or Engine.ResolvedStyle.CornerGroup
            or Engine.ResolvedStyle.AlignmentGroup or Engine.ResolvedStyle.LayoutGroup or Engine.ResolvedStyle.OffsetGroup
            or Engine.ResolvedStyle.DisplayGroup or Engine.UnionFont => false,
        _ => true,
    };

    private void PublishColors(UnionStyleSlot? slot)
    {
        if (_colorDependent is false)
        {
            return;
        }

        Publish(slot);
    }

    private void Publish(UnionStyleSlot? slot)
    {
        var style = slot?.Current;
        if (_listener is not { } listener)
        {
            return;
        }

        if (_leaf is not null)
        {
            var leaf = style is null ? null : _leaf(style);
            if (style is not null)
            {
                _colorDependent = IsColorDependent(leaf);
            }

            var rightToLeft = slot?.IsRightToLeft ?? false;
            if (_hasLast && Equals(leaf, _lastLeaf) && rightToLeft == _lastRightToLeft && style is not null)
            {
                return;
            }

            if (_hasLast && Brush is not null && _lastLeaf is Color && leaf is Color color)
            {
                _lastLeaf = leaf;
                if (ReferenceEquals(BrushFor(slot?.Key, color), Brush))
                {
                    return;
                }
            }

            _lastLeaf = leaf;
            _lastRightToLeft = rightToLeft;
            _hasLast = style is not null;
        }

        listener(style is null ? null : new StyleValue(style, this, slot!.Key, slot.IsRightToLeft));
    }

    internal SolidColorBrush? Brush { get; set; }

    internal SolidColorBrush BrushFor(string? key, Color color)
    {
        SolidColorBrush? brush;

        if (key is not null && ReferenceEquals(key, _brushKey) && _keyBrush is not null)
        {
            brush = _keyBrush;
        }
        else if (key is null || _leaf is null)
        {
            brush = _ownBrush ??= new SolidColorBrush(color);
        }
        else if (!s_brushes.TryGetValue((key, _leaf), out brush))
        {
            if (s_brushes.Count > 8192)
            {
                s_brushes.Clear();
            }

            brush = new SolidColorBrush(color);
            s_brushes[(key, _leaf)] = brush;
        }

        if (key is not null && _leaf is not null)
        {
            _brushKey = key;
            _keyBrush = brush;
        }

        if (brush.Color != color)
        {
            brush.Color = color;
        }

        return brush;
    }

    private AvaloniaObject? Instance => _target.TryGetTarget(out var o) ? o as AvaloniaObject : null;

    public Type? PropertyType => typeof(StyleValue);

    public object? Value => Instance?.GetValue(Union.StyleProperty) is { } slot ? new StyleValue(slot.Current, this, slot.Key, slot.IsRightToLeft) : null;

    public bool SetValue(object? value, BindingPriority priority) => false;

    public void Subscribe(Action<object?> listener)
    {
        if (_listener is not null)
        {
            throw new InvalidOperationException("A StyleAccessor has one listener.");
        }

        _listener = listener;
        _hasLast = false;
        if (Instance is { } o)
        {
            var subscribers = s_subscribers.GetOrCreateValue(o);
            subscribers.Add(this);
            subscribers.Watch(o.GetValue(Union.StyleProperty));
            Publish(o.GetValue(Union.StyleProperty));
        }
    }

    public void Unsubscribe()
    {
        if (_listener is null)
        {
            return;
        }

        _listener = null;
        if (Instance is { } o && s_subscribers.TryGetValue(o, out var subscribers))
        {
            subscribers.Remove(this);
        }
    }

    public void Dispose() => Unsubscribe();

    private sealed class Subscribers
    {
        private readonly List<StyleAccessor> _accessors = [];
        private UnionStyleSlot? _slot;

        public void Add(StyleAccessor accessor) => _accessors.Add(accessor);

        public void Remove(StyleAccessor accessor)
        {
            _accessors.Remove(accessor);
            if (_accessors.Count == 0)
            {
                Watch(null);
            }
        }

        public void Watch(UnionStyleSlot? slot)
        {
            if (_accessors.Count == 0)
            {
                slot = null;
            }

            if (ReferenceEquals(slot, _slot))
            {
                return;
            }

            if (_slot is not null)
            {
                _slot.ColorsChanged -= OnColorsChanged;
            }

            _slot = slot;
            if (_slot is not null)
            {
                _slot.ColorsChanged += OnColorsChanged;
            }
        }

        public void PublishColors()
        {
            var slot = _slot;
            for (var i = _accessors.Count - 1; i >= 0; i--)
            {
                if (i < _accessors.Count)
                {
                    _accessors[i].PublishColors(slot);
                }
            }
        }

        public void PublishAll()
        {
            var slot = _slot;

            for (var i = _accessors.Count - 1; i >= 0; i--)
            {
                if (i < _accessors.Count)
                {
                    _accessors[i].Publish(slot);
                }
            }
        }

        private void OnColorsChanged(object? sender, EventArgs e) => PublishColors();
    }
}
