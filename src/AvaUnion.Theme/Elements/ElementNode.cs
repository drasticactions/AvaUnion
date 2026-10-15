using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaUnion.Engine;

namespace AvaUnion.Elements;

/// <summary>
/// The Union element of an Avalonia element. It holds the type, id, states,
/// hints, attributes and resolved style.
/// </summary>
public sealed class ElementNode
{
    private static readonly IReadOnlyList<KeyValuePair<string, object>> s_noAttributes = [];

    private readonly StyledElement _owner;
    private StyledElement? _stateSource;
    private ElementHandle? _handle;
    private UnionStyleHandle? _styleHandle;
    private ResolvedStyle _resolved = ResolvedStyle.Unmatched;
    private bool _dirty = true;
    private bool _attached;
    private string _cacheKey = string.Empty;
    private int _epoch = -1;

    internal ElementNode(StyledElement owner)
    {
        _owner = owner;
    }

    /// <summary>The Avalonia element of this node.</summary>
    public StyledElement Owner => _owner;

    /// <summary>The lowercase Union type.</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>The id.</summary>
    public string Id { get; private set; } = string.Empty;

    /// <summary>The current states.</summary>
    public UnionStates States { get; private set; }

    /// <summary>Classes, explicit hints, automatic hints and state hints.</summary>
    public IReadOnlyList<string> Hints { get; private set; } = [];

    /// <summary>The attributes.</summary>
    public IReadOnlyList<KeyValuePair<string, object>> Attributes { get; private set; } = s_noAttributes;

    /// <summary>The key of all the data in this element that a query uses.</summary>
    public string CacheKey => _cacheKey;

    /// <summary>Occurs when <see cref="Resolved"/> changes to a different value.</summary>
    public event EventHandler? ResolvedChanged;

    /// <summary>The resolved style. If the node is dirty, it resolves the style now.</summary>
    public ResolvedStyle Resolved
    {
        get
        {
            if (_dirty || _epoch != UnionEnvironment.Engine.Epoch)
            {
                Resolve();
            }

            return _resolved;
        }
    }

    /// <summary>True when the element lays out right to left.</summary>
    public bool IsRightToLeft => _owner is Visual { FlowDirection: Avalonia.Media.FlowDirection.RightToLeft };

    /// <summary>The nearest typed node above this one.</summary>
    public ElementNode? Parent => ChainBuilder.FindParent(this);

