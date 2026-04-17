using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implement on MonoBehaviour components to receive pool lifecycle callbacks.
/// All IPoolable components on the GameObject (including children) will be notified.
/// </summary>
public interface IPoolable
{
    /// <summary>Called when the object is taken from the pool (after SetActive(true)).</summary>
    void OnGetFromPool();

    /// <summary>Called when the object is returned to the pool (before SetActive(false)).</summary>
    void OnReturnToPool();
}

/// <summary>
/// Object pool for a single GameObject prefab.
/// Manages instantiation, recycling, and IPoolable lifecycle callbacks.
/// </summary>
public class GameObjectPool
{
    private readonly GameObject _prefab;
    private readonly Transform _root;
    private readonly Stack<GameObject> _inactive = new();
    private readonly HashSet<GameObject> _active = new();

    public int MaxSize { get; set; }
    public bool AutoExpand { get; set; } = true;

    public int ActiveCount => _active.Count;
    public int InactiveCount => _inactive.Count;
    public int TotalCount => ActiveCount + InactiveCount;
    public GameObject Prefab => _prefab;

    /// <param name="prefab">The prefab to clone.</param>
    /// <param name="root">Parent for inactive objects. A child container is created under this.</param>
    /// <param name="preloadCount">Number of instances to pre-create.</param>
    /// <param name="maxSize">Maximum pooled (inactive) instances. Active instances are not limited.</param>
    public GameObjectPool(GameObject prefab, Transform root = null, int preloadCount = 0, int maxSize = 100)
    {
        _prefab = prefab;
        MaxSize = maxSize;

        // Create a container for inactive objects
        var container = new GameObject($"[Pool] {prefab.name}");
        if (root != null)
            container.transform.SetParent(root, false);
        else
            Object.DontDestroyOnLoad(container);
        _root = container.transform;

        if (preloadCount > 0)
            Preload(preloadCount);
    }

    /// <summary>
    /// Get an instance from the pool. Creates a new one if the pool is empty.
    /// </summary>
    public GameObject Get(Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        GameObject instance;

        while (_inactive.Count > 0)
        {
            instance = _inactive.Pop();
            if (instance != null)
            {
                SetupTransform(instance, position, rotation, parent);
                instance.SetActive(true);
                _active.Add(instance);
                NotifyGet(instance);
                return instance;
            }
            // Skip destroyed objects
        }

        // No available instance — create new
        if (!AutoExpand && TotalCount >= MaxSize)
        {
            EnhancedLog.Warning("Pool", $"Pool '{_prefab.name}' reached max size ({MaxSize}), cannot expand.");
            return null;
        }

        instance = Object.Instantiate(_prefab);
        instance.name = _prefab.name;
        SetupTransform(instance, position, rotation, parent);
        instance.SetActive(true);
        _active.Add(instance);
        NotifyGet(instance);
        return instance;
    }

    /// <summary>
    /// Return an instance to the pool. Calls IPoolable.OnReturnToPool before deactivating.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (!_active.Remove(instance))
        {
            EnhancedLog.Warning("Pool", $"Releasing '{instance.name}' that is not tracked as active by pool '{_prefab.name}'.");
            return;
        }

        NotifyReturn(instance);

        if (_inactive.Count < MaxSize)
        {
            instance.SetActive(false);
            instance.transform.SetParent(_root, false);
            _inactive.Push(instance);
        }
        else
        {
            Object.Destroy(instance);
        }
    }

    /// <summary>
    /// Pre-create instances and store them as inactive.
    /// </summary>
    public void Preload(int count)
    {
        for (int i = 0; i < count && InactiveCount < MaxSize; i++)
        {
            var instance = Object.Instantiate(_prefab, _root);
            instance.name = _prefab.name;
            instance.SetActive(false);
            _inactive.Push(instance);
        }
    }

    /// <summary>
    /// Destroy all instances (active and inactive) and clean up the container.
    /// </summary>
    public void Clear()
    {
        foreach (var go in _inactive)
            if (go != null) Object.Destroy(go);
        _inactive.Clear();

        foreach (var go in _active)
            if (go != null) Object.Destroy(go);
        _active.Clear();

        if (_root != null)
            Object.Destroy(_root.gameObject);
    }

    private static void SetupTransform(GameObject go, Vector3 position, Quaternion rotation, Transform parent)
    {
        var t = go.transform;
        if (parent != null)
        {
            t.SetParent(parent, false);
            t.localPosition = position;
            t.localRotation = rotation;
        }
        else
        {
            t.SetParent(null, false);
            t.SetPositionAndRotation(position, rotation);
        }
    }

    // Cache-friendly: avoid repeated GetComponents per call by using a shared list
    private static readonly List<IPoolable> _poolableCache = new(8);

    private static void NotifyGet(GameObject go)
    {
        go.GetComponentsInChildren(true, _poolableCache);
        for (int i = 0; i < _poolableCache.Count; i++)
            _poolableCache[i].OnGetFromPool();
        _poolableCache.Clear();
    }

    private static void NotifyReturn(GameObject go)
    {
        go.GetComponentsInChildren(true, _poolableCache);
        for (int i = 0; i < _poolableCache.Count; i++)
            _poolableCache[i].OnReturnToPool();
        _poolableCache.Clear();
    }
}