using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

/// <summary>
/// 资源服务：阶段批量加载 + 按路径读取。
///
/// 游戏资源挂在配置上以直接引用存在，随所属 bundle 的依赖一起加载，
/// 运行期不经过本服务；这里只负责「把某个阶段的资源拉进内存」和
/// 「配置 / UI 这类按路径读取的框架资产」。
/// </summary>
public class AssetService : KSingleton<AssetService>, IAssetService
{
    /// <summary>路径 → 资产。</summary>
    private readonly Dictionary<string, Object> _byPath = new();

    /// <summary>已完成的批次：label → 资产列表。</summary>
    private readonly Dictionary<string, IList<Object>> _labelAssets = new();

    /// <summary>批次持有的句柄，ReleaseLabel 时统一释放。</summary>
    private readonly Dictionary<string, List<AsyncOperationHandle>> _labelHandles = new();

    /// <summary>进行中的批次，并发调用共用同一次加载。</summary>
    private readonly Dictionary<string, Task> _labelLoading = new();

    protected override void OnServiceInit()
    {
        ServiceLocator.Register<IAssetService>(this);
    }

    #region 阶段加载

    public bool IsLabelLoaded(string label)
        => !string.IsNullOrEmpty(label) && _labelHandles.ContainsKey(label);

    public async Task<IList<T>> LoadAssetsAsync<T>(string label, Action<int, int> onProgress = null) where T : Object
    {
        if (string.IsNullOrEmpty(label))
        {
            Debug.LogWarning("[AssetService] 批次 label 为空");
            return Array.Empty<T>();
        }

        if (!_labelAssets.ContainsKey(label))
        {
            if (_labelLoading.TryGetValue(label, out var inFlight))
            {
                await inFlight;
            }
            else
            {
                var task = LoadLabelAsync(label, onProgress);
                _labelLoading[label] = task;
                await task;
                _labelLoading.Remove(label);
            }
        }

        return _labelAssets.TryGetValue(label, out var loaded) ? CastList<T>(loaded) : Array.Empty<T>();
    }

    private async Task LoadLabelAsync(string label, Action<int, int> onProgress)
    {
#if UNITY_EDITOR
        // Editor 下游戏资源是直接引用，随配置天然可用，无需真正加载。
        await Task.Yield();
        onProgress?.Invoke(1, 1);
        _labelAssets[label] = Array.Empty<Object>();
        _labelHandles[label] = new List<AsyncOperationHandle>();
        return;
#else
        var locationHandle = Addressables.LoadResourceLocationsAsync(label, typeof(Object));
        IList<IResourceLocation> locations;
        try
        {
            locations = await locationHandle.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AssetService] 批次 '{label}' 找不到资源: {e.Message}");
            if (locationHandle.IsValid()) Addressables.Release(locationHandle);
            return;
        }

        if (locations == null || locations.Count == 0)
        {
            Debug.LogWarning($"[AssetService] 批次 '{label}' 为空（label 是否已挂到条目上？）");
            if (locationHandle.IsValid()) Addressables.Release(locationHandle);
            _labelAssets[label] = Array.Empty<Object>();
            _labelHandles[label] = new List<AsyncOperationHandle>();
            return;
        }

        var results = new List<Object>(locations.Count);
        var handles = new List<AsyncOperationHandle>(locations.Count);
        var handleLocations = new List<IResourceLocation>(locations.Count);
        Debug.Log($"[AssetService] 批次 '{label}': {locations.Count} 个地址");

        for (int i = 0; i < locations.Count; i++)
        {
            // 场景由 SceneManager 管理，不能当普通资产加载。
            if (locations[i].ResourceType == typeof(UnityEngine.SceneManagement.Scene))
                continue;

            handleLocations.Add(locations[i]);
            handles.Add(Addressables.LoadAssetAsync<Object>(locations[i]));
        }

        // 一次性把所有加载操作排进 ResourceManager 再统一等待：
        // 逐个 await 会让每个资源至少占用一帧（WebGL 上上千个资源就是数十秒）。
        await WaitAllAsync(handles, onProgress);

