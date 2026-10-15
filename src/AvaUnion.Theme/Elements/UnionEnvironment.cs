using System;
using System.Collections.Generic;
using Avalonia.Logging;
using Avalonia.Threading;
using AvaUnion.Engine;
using AvaUnion.Engine.Managed;

namespace AvaUnion.Elements;

/// <summary>
/// The engine of the process, and the records of live nodes. It resolves
/// the nodes again after a session change.
/// </summary>
public static class UnionEnvironment
{
    private static readonly HashSet<ElementNode> s_live = [];
    private static readonly HashSet<ElementNode> s_dirty = [];
    private static readonly HashSet<string> s_warnedStyles = new(StringComparer.Ordinal);
    private static IUnionEngine? s_engine;
    private static bool s_flushQueued;

    /// <summary>True when an engine is installed.</summary>
    public static bool IsReady => s_engine is not null;

    /// <summary>The engine. If no engine is selected yet, this installs the managed engine.</summary>
    public static IUnionEngine Engine => s_engine ?? Install(EngineSelector.Select(RequestedEngine, null));

    /// <summary>The engine kind that <see cref="UnionTheme.Engine"/> requests.</summary>
    public static UnionEngineKind RequestedEngine { get; internal set; }

    /// <summary>The style id that <see cref="UnionTheme.StyleId"/> sets. Null follows the session.</summary>
    public static string? ThemeStyleId { get; internal set; }

    internal static LruCache<string, ResolvedEntry> Cache { get; } = new(2000);

    private static readonly Dictionary<ResolvedStyle, ResolvedEntry> s_interned = [];

    internal static ResolvedEntry Intern(ResolvedStyle style)
    {
        if (!s_interned.TryGetValue(style, out var entry))
        {
            if (s_interned.Count > 8192)
            {
                s_interned.Clear();
            }

            entry = new ResolvedEntry(style);
            s_interned[style] = entry;
        }

        return entry;
    }

    /// <summary>The number of live nodes.</summary>
    public static int LiveNodeCount => s_live.Count;

    /// <summary>Occurs after a session change, when all live nodes are resolved again.</summary>
    public static event Action<UnionChange>? Changed;

    internal static IUnionEngine Install(IUnionEngine engine)
    {
        if (s_engine is not null && !ReferenceEquals(s_engine, engine))
        {
            s_engine.Changed -= OnEngineChanged;
        }

        s_engine = engine;
        engine.Changed += OnEngineChanged;
        Union.EnsureRegistered();
        Markup.Scaled.Rescale();
        return engine;
    }

    internal static void Register(ElementNode node) => s_live.Add(node);

    internal static IReadOnlyCollection<ElementNode> LiveNodes => s_live;

    internal static void Unregister(ElementNode node)
    {
        s_live.Remove(node);
        s_dirty.Remove(node);
    }

    internal static void Schedule(ElementNode node)
    {
        s_dirty.Add(node);
        if (!s_flushQueued)
        {
            s_flushQueued = true;
            Dispatcher.UIThread.Post(Flush, DispatcherPriority.Render);
        }
    }

    /// <summary>Resolves every dirty node now.</summary>
    public static void Flush()
    {
        s_flushQueued = false;
        if (s_dirty.Count == 0)
        {
            return;
        }

        var nodes = new ElementNode[s_dirty.Count];
        s_dirty.CopyTo(nodes);
        s_dirty.Clear();
        foreach (var node in nodes)
        {
            node.ResolveIfDirty();
        }
    }

    internal static void MarkPopupsDirty()
    {
        foreach (var node in s_live)
        {
            if (node.Owner is Avalonia.Visual v && ChainBuilder.IsPopupRoot(v))
            {
                ElementNode.MarkSubtreeDirty(v, includeSelf: true, styleHandles: false);
            }
        }
    }

    internal static void RefreshPopupRootsOf(Avalonia.Controls.Primitives.Popup popup)
    {
        foreach (var node in new List<ElementNode>(s_live))
        {
            if (node.Owner is Avalonia.Visual v && ChainBuilder.IsPopupRoot(v) && ReferenceEquals(ChainBuilder.OwningPopup(v), popup))
            {
                node.Refresh();
            }
        }
    }

    internal static void InvalidateAll(bool styleHandles)
    {
        Cache.Clear();
        foreach (var node in s_live)
        {
            if (styleHandles)
            {
                node.InvalidateStyleHandle();
            }
            else
            {
                node.MarkStale();
            }
        }
    }

    internal static void WarnMissingStyle(string id)
    {
        if (s_warnedStyles.Add(id))
        {
            Logger.TryGet(LogEventLevel.Warning, "AvaUnion")?.Log(null, "Style {Style} not found; using the default style", id);
        }
    }

    private static void OnEngineChanged(UnionChange change)
    {
        if ((change & (UnionChange.Colors | UnionChange.Style)) != 0)
        {
            InvalidateAll(styleHandles: (change & UnionChange.Style) != 0);
        }

        Changed?.Invoke(change);
    }
}
