using System;
using System.Collections;
using UnityEngine.AddressableAssets;

/// <summary>
/// 场景管理服务接口
/// 提供场景加载、卸载、切换、叠加、历史导航等能力
/// </summary>
public interface ISceneService
{
    // ─── 状态查询 ───

    bool IsLoading { get; }
    float Progress { get; }
    SceneInfo CurrentScene { get; }
    bool CanGoBack { get; }
    string ActiveSceneName { get; }
    int AdditiveSceneCount { get; }
    int HistoryCount { get; }

    // ─── 信号 ───

    KSignal<string> OnSceneLoadBegin { get; }
    KSignal<float> OnSceneLoadProgress { get; }
    KSignal<string> OnSceneLoadComplete { get; }
    KSignal<string> OnSceneUnloaded { get; }
    KSignal<string> OnSceneLoadError { get; }

    // ─── 主场景切换 ───

    void LoadScene(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null, bool recordHistory = true);
    void LoadSceneByName(string sceneName, Action onComplete = null, bool recordHistory = true);
    void LoadSceneByIndex(int buildIndex, Action onComplete = null, bool recordHistory = true);
    IEnumerator LoadSceneCoroutine(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null, bool recordHistory = true);
    IEnumerator LoadSceneByNameCoroutine(string sceneName, Action onComplete = null, bool recordHistory = true);

    // ─── 场景历史 ───

    void GoBack(Action onComplete = null);
    void ClearHistory();

    // ─── 叠加场景 ───

    void LoadAdditiveScene(AssetReference sceneRef, Action<string> onComplete = null);
    IEnumerator LoadAdditiveSceneCoroutine(AssetReference sceneRef, Action<string> onComplete = null);
    IEnumerator LoadAdditiveSceneByNameCoroutine(string sceneName, Action onComplete = null);
    IEnumerator UnloadAdditiveSceneCoroutine(AssetReference sceneRef, Action onComplete = null);
    IEnumerator UnloadAdditiveSceneByNameCoroutine(string sceneName, Action onComplete = null);
    IEnumerator UnloadAllAdditiveScenes();
    bool IsAdditiveSceneLoaded(AssetReference sceneRef);

    // ─── 过渡效果 ───

    ISceneTransition Transition { get; }
    void SetTransition(ISceneTransition transition);
    void LoadSceneWithTransition(AssetReference sceneRef, ScenePersistentData persistentData = null,
        Action onComplete = null);
    void LoadSceneWithTransition(string sceneName, Action onComplete = null);

    // ─── 重载 ───

    void ReloadCurrentScene(Action onComplete = null);
}
