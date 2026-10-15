using System.Collections.Generic;

namespace AvaUnion.Engine.Managed;

/// <summary>A small least-recently-used cache.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
public sealed class LruCache<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _order = new();

    /// <summary>Creates a cache holding at most <paramref name="capacity"/> entries.</summary>
    public LruCache(int capacity)
    {
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
    }

    /// <summary>The number of entries.</summary>
    public int Count => _map.Count;

    /// <summary>Looks up a value and marks it as most recently used.</summary>
    public bool TryGet(TKey key, out TValue value)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _order.Remove(node);
            _order.AddFirst(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Adds or replaces a value. When the cache is full, it removes the least recently used entry.</summary>
    public void Set(TKey key, TValue value)
    {
        if (_map.TryGetValue(key, out var existing))
        {
            _order.Remove(existing);
            _map.Remove(key);
        }
        else if (_map.Count >= _capacity && _order.Last is { } last)
        {
            _order.RemoveLast();
            _map.Remove(last.Value.Key);
        }

        _map[key] = _order.AddFirst((key, value));
    }

    /// <summary>Removes all entries.</summary>
    public void Clear()
    {
        _map.Clear();
        _order.Clear();
    }
}
