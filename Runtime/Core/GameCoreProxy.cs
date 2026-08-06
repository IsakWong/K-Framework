using System;
using System.Collections;
using System.Collections.Generic;
using KFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
///
/// 
/// </summary>
[DefaultExecutionOrder(GameCoreProxyOrder)]
public class GameCoreProxy : MonoBehaviour
{
    public const int GameCoreProxyOrder = -100;
    public const int ModuleOrder = -80;
    public const int GameModeOrder = -50;

    private void Awake()
    {
        var instace = KGameCore.Instance;
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
    }


    private void FixedUpdate()
    {
        var instace = KGameCore.Instance;
        if (instace.proxy != this)
        {
            return;
        }

        KGameCore.Instance.OnLogic();
    }



    private bool shutdown = false;

    private Action cleanupDone;

    public IEnumerator ShutdownModules()
    {
        EnhancedLog.Log("Shutting down non-persistent modules...");
        if (shutdown)
        {
            yield return null;
        }

        shutdown = true;
        Queue<IModule> modules = new();
        KGameCore.Instance.GetAllModules(modules);

        while (modules.Count > 0)
        {
            var first = modules.Peek();
            // 持久化模块跨场景存活，不随场景切换销毁
            if (first.Persistent)
            {
                modules.Dequeue();
                continue;
            }

            if (first.RequestShutdown())
            {
                first.Dispose();
                KGameCore.Instance.RemoveModule(first);
                modules.Dequeue();
            }

            yield return new WaitForFixedUpdate();
        }

        yield return null;
    }

    [NonSerialized] public bool LoadingScene = false;

    private AssetReference nextLevel;
    private Action<bool> finishCallback;

    [NonSerialized] public AsyncOperationHandle<SceneInstance> LoadSceneHandle = new();

    [NonSerialized] public AsyncOperation ActivateSceneHandle;
    
    private Scene OldScene;

    private ScenePersistentData _scenePersistentData;

    /// <summary>
    /// 加载新场景的协程接口
    /// </summary>
    [Obsolete("Use SceneManager.Instance.LoadSceneCoroutine() instead")]
    public IEnumerator LoadNextSceneCoroutine(AssetReference NextLevel, ScenePersistentData persistentData = null)
    {
        yield return SceneManager.Instance.LoadSceneCoroutine(NextLevel, persistentData);
    }

}