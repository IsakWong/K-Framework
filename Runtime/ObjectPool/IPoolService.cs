using UnityEngine;

/// <summary>
/// Service interface for the object pool system via ServiceLocator.
/// Provides GameObject/Component pooling with automatic instance tracking.
/// </summary>
public interface IPoolService
{
    /// <summary>Get a GameObject from pool.</summary>
    GameObject Get(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);

    /// <summary>Get a typed Component from pool.</summary>
    T Get<T>(T prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component;

    /// <summary>Return a GameObject to its pool.</summary>
    void Release(GameObject instance);

    /// <summary>Return a Component to its pool.</summary>
    void Release(Component instance);

    /// <summary>Pre-warm a pool with instances.</summary>
    void Preload(GameObject prefab, int count);

    /// <summary>Pre-warm a pool with typed prefab.</summary>
    void Preload<T>(T prefab, int count) where T : Component;

    /// <summary>Clear a specific pool.</summary>
    void ClearPool(GameObject prefab);

    /// <summary>Clear all pools.</summary>
    void ClearAll();

    /// <summary>Check if a GameObject was obtained from a pool.</summary>
    bool IsPooled(GameObject instance);

    /// <summary>Check if a Component was obtained from a pool.</summary>
    bool IsPooled(Component instance);
}