    /// <summary>
    /// The chain as CSS-like text, for example <c>applicationwindow.inactive
    /// &gt; button:hovered.flat</c>.
    /// </summary>
    public string ChainText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var n in ChainBuilder.Build(this))
            {
                if (sb.Length > 0)
                {
                    sb.Append(" > ");
                }

                sb.Append(n.ToString());
            }

            return sb.ToString();
        }
    }

    internal ElementHandle Handle
    {
        get
        {
            if (_handle is null)
            {
                _handle = UnionEnvironment.Engine.CreateElement();
                Push();
            }

            return _handle;
        }
    }

    internal void Attach()
    {
        _attached = true;
        UnionEnvironment.Register(this);
        Rebind();
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        Compute();

        if (_owner is Visual v)
        {
            MarkSubtreeDirty(v, includeSelf: false, styleHandles: false);
        }

        Resolve();
    }

    internal void Detach()
    {
        _attached = false;
        _lastKey = null;
        _lastHandles = null;
        UnionEnvironment.Unregister(this);
        _owner.PropertyChanged -= OnOwnerPropertyChanged;
        _owner.Classes.CollectionChanged -= OnClassesChanged;
        if (_stateSource is not null && !ReferenceEquals(_stateSource, _owner))
        {
            _stateSource.Classes.CollectionChanged -= OnClassesChanged;
            _stateSource.PropertyChanged -= OnStateSourcePropertyChanged;
        }

        _stateSource = null;
        _handle?.Dispose();
        _handle = null;
    }

    internal void Rebind()
    {
        _owner.Classes.CollectionChanged -= OnClassesChanged;
        if (_stateSource is not null && !ReferenceEquals(_stateSource, _owner))
        {
            _stateSource.Classes.CollectionChanged -= OnClassesChanged;
            _stateSource.PropertyChanged -= OnStateSourcePropertyChanged;
        }

        _owner.Classes.CollectionChanged += OnClassesChanged;
        _stateSource = StateMapper.StateSourceOf(_owner);
        if (_stateSource is not null && !ReferenceEquals(_stateSource, _owner))
        {
            _stateSource.Classes.CollectionChanged += OnClassesChanged;
            _stateSource.PropertyChanged += OnStateSourcePropertyChanged;
        }

        if (_attached)
        {
            Refresh();
        }
    }

    /// <summary>Calculates the element description again. If it changed, the subtree resolves again.</summary>
    public void Refresh()
    {
        if (!_attached)
        {
            return;
        }

        if (Compute())
        {
            Push();
            if (_owner is Visual v)
            {
                MarkSubtreeDirty(v, includeSelf: true, styleHandles: false);
            }

            if (_owner is TopLevel)
            {
                UnionEnvironment.MarkPopupsDirty();
            }
        }
    }

    internal void InvalidateStyleHandle()
    {
        _styleHandle = null;
        MarkDirty();
    }

    private ResolvedEntry? _entry;

    private UnionStyleSlot? _slot;

    private string? _lastKey;
    private ElementHandle[]? _lastHandles;
    private UnionStyleHandle? _lastStyle;

    internal void MarkStale()
    {
        if (!_dirty)
        {
            UnionEnvironment.Schedule(this);
        }
    }

    internal void MarkDirty()
    {
        if (_dirty)
        {
            return;
        }

        _dirty = true;
        UnionEnvironment.Schedule(this);
    }

    internal static void MarkSubtreeDirty(Visual root, bool includeSelf, bool styleHandles)
    {
        if (includeSelf && root.GetValue(Union.NodeProperty) is { } self)
        {
            if (styleHandles)
            {
                self._styleHandle = null;
            }

            self.MarkDirty();
        }

        foreach (var child in root.GetVisualChildren())
        {
            MarkSubtreeDirty(child, includeSelf: true, styleHandles);
        }
    }

    internal void ResolveIfDirty()
    {
        if (_attached && (_dirty || _epoch != UnionEnvironment.Engine.Epoch))
        {
            Resolve();
        }
    }

    private void Resolve()
    {
        var structural = _dirty || _lastKey is null;
        _dirty = false;
        var engine = UnionEnvironment.Engine;
        _epoch = engine.Epoch;
        var style = StyleHandle(engine);
        if (structural || !ReferenceEquals(style, _lastStyle))
        {
            var chain = ChainBuilder.Build(this);
            var key = new StringBuilder(style.Id);
            var chainHandles = new ElementHandle[chain.Count];
            for (var i = 0; i < chain.Count; i++)
            {
                chainHandles[i] = chain[i].Handle;
                key.Append('\u001d').Append(chain[i]._cacheKey);
            }

            _lastKey = key.ToString();
            _lastHandles = chainHandles;
            _lastStyle = style;
        }

        var cacheKey = _lastKey!;
        var handles = _lastHandles!;
        if (!UnionEnvironment.Cache.TryGet(cacheKey, out var entry))
        {
            entry = UnionEnvironment.Intern(engine.Query(style, handles));
            UnionEnvironment.Cache.Set(cacheKey, entry);
        }

        var result = entry.Style;
        var rightToLeft = IsRightToLeft;
        var own = _slot is not null && ReferenceEquals(_owner.GetValue(Union.StyleProperty), _slot) && _slot.IsRightToLeft == rightToLeft;
        if (!own || !ReferenceEquals(entry, _entry))
        {
            var previous = _entry;
            _entry = entry;
            _resolved = result;
            if (own && previous is not null && previous.SameMetrics(entry))
            {
                _slot!.SetColors(result, cacheKey);
            }
            else
            {
                _slot = new UnionStyleSlot(result, cacheKey, rightToLeft);
                _owner.SetValue(Union.StyleProperty, _slot);
            }

            ResolvedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private UnionStyleHandle StyleHandle(IUnionEngine engine)
    {
        if (_styleHandle is not null)
        {
            return _styleHandle;
        }

        var id = _owner.GetValue(Union.StyleIdProperty) ?? UnionEnvironment.ThemeStyleId;
        UnionStyleHandle? handle = null;
        if (!string.IsNullOrEmpty(id))
        {
            handle = engine.GetStyle(id);
            if (handle is null)
            {
                UnionEnvironment.WarnMissingStyle(id);
            }
        }

        if (handle is null && engine.DefaultStyle.Id is "breeze" or "breeze-rtl")
        {
            var directed = IsRightToLeft ? "breeze-rtl" : "breeze";
            if (engine.DefaultStyle.Id != directed)
            {
                handle = engine.GetStyle(directed);
            }
        }

        return _styleHandle = handle ?? engine.DefaultStyle;
    }

    private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty && ReferenceEquals(_stateSource, _owner))
        {
            Refresh();
        }
        else if (AutoHints.IsWatched(e.Property))
        {
            Refresh();
        }
        else if (e.Property == Visual.FlowDirectionProperty)
        {
            InvalidateStyleHandle();
        }
    }

    private void OnStateSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty || AutoHints.IsWatched(e.Property))
        {
            Refresh();
        }
    }

    private bool Compute()
    {
        var type = (_owner.GetValue(Union.TypeProperty) ?? string.Empty).ToLowerInvariant();

        if (type == "toolseparator" && _owner is Separator && _owner.Parent is MenuBase or MenuItem or MenuFlyoutPresenter)
        {
            type = "menuseparator";
        }
        var id = _owner.GetValue(Union.IdProperty) ?? string.Empty;

        var hints = new List<string>();
        foreach (var c in _owner.Classes)
        {
            if (c.Length > 0 && c[0] != ':' && !hints.Contains(c))
            {
                hints.Add(c);
            }
        }

        AutoHints.AddSpaced(_owner.GetValue(Union.HintsProperty), hints);
        var attributes = new List<KeyValuePair<string, object>>();
        if (_owner.GetValue(Union.AttributesProperty) is { } explicitAttributes)
        {
            attributes.AddRange(explicitAttributes.Values);
        }

        if (_owner.GetValue(Union.ColorSetProperty) is var set and not UnionColorSet.None)
        {
            attributes.Add(new("colorset", set.ToString().ToLowerInvariant()));
        }

        AutoHints.Compute(_owner, type, hints, attributes);
        var states = StateMapper.Map(_stateSource, type, hints);

        var merged = new List<KeyValuePair<string, object>>(attributes.Count);
        foreach (var a in attributes)
        {
            if (!merged.Exists(m => string.Equals(m.Key, a.Key, StringComparison.Ordinal)))
            {
                merged.Add(a);
            }
        }

        var key = BuildKey(type, id, states, hints, merged);
        if (key == _cacheKey)
        {
            return false;
        }

        Type = type;
        Id = id;
        States = states;
        Hints = hints;
        Attributes = merged;
        _cacheKey = key;
        return true;
    }

    private void Push()
    {
        if (_handle is null)
        {
            return;
        }

        _handle.SetType(Type);
        _handle.SetId(Id);
        _handle.SetStates(States);
        _handle.SetHints(Hints);
        _handle.ClearAttributes();
        foreach (var (name, value) in Attributes)
        {
            if (value is double d)
            {
                _handle.SetAttribute(name, d);
            }
            else
            {
                _handle.SetAttribute(name, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }
        }
    }

    private static string BuildKey(string type, string id, UnionStates states, List<string> hints, List<KeyValuePair<string, object>> attributes)
    {
        var sb = new StringBuilder(type.Length + 32);
        sb.Append(type).Append('\u001f').Append(id).Append('\u001f').Append((int)states).Append('\u001f');
        foreach (var h in hints)
        {
            sb.Append(h).Append('\u001e');
        }

        sb.Append('\u001f');
        foreach (var (k, v) in attributes)
        {
            sb.Append(k).Append('=').Append(v is double d ? "n" + d.ToString("R", CultureInfo.InvariantCulture) : "s" + v).Append('\u001e');
        }

        return sb.ToString();
    }

    /// <summary>CSS-like text for this element.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Type.Length == 0 ? "*" : Type);
        if (Id.Length > 0)
        {
            sb.Append('#').Append(Id);
        }

        foreach (var h in Hints)
        {
            sb.Append('.').Append(h);
        }

        foreach (var (k, v) in Attributes)
        {
            sb.Append('[').Append(k).Append('=').Append(Convert.ToString(v, CultureInfo.InvariantCulture)).Append(']');
        }

        foreach (var state in Enum.GetValues<UnionStates>())
        {
            if (state != UnionStates.None && (States & state) != 0)
            {
                sb.Append(':').Append(UnionStateNames.Css(state));
            }
        }

        return sb.ToString();
    }
}
