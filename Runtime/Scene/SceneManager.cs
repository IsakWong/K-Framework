using System;
using System.Collections;
using System.Collections.Generic;
using Framework.Foundation;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景管理器 — 集中管理场景加载、卸载、切换和叠加
/// 使用 PersistentSingleton 保证跨场景存活
/// </summary>
public class SceneManager : PersistentSingleton<SceneManager>, ISceneService
{
    // ─── 状态 ───

    /// <summary>当前是否正在加载场景</summary>
    public bool IsLoading { get; private set; }

    /// <summary>加载进度 0~1</summary>
    public float Progress { get; private set; }

    /// <summary>当前主场景信息</summary>
    public SceneInfo CurrentScene { get; private set; }

    // ─── 信号 ───

    /// <summary>场景开始加载时触发（参数：目标场景名）</summary>
    public KSignal<string> OnSceneLoadBegin { get; } = new();

    /// <summary>加载进度更新（参数：0~1 进度值）</summary>
    public KSignal<float> OnSceneLoadProgress { get; } = new();

    /// <summary>场景加载完成时触发（参数：场景名）</summary>
    public KSignal<string> OnSceneLoadComplete { get; } = new();

    /// <summary>场景卸载完成时触发（参数：场景名）</summary>
    public KSignal<string> OnSceneUnloaded { get; } = new();

    /// <summary>发生错误时触发（参数：错误信息）</summary>
    public KSignal<string> OnSceneLoadError { get; } = new();

    // ─── 场景历史 ───

    private readonly Stack<SceneInfo> _history = new();

    /// <summary>是否可以返回上一场景</summary>
    public bool CanGoBack => _history.Count > 0;

    // ─── 叠加场景跟踪 ───

    private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _additiveScenes = new();

    // ─── 配置 ───

    [Header("场景切换配置")]
    [Tooltip("切换主场景时是否自动关闭所有 Module")]
    public bool ShutdownModulesOnSwitch = true;

