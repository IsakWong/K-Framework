using System;
using System.Collections.Generic;

/// <summary>
/// Generic object pool for pure C# objects (no Unity dependencies).
/// Use for events, data containers, computation contexts, etc.
/// </summary>
/// <typeparam name="T">Must be a reference type.</typeparam>
public class CSharpPool<T> where T : class
{
    private readonly Stack<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly Action<T> _onDestroy;
    private readonly int _maxSize;

    public int CountInactive => _pool.Count;

    /// <param name="factory">Factory function to create new instances.</param>
    /// <param name="onGet">Called when an object is taken from the pool.</param>
    /// <param name="onRelease">Called when an object is returned to the pool.</param>
    /// <param name="onDestroy">Called when an object is discarded (pool at max capacity).</param>
    /// <param name="defaultCapacity">Initial stack capacity.</param>
    /// <param name="maxSize">Maximum number of pooled (inactive) objects.</param>
    public CSharpPool(
        Func<T> factory,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        Action<T> onDestroy = null,
        int defaultCapacity = 10,
        int maxSize = 1000)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _pool = new Stack<T>(defaultCapacity);
        _onGet = onGet;
        _onRelease = onRelease;
        _onDestroy = onDestroy;
        _maxSize = maxSize;
    }

    public T Get()
    {
        T item = _pool.Count > 0 ? _pool.Pop() : _factory();
        _onGet?.Invoke(item);
        return item;
    }

    public void Release(T item)
    {
        if (item == null) return;

        _onRelease?.Invoke(item);

        if (_pool.Count < _maxSize)
        {
            _pool.Push(item);
        }
        else
        {
            _onDestroy?.Invoke(item);
        }
    }

    public void Clear()
    {
        if (_onDestroy != null)
        {
            while (_pool.Count > 0)
                _onDestroy(_pool.Pop());
        }
        else
        {
            _pool.Clear();
        }
    }

    /// <summary>
    /// Pre-fill the pool with instances.
    /// </summary>
    public void Preload(int count)
    {
        for (int i = 0; i < count && _pool.Count < _maxSize; i++)
            _pool.Push(_factory());
    }
}

/// <summary>
/// Shared pool for List&lt;T&gt; instances. Automatically clears on return.
/// </summary>
public static class ListPool<T>
{
    private static readonly CSharpPool<List<T>> Pool = new(
        factory: () => new List<T>(),
        onRelease: list => list.Clear()
    );

    public static List<T> Get() => Pool.Get();
    public static void Release(List<T> list) => Pool.Release(list);
}

/// <summary>
/// Shared pool for Dictionary&lt;TKey, TValue&gt; instances. Automatically clears on return.
/// </summary>
public static class DictionaryPool<TKey, TValue>
{
    private static readonly CSharpPool<Dictionary<TKey, TValue>> Pool = new(
        factory: () => new Dictionary<TKey, TValue>(),
        onRelease: dict => dict.Clear()
    );

    public static Dictionary<TKey, TValue> Get() => Pool.Get();
    public static void Release(Dictionary<TKey, TValue> dict) => Pool.Release(dict);
}

/// <summary>
/// Shared pool for HashSet&lt;T&gt; instances. Automatically clears on return.
/// </summary>
public static class HashSetPool<T>
{
    private static readonly CSharpPool<HashSet<T>> Pool = new(
        factory: () => new HashSet<T>(),
        onRelease: set => set.Clear()
    );

    public static HashSet<T> Get() => Pool.Get();
    public static void Release(HashSet<T> set) => Pool.Release(set);
}
