using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public class AssetManager : KSingleton<AssetManager>, IAssetService
{
    // Cache for loaded assets to prevent duplicate loading
    private Dictionary<string, AsyncOperationHandle> _loadedHandles = new Dictionary<string, AsyncOperationHandle>();

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IAssetService>(this);
    }
    
    #region Path-based Loading
    
    /// <summary>
    /// 同步加载资源 (通过路径)
    /// </summary>
    public T LoadAsset<T>(string path) where T : Object
    {
        Debug.Log($"[AssetManager] Loading Asset: {path}");
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<T>(path);
#else
        if (_loadedHandles.TryGetValue(path, out var cached) && cached.IsValid() && cached.IsDone)
        {
            return cached.Result as T;
        }
        var handle = Addressables.LoadAssetAsync<T>(path);
        handle.WaitForCompletion();
        _loadedHandles[path] = handle;
        return handle.Result;
#endif
    }
    
    /// <summary>
    /// 异步加载资源 (通过路径)
    /// </summary>
    public async Task<T> LoadAssetAsync<T>(string path) where T : Object
    {
        Debug.Log($"[AssetManager] Loading Asset Async: {path}");
#if UNITY_EDITOR
        await Task.Yield();
        return AssetDatabase.LoadAssetAtPath<T>(path);
#else
        if (_loadedHandles.TryGetValue(path, out var cached) && cached.IsValid() && cached.IsDone)
        {
            return cached.Result as T;
        }
        var handle = Addressables.LoadAssetAsync<T>(path);
        await handle.Task;
        _loadedHandles[path] = handle;
        return handle.Result;
#endif
    }
    
    #endregion
    
    #region AssetReference Loading (Sync)
    
    /// <summary>
    /// 同步加载 AssetReference
    /// </summary>
    public T LoadAssetReference<T>(AssetReference assetReference) where T : Object
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReference");
            return null;
        }

#if UNITY_EDITOR
        // Editor模式下通过GUID加载
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[AssetManager] AssetReference GUID not found: {assetReference.AssetGUID}");
            return null;
        }
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
        // 检查是否已经加载
        if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.IsDone)
        {
            return assetReference.OperationHandle.Result as T;
        }
        
        // 同步加载
        var handle = assetReference.LoadAssetAsync<T>();
        handle.WaitForCompletion();
        return handle.Result;
#endif
    }
    
    /// <summary>
    /// 同步加载 AssetReferenceT
    /// </summary>
    public T LoadAsset<T>(AssetReferenceT<T> assetReference) where T : Object
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReferenceT");
            return null;
        }

#if UNITY_EDITOR
        // Editor模式下通过GUID加载
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[AssetManager] AssetReferenceT GUID not found: {assetReference.AssetGUID}");
            return null;
        }
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
        // 检查是否已经加载
        if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.IsDone)
        {
            return assetReference.OperationHandle.Result as T;
        }
        
        // 同步加载
        var handle = assetReference.LoadAssetAsync();
        handle.WaitForCompletion();
        return handle.Result;
#endif
    }
    
    #endregion
    
    #region AssetReference Loading (Async)
    
    /// <summary>
    /// 异步加载 AssetReference
    /// </summary>
    public async Task<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReference");
            return null;
        }

#if UNITY_EDITOR
        // Editor模式下通过GUID加载
        await Task.Yield();
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[AssetManager] AssetReference GUID not found: {assetReference.AssetGUID}");
            return null;
        }
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
        // 检查是否已经加载
        if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.IsDone)
        {
            return assetReference.OperationHandle.Result as T;
        }
        
        // 异步加载
        var handle = assetReference.LoadAssetAsync<T>();
        await handle.Task;
        return handle.Result;
#endif
    }
    
    /// <summary>
    /// 异步加载 AssetReferenceT
    /// </summary>
    public async Task<T> LoadAssetAsync<T>(AssetReferenceT<T> assetReference) where T : Object
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReferenceT");
            return null;
        }

#if UNITY_EDITOR
        // Editor模式下通过GUID加载
        await Task.Yield();
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning($"[AssetManager] AssetReferenceT GUID not found: {assetReference.AssetGUID}");
            return null;
        }
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
#else
        // 检查是否已经加载
        if (assetReference.OperationHandle.IsValid() && assetReference.OperationHandle.IsDone)
        {
            return assetReference.OperationHandle.Result as T;
        }
        
        // 异步加载
        var handle = assetReference.LoadAssetAsync();
        await handle.Task;
        return handle.Result;
#endif
    }
    
    #endregion
    
    #region AssetReference Instantiate
    
    /// <summary>
    /// 同步实例化 AssetReference GameObject
    /// </summary>
    public GameObject InstantiateAssetReference(AssetReference assetReference, Transform parent = null)
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReference for instantiate");
            return null;
        }

#if UNITY_EDITOR
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[AssetManager] Failed to load prefab: {assetPath}");
            return null;
        }
        return parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
#else
        var handle = assetReference.InstantiateAsync(parent);
        handle.WaitForCompletion();
        return handle.Result;
#endif
    }
    
    /// <summary>
    /// 异步实例化 AssetReference GameObject
    /// </summary>
    public async Task<GameObject> InstantiateAssetReferenceAsync(AssetReference assetReference, Transform parent = null)
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogWarning("[AssetManager] Invalid AssetReference for instantiate");
            return null;
        }

#if UNITY_EDITOR
        await Task.Yield();
        string assetPath = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[AssetManager] Failed to load prefab: {assetPath}");
            return null;
        }
        return parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
#else
        var handle = assetReference.InstantiateAsync(parent);
        await handle.Task;
        return handle.Result;
#endif
    }
    
    #endregion
    
    #region Release Assets
    
    /// <summary>
    /// 释放 AssetReference
    /// </summary>
    public void ReleaseAssetReference(AssetReference assetReference)
    {
        if (assetReference == null)
            return;

#if UNITY_EDITOR
        // Editor模式下不需要释放
        return;
#else
        if (assetReference.OperationHandle.IsValid())
        {
            assetReference.ReleaseAsset();
        }
#endif
    }
    
    /// <summary>
    /// 释放通过路径加载的资源
    /// </summary>
    public void ReleaseAsset(string path)
    {
#if UNITY_EDITOR
        // Editor模式下不需要释放
        return;
#else
        if (_loadedHandles.TryGetValue(path, out var handle))
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            _loadedHandles.Remove(path);
        }
#endif
    }
    
    /// <summary>
    /// 释放所有已加载的资源
    /// </summary>
    public void ReleaseAll()
    {
#if UNITY_EDITOR
        // Editor模式下不需要释放
        return;
#else
        foreach (var handle in _loadedHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        _loadedHandles.Clear();
#endif
    }
    
    #endregion
}