    [Tooltip("切换主场景时是否自动将 GameMode 置空")]
    public bool ResetGameModeOnSwitch = true;

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<ISceneService>(this);
    }

    // ═══════════════════════════════════════════════════════════════
    //  主场景切换 — Addressables
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 切换主场景（通过 AssetReference）
    /// </summary>
    public void LoadScene(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null, bool recordHistory = true)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[SceneManager] 已有场景正在加载，忽略重复请求");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneRef, persistentData, onComplete, recordHistory));
    }

    /// <summary>
    /// 切换主场景的协程（可配合 LoadingPanel 使用）
    /// </summary>
    public IEnumerator LoadSceneCoroutine(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null, bool recordHistory = true)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[SceneManager] 已有场景正在加载，忽略重复请求");
            yield break;
        }

        IsLoading = true;
        Progress = 0f;

        var targetName = sceneRef?.AssetGUID ?? "unknown";
        OnSceneLoadBegin.Invoke(targetName);
        EnhancedLog.Info("Scene", $"开始加载场景: {targetName}");

        // 1. 记录历史
        if (recordHistory && CurrentScene != null)
        {
            _history.Push(CurrentScene);
        }

        // 2. 切换 GameMode
        if (ResetGameModeOnSwitch)
        {
            KGameCore.Instance.SwitchGameMode(null);
        }

        // 3. 关闭模块
        if (ShutdownModulesOnSwitch)
        {
            yield return KGameCore.Instance.proxy.ShutdownModules();
        }

        Progress = 0.2f;
        OnSceneLoadProgress.Invoke(Progress);
        Transition?.ReportProgress(Progress);

        // 4. 通过 Addressables 加载场景
        var loadHandle = Addressables.LoadSceneAsync(sceneRef, activateOnLoad: false);

        while (!loadHandle.IsDone)
        {
            Progress = 0.2f + loadHandle.PercentComplete * 0.6f;
            OnSceneLoadProgress.Invoke(Progress);
            Transition?.ReportProgress(Progress);
            yield return null;
        }

        if (loadHandle.Status != AsyncOperationStatus.Succeeded)
        {
            var error = $"场景加载失败: {loadHandle.OperationException?.Message ?? "Unknown error"}";
            Debug.LogError($"[SceneManager] {error}");
            OnSceneLoadError.Invoke(error);
            IsLoading = false;
            yield break;
        }

        Progress = 0.8f;
        OnSceneLoadProgress.Invoke(Progress);
        Transition?.ReportProgress(Progress);

        // 5. 传递 PersistentData 给新场景的 GameMode
        if (persistentData != null)
        {
            persistentData.SceneAddressablePath = sceneRef.AssetGUID;
            var gameModes = FindObjectsByType<GameMode>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (var gm in gameModes)
            {
                gm.PersistentData = persistentData;
            }
        }

        // 6. 激活场景
        var activateOp = loadHandle.Result.ActivateAsync();
        activateOp.allowSceneActivation = true;
        yield return activateOp;

        // 7. 更新当前场景信息
        CurrentScene = new SceneInfo
        {
            SceneName = loadHandle.Result.Scene.name,
            SceneRef = sceneRef,
            LoadHandle = loadHandle,
            PersistentData = persistentData
        };

        Progress = 1f;
        OnSceneLoadProgress.Invoke(Progress);
        Transition?.ReportProgress(Progress);
        IsLoading = false;

        EnhancedLog.Info("Scene", $"场景加载完成: {CurrentScene.SceneName}");
        OnSceneLoadComplete.Invoke(CurrentScene.SceneName);
        onComplete?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════
    //  主场景切换 — Build Index / 场景名
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 通过场景名切换主场景（使用 Unity 内置 SceneManager）
    /// </summary>
    public void LoadSceneByName(string sceneName, Action onComplete = null, bool recordHistory = true)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[SceneManager] 已有场景正在加载，忽略重复请求");
            return;
        }

        StartCoroutine(LoadSceneByNameCoroutine(sceneName, onComplete, recordHistory));
    }

    /// <summary>
    /// 通过场景名切换主场景的协程
    /// </summary>
    public IEnumerator LoadSceneByNameCoroutine(string sceneName, Action onComplete = null, bool recordHistory = true)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[SceneManager] 已有场景正在加载，忽略重复请求");
            yield break;
        }

        IsLoading = true;
        Progress = 0f;
        OnSceneLoadBegin.Invoke(sceneName);

        if (recordHistory && CurrentScene != null)
        {
            _history.Push(CurrentScene);
        }

        if (ResetGameModeOnSwitch)
        {
            KGameCore.Instance.SwitchGameMode(null);
        }

        if (ShutdownModulesOnSwitch)
        {
            yield return KGameCore.Instance.proxy.ShutdownModules();
        }

        var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        if (asyncOp == null)
        {
            var error = $"无法加载场景: {sceneName}（场景未添加到 Build Settings）";
            Debug.LogError($"[SceneManager] {error}");
            OnSceneLoadError.Invoke(error);
            IsLoading = false;
            yield break;
        }

        asyncOp.allowSceneActivation = false;

        while (asyncOp.progress < 0.9f)
        {
            Progress = asyncOp.progress;
            OnSceneLoadProgress.Invoke(Progress);
            Transition?.ReportProgress(Progress);
            yield return null;
        }

        Progress = 0.9f;
        OnSceneLoadProgress.Invoke(Progress);
        Transition?.ReportProgress(Progress);

        asyncOp.allowSceneActivation = true;
        yield return asyncOp;

        CurrentScene = new SceneInfo
        {
            SceneName = sceneName,
            SceneRef = null,
            PersistentData = null
        };

        Progress = 1f;
        OnSceneLoadProgress.Invoke(Progress);
        Transition?.ReportProgress(Progress);
        IsLoading = false;

        EnhancedLog.Info("Scene", $"场景加载完成: {sceneName}");
        OnSceneLoadComplete.Invoke(sceneName);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 通过 Build Index 切换主场景
    /// </summary>
    public void LoadSceneByIndex(int buildIndex, Action onComplete = null, bool recordHistory = true)
    {
        var scenePath = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(buildIndex).name;
        if (string.IsNullOrEmpty(scenePath))
        {
            // GetSceneByBuildIndex returns empty name for unloaded scenes; use path from build settings
            scenePath = System.IO.Path.GetFileNameWithoutExtension(
                UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex));
        }

        LoadSceneByName(scenePath, onComplete, recordHistory);
    }

    // ═══════════════════════════════════════════════════════════════
    //  场景历史 / 返回
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 返回上一场景
    /// </summary>
    public void GoBack(Action onComplete = null)
    {
        if (!CanGoBack)
        {
            Debug.LogWarning("[SceneManager] 没有可返回的场景");
            return;
        }

        var prev = _history.Pop();

        if (prev.SceneRef != null)
        {
            LoadScene(prev.SceneRef, prev.PersistentData, onComplete, recordHistory: false);
        }
        else
        {
            LoadSceneByName(prev.SceneName, onComplete, recordHistory: false);
        }
    }

    /// <summary>
    /// 清空场景历史
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  叠加场景（Additive）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 叠加加载场景（Addressables）
    /// </summary>
    public void LoadAdditiveScene(AssetReference sceneRef, Action<string> onComplete = null)
    {
        StartCoroutine(LoadAdditiveSceneCoroutine(sceneRef, onComplete));
    }

    /// <summary>
    /// 叠加加载场景协程
    /// </summary>
    public IEnumerator LoadAdditiveSceneCoroutine(AssetReference sceneRef, Action<string> onComplete = null)
    {
        var key = sceneRef.AssetGUID;
        if (_additiveScenes.ContainsKey(key))
        {
            Debug.LogWarning($"[SceneManager] 叠加场景已加载: {key}");
            yield break;
        }

        var handle = Addressables.LoadSceneAsync(sceneRef, LoadSceneMode.Additive);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var sceneName = handle.Result.Scene.name;
            _additiveScenes[key] = handle;
            EnhancedLog.Info("Scene", $"叠加场景加载完成: {sceneName}");
            onComplete?.Invoke(sceneName);
        }
        else
        {
            Debug.LogError($"[SceneManager] 叠加场景加载失败: {handle.OperationException?.Message}");
            OnSceneLoadError.Invoke($"叠加场景加载失败: {key}");
        }
    }

    /// <summary>
    /// 叠加加载场景（通过名称，使用内置 SceneManager）
    /// </summary>
    public IEnumerator LoadAdditiveSceneByNameCoroutine(string sceneName, Action onComplete = null)
    {
        var asyncOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncOp == null)
        {
            Debug.LogError($"[SceneManager] 无法叠加加载场景: {sceneName}");
            yield break;
        }

        yield return asyncOp;
        EnhancedLog.Info("Scene", $"叠加场景加载完成: {sceneName}");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 卸载叠加场景（Addressables）
    /// </summary>
    public IEnumerator UnloadAdditiveSceneCoroutine(AssetReference sceneRef, Action onComplete = null)
    {
        var key = sceneRef.AssetGUID;
        if (!_additiveScenes.TryGetValue(key, out var handle))
        {
            Debug.LogWarning($"[SceneManager] 叠加场景未找到: {key}");
            yield break;
        }

        var unloadHandle = Addressables.UnloadSceneAsync(handle);
        yield return unloadHandle;

        _additiveScenes.Remove(key);
        EnhancedLog.Info("Scene", $"叠加场景已卸载: {key}");
        OnSceneUnloaded.Invoke(key);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 卸载叠加场景（通过名称）
    /// </summary>
    public IEnumerator UnloadAdditiveSceneByNameCoroutine(string sceneName, Action onComplete = null)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"[SceneManager] 场景未加载: {sceneName}");
            yield break;
        }

        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        EnhancedLog.Info("Scene", $"叠加场景已卸载: {sceneName}");
        OnSceneUnloaded.Invoke(sceneName);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 卸载所有叠加场景
    /// </summary>
    public IEnumerator UnloadAllAdditiveScenes()
    {
        var keys = new List<string>(_additiveScenes.Keys);
        foreach (var key in keys)
        {
            if (_additiveScenes.TryGetValue(key, out var handle) && handle.IsValid())
            {
                var unloadHandle = Addressables.UnloadSceneAsync(handle);
                yield return unloadHandle;
            }
        }

        _additiveScenes.Clear();
        EnhancedLog.Info("Scene", "所有叠加场景已卸载");
    }

    // ═══════════════════════════════════════════════════════════════
    //  过渡效果（Loading Screen）— 通过接口解耦，框架不依赖具体 UI
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 当前使用的场景过渡效果提供者
    /// 业务层通过 SetTransition() 注入具体实现（如 UILoadingPanel）
    /// 若未设置，LoadSceneWithTransition 会直接加载（无过渡效果）
    /// </summary>
    public ISceneTransition Transition { get; private set; }

    /// <summary>
    /// 注入场景过渡效果实现
    /// </summary>
    public void SetTransition(ISceneTransition transition)
    {
        Transition = transition;
    }

    /// <summary>
    /// 带过渡效果切换场景（Addressables）
    /// 如果未注入 ISceneTransition，直接加载无过渡
    /// </summary>
    public void LoadSceneWithTransition(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null)
    {
        if (Transition != null)
        {
            Transition.BeginTransition(
                LoadSceneCoroutine(sceneRef, persistentData, onComplete)
            );
        }
        else
        {
            LoadScene(sceneRef, persistentData, onComplete);
        }
    }

    /// <summary>
    /// 带过渡效果切换场景（通过场景名）
    /// </summary>
    public void LoadSceneWithTransition(string sceneName, Action onComplete = null)
    {
        if (Transition != null)
        {
            Transition.BeginTransition(
                LoadSceneByNameCoroutine(sceneName, onComplete)
            );
        }
        else
        {
            LoadSceneByName(sceneName, onComplete);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  重载当前场景
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene(Action onComplete = null)
    {
        if (CurrentScene == null)
        {
            Debug.LogWarning("[SceneManager] 没有当前场景信息，无法重载");
            return;
        }

        if (CurrentScene.SceneRef != null)
        {
            LoadScene(CurrentScene.SceneRef, CurrentScene.PersistentData, onComplete, recordHistory: false);
        }
        else
        {
            LoadSceneByName(CurrentScene.SceneName, onComplete, recordHistory: false);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  查询
    // ═══════════════════════════════════════════════════════════════

    /// <summary>获取当前活跃场景名</summary>
    public string ActiveSceneName =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    /// <summary>获取已加载的叠加场景数量</summary>
    public int AdditiveSceneCount => _additiveScenes.Count;

    /// <summary>检查某个叠加场景是否已加载</summary>
    public bool IsAdditiveSceneLoaded(AssetReference sceneRef)
    {
        return sceneRef != null && _additiveScenes.ContainsKey(sceneRef.AssetGUID);
    }

    /// <summary>获取场景历史数量</summary>
    public int HistoryCount => _history.Count;
}

/// <summary>
/// 场景信息记录
/// </summary>
public class SceneInfo
{
    /// <summary>场景名称</summary>
    public string SceneName;

    /// <summary>Addressable 场景引用（可为 null，表示使用内置场景名加载的）</summary>
    public AssetReference SceneRef;

    /// <summary>加载 Handle（用于 Addressables 的场景卸载）</summary>
    public AsyncOperationHandle<SceneInstance> LoadHandle;

    /// <summary>关联的持久化数据</summary>
    public ScenePersistentData PersistentData;
}

/// <summary>
/// 场景过渡效果接口
/// 业务层实现此接口来提供自定义 Loading Screen / 过渡动画
/// 框架的 SceneManager 通过此接口驱动过渡，不依赖任何具体 UI 类
/// </summary>
/// <example>
/// // 示例：在业务层用 UILoadingPanel 实现
/// public class LoadingPanelTransition : ISceneTransition
/// {
///     public void BeginTransition(IEnumerator sceneLoadTask)
///     {
///         var panel = UIManager.Instance.PushUI&lt;UILoadingPanel&gt;();
///         panel.BeginTask(sceneLoadTask, () => UIManager.Instance.PopUI(panel));
///     }
///     
///     public void ReportProgress(float progress)
///     {
///         // 更新进度条
///     }
/// }
///
/// // 注册:
/// SceneManager.Instance.SetTransition(new LoadingPanelTransition());
/// </example>
public interface ISceneTransition
{
    /// <summary>
    /// 开始过渡效果并执行场景加载任务
    /// 实现者负责：显示 Loading UI → yield sceneLoadTask → 隐藏 Loading UI
    /// </summary>
    /// <param name="sceneLoadTask">场景加载协程，由 SceneManager 提供</param>
    void BeginTransition(IEnumerator sceneLoadTask);

    /// <summary>
    /// 进度更新回调（0~1），可用于更新进度条
    /// SceneManager 在加载过程中会主动调用此方法
    /// </summary>
    void ReportProgress(float progress);
}
