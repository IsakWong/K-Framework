using System.Collections.Generic;
using Framework.Foundation;
using UnityEngine;

/// <summary>
/// Central pool manager for GameObjects. Manages multiple GameObjectPools keyed by prefab.
/// Tracks instance-to-pool mapping so Release() works without knowing the prefab.
/// 
/// Usage:
///   // Get from pool (auto-creates pool if needed)
///   var enemy = PoolManager.Instance.Get(enemyPrefab, spawnPos, Quaternion.identity);
///   var vfx   = PoolManager.Instance.Get&lt;Vfx&gt;(vfxPrefab, pos, rot);
///
///   // Return to pool
///   PoolManager.Instance.Release(enemy);
///
///   // Pre-warm
///   PoolManager.Instance.Preload(bulletPrefab, 50);
///
///   // Via ServiceLocator
///   ServiceLocator.Get&lt;IPoolService&gt;().Get(prefab, pos, rot);
/// </summary>
public class PoolManager : PersistentSingleton<PoolManager>, IPoolService
{
    [SerializeField, Tooltip("Default max inactive objects per pool")]
    private int _defaultMaxSize = 100;

    [SerializeField, Tooltip("Auto-expand pools beyond max size")]
    private bool _defaultAutoExpand = true;

    // prefab InstanceID → pool
    private readonly Dictionary<int, GameObjectPool> _pools = new();
    // instance InstanceID → prefab InstanceID (for release without knowing prefab)
    private readonly Dictionary<int, int> _instanceToPool = new();

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IPoolService>(this);
    }

    #region Pool Registration

    /// <summary>
    /// Pre-register a pool for a prefab with optional pre-loading.
    /// </summary>
    public GameObjectPool RegisterPool(GameObject prefab, int preloadCount = 0, int maxSize = -1)
    {
        int prefabId = prefab.GetInstanceID();
        if (_pools.TryGetValue(prefabId, out var existing))
        {
            if (preloadCount > 0)
                existing.Preload(preloadCount);
            return existing;
        }

        int max = maxSize > 0 ? maxSize : _defaultMaxSize;
        var pool = new GameObjectPool(prefab, transform, preloadCount, max)
        {
            AutoExpand = _defaultAutoExpand
        };
        _pools[prefabId] = pool;
        return pool;
    }

    private GameObjectPool GetOrCreatePool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (!_pools.TryGetValue(prefabId, out var pool))
        {
            pool = new GameObjectPool(prefab, transform, 0, _defaultMaxSize)
            {
                AutoExpand = _defaultAutoExpand
            };
            _pools[prefabId] = pool;
        }
        return pool;
    }

    #endregion

    #region Get

    /// <summary>
    /// Get a GameObject from pool. Auto-creates pool if not registered.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab);
        var instance = pool.Get(position, rotation, parent);
        if (instance != null)
            _instanceToPool[instance.GetInstanceID()] = prefab.GetInstanceID();
        return instance;
    }

    /// <summary>
    /// Get a typed Component from pool.
    /// </summary>
    public T Get<T>(T prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
    {
        var go = Get(prefab.gameObject, position, rotation, parent);
        return go != null ? go.GetComponent<T>() : null;
    }

    #endregion

    #region Release

    /// <summary>
    /// Return a GameObject to its originating pool.
    /// If the instance was not obtained from a pool, it is destroyed.
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        int instanceId = instance.GetInstanceID();
        if (!_instanceToPool.TryGetValue(instanceId, out int prefabId))
        {
            EnhancedLog.Warning("Pool", $"'{instance.name}' was not from a pool. Destroying.");
            Object.Destroy(instance);
            return;
        }

        _instanceToPool.Remove(instanceId);

        if (_pools.TryGetValue(prefabId, out var pool))
        {
            pool.Release(instance);
        }
        else
        {
            Object.Destroy(instance);
        }
    }

    /// <summary>
    /// Return a Component's GameObject to its pool.
    /// </summary>
    public void Release(Component instance)
    {
        if (instance != null)
            Release(instance.gameObject);
    }

    #endregion

    #region Preload

    /// <summary>
    /// Pre-warm a pool with instances.
    /// </summary>
    public void Preload(GameObject prefab, int count)
    {
        RegisterPool(prefab, count);
    }

    /// <summary>
    /// Pre-warm a pool with typed prefab.
    /// </summary>
    public void Preload<T>(T prefab, int count) where T : Component
    {
        Preload(prefab.gameObject, count);
    }

    #endregion

    #region Clear

    /// <summary>
    /// Clear and destroy a specific pool.
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (!_pools.TryGetValue(prefabId, out var pool))
            return;

        pool.Clear();
        _pools.Remove(prefabId);

        // Clean up instance tracking for this pool
        var toRemove = ListPool<int>.Get();
        foreach (var kvp in _instanceToPool)
        {
            if (kvp.Value == prefabId)
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
            _instanceToPool.Remove(id);
        ListPool<int>.Release(toRemove);
    }

    /// <summary>
    /// Clear and destroy all pools.
    /// </summary>
    public void ClearAll()
    {
        foreach (var pool in _pools.Values)
            pool.Clear();
        _pools.Clear();
        _instanceToPool.Clear();
    }

    #endregion

    #region Query

    /// <summary>
    /// Check if an instance was obtained from a pool.
    /// </summary>
    public bool IsPooled(GameObject instance)
    {
        return instance != null && _instanceToPool.ContainsKey(instance.GetInstanceID());
    }

    /// <summary>
    /// Check if an instance was obtained from a pool.
    /// </summary>
    public bool IsPooled(Component instance)
    {
        return instance != null && IsPooled(instance.gameObject);
    }

    /// <summary>
    /// Get pool statistics string for debugging.
    /// </summary>
    public string GetStatistics()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[PoolManager] {_pools.Count} pools, {_instanceToPool.Count} tracked instances");
        foreach (var kvp in _pools)
        {
            var pool = kvp.Value;
            sb.AppendLine($"  {pool.Prefab.name}: Active={pool.ActiveCount} Inactive={pool.InactiveCount}");
        }
        return sb.ToString();
    }

    #endregion

    private void OnDestroy()
    {
        ClearAll();
    }
}