        for (int i = 0; i < handles.Count; i++)
        {
            var handle = handles[i];
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogWarning($"[AssetService] 批次 '{label}' 中资源加载失败: {handleLocations[i].PrimaryKey}");
                continue;
            }

            results.Add(handle.Result as Object);
            if (handleLocations[i].PrimaryKey is string primaryKey)
                _byPath[primaryKey] = handle.Result as Object;
        }

        _labelAssets[label] = results;
        _labelHandles[label] = handles;
        Addressables.Release(locationHandle);
#endif
    }

    /// <summary>等待一批加载操作完成，期间按完成数量回报进度。</summary>
    private static async Task WaitAllAsync(List<AsyncOperationHandle> handles, Action<int, int> onProgress)
    {
        if (handles.Count == 0)
        {
            onProgress?.Invoke(0, 0);
            return;
        }

        while (true)
        {
            int completed = 0;
            for (int i = 0; i < handles.Count; i++)
                if (handles[i].IsDone || !handles[i].IsValid()) completed++;

            onProgress?.Invoke(completed, handles.Count);
            if (completed >= handles.Count) break;

            await Task.Yield();
        }
    }

    public void ReleaseLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return;

        if (_labelHandles.TryGetValue(label, out var handles))
        {
            foreach (var handle in handles)
                if (handle.IsValid()) Addressables.Release(handle);
            _labelHandles.Remove(label);
        }

        if (_labelAssets.TryGetValue(label, out var assets))
        {
            var keysToRemove = new List<string>();
            foreach (var pair in _byPath)
                foreach (var asset in assets)
                    if (ReferenceEquals(pair.Value, asset))
                    {
                        keysToRemove.Add(pair.Key);
                        break;
                    }

            foreach (var key in keysToRemove) _byPath.Remove(key);
            _labelAssets.Remove(label);
        }
    }

    private static IList<T> CastList<T>(IList<Object> source) where T : Object
    {
        var result = new List<T>(source.Count);
        foreach (var asset in source)
            if (asset is T typed) result.Add(typed);
        return result;
    }

    #endregion

    #region 路径读取

    public async Task<T> LoadAsync<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (_byPath.TryGetValue(path, out var cached)) return cached as T;

#if UNITY_EDITOR
        await Task.Yield();
        var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (editorAsset != null) _byPath[path] = editorAsset;
        return editorAsset;
#else
        var handle = Addressables.LoadAssetAsync<T>(path);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"[AssetService] 路径加载失败: {path} ({handle.Status})");
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }

        _byPath[path] = handle.Result;
        return handle.Result;
#endif
    }

    public T Load<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException("[AssetService] 路径为空");

        if (_byPath.TryGetValue(path, out var asset) && asset is T typed)
            return typed;

#if UNITY_EDITOR
        // Editor 下配置 / UI 允许即时读取；游戏资源走直接引用，不受影响。
        var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (editorAsset != null)
        {
            _byPath[path] = editorAsset;
            return editorAsset;
        }
#endif

        throw new InvalidOperationException(
            $"[AssetService] 资源未预热: {path}。请把它所在的分组加入对应阶段的 label。");
    }

    public bool TryLoad<T>(string path, out T value) where T : Object
    {
        value = null;
        if (string.IsNullOrEmpty(path)) return false;

        if (_byPath.TryGetValue(path, out var asset) && asset is T typed)
        {
            value = typed;
            return true;
        }

#if UNITY_EDITOR
        value = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (value == null) return false;
        _byPath[path] = value;
        return true;
#else
        return false;
#endif
    }

    #endregion

    #region 释放

    public void ReleaseAll()
    {
        foreach (var handles in _labelHandles.Values)
            foreach (var handle in handles)
                if (handle.IsValid()) Addressables.Release(handle);

        _labelHandles.Clear();
        _labelAssets.Clear();
        _labelLoading.Clear();
        _byPath.Clear();
    }

    #endregion
}
