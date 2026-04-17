using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏模式基类
/// 每个场景应该有且仅有一个 GameMode
/// 负责场景的初始化、持久化数据管理和生命周期控制
/// </summary>
[DefaultExecutionOrder(GameCoreProxy.GameModeOrder)]
public class GameMode : MonoBehaviour
{
    #region State Flags
    
    private bool isBegan = false;
    private bool isSaving = false;
    private bool isInitialized = false;
    
    #endregion

    #region Settings
    
    [Header("Mode Settings")]
    [Tooltip("是否启用此 GameMode")]
    public bool Enable = true;
    
    [Tooltip("是否自动启动场景（如果为 false，需要手动调用 StartMode）")]
    public bool AutoStart = true;
    
    [Tooltip("场景背景音乐")]
    public AudioClip SceneMusic;
    
    [Tooltip("是否在启动时加载持久化数据")]
    public bool LoadPersistentDataOnStart = true;
    
    #endregion

    #region Persistent Data
    
    [NonSerialized]
    public ScenePersistentData PersistentData;
    
    /// <summary>
    /// 是否正在保存数据
    /// </summary>
    public bool IsSaving => isSaving;
    
    /// <summary>
    /// 场景是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
    
    #endregion

    #region Timers and Signals
    
    [NonSerialized]
    public KTimerManager Timers = new(); 

    protected Subscriber subscriber = new();
    
    #endregion

    #region Unity Lifecycle

    private bool _awaken = false;
    private void Awake()
    {
        _awaken = true;
        EnhancedLog.Info("GameMode", $"{gameObject.name} Awake - Switching to this GameMode");
        KGameCore.Instance.SwitchGameMode(this);
    }

    private void Start()
    {
        Debug.Assert(_awaken);
        if (AutoStart)
        {
            StartCoroutine(StartMode());
        }
    }

    private void FixedUpdate()
    {
        if (!Enable) return;
        
        Timers.OnLogic(KTime.scaleDeltaTime);
    }

    private void OnDestroy()
    {
        OnModeEnd();
    }
    
    #endregion

    #region Mode Lifecycle

    /// <summary>
    /// 开始游戏模式
    /// 这是场景的主要入口点
    /// </summary>
    public IEnumerator StartMode()
    {
        yield return InitializeMode();
        yield return OnModeStart();
    }

    /// <summary>
    /// 初始化游戏模式
    /// 包括加载持久化数据、初始化场景等
    /// </summary>
    protected virtual IEnumerator InitializeMode()
    {
        Debug.Assert(_awaken);
        if (isInitialized)
        {
            Debug.LogWarning($"[GameMode] {gameObject.name} already initialized, skipping");
            yield return null;
        }
        EnhancedLog.Info("GameMode", $"{gameObject.name} Initializing...");
        
        // 加载持久化数据
        if (LoadPersistentDataOnStart)
        {
            LoadScenePersistentData();
        }
        
        foreach (var comp in modeComponents)
        {
            comp.Initialize();
        }
        // 调用虚方法，允许子类扩展初始化逻辑
        yield return OnModeInitialize();
        
        foreach (var comp in modeComponents)
        {
            comp.Initialized();
        }

        isInitialized = true;
        EnhancedLog.Info("GameMode", $"{gameObject.name} Initialized");
        yield return null;
    }

    /// <summary>
    /// 场景初始化时调用（在 OnModeStart 之前）
    /// 子类可以重写此方法来执行自定义初始化逻辑
    /// </summary>
    protected virtual IEnumerator OnModeInitialize()
    {
        
        // 子类实现
        yield return null;
    }
    List<IGameModeEventListener> modeComponents = new List<IGameModeEventListener>();
    /// <summary>
    /// 场景开始时调用
    /// </summary>
    public virtual void OnModeAwake()
    {
        if (isBegan)
            return;
        modeComponents = new List<IGameModeEventListener>(FindObjectsByType<IGameModeEventListener>(FindObjectsSortMode.None));
        isBegan = true;
        EnhancedLog.Info("GameMode", $"{gameObject.name} {GetType().Name} OnModeBegin");
    }

    /// <summary>
    /// 场景启动时调用
    /// </summary>
    public virtual IEnumerator OnModeStart()
    {
        if (SceneMusic != null)
        {
            SoundManager.Instance.PlayMusic(SceneMusic);
        }
        
        EnhancedLog.Info("GameMode", $"{gameObject.name} {GetType().Name} OnModeStart");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnModeStart();
            }
        }
        
        yield return null;
    }

    /// <summary>
    /// 重启场景
    /// </summary>
    public virtual void OnModeRestart()
    {
        EnhancedLog.Info("GameMode", $"{gameObject.name} {GetType().Name} OnModeRestart");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnLevelRestart();
            }
        }
    }

    /// <summary>
    /// 场景结束时调用
    /// </summary>
    public virtual void OnModeEnd()
    {
        subscriber.DisconnectAll();
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnComponentDestroy();
            }
        }
        
        EnhancedLog.Info("GameMode", $"{GetType().Name} End");
    }

    /// <summary>
    /// 切换到新场景时调用
    /// </summary>
    public virtual void OnSwitchGameMode(GameMode newMode)
    {
        Enable = false;
        EnhancedLog.Info("GameMode", $"{gameObject.name} switching to {newMode?.gameObject.name ?? "null"}");
        
        // 可以在这里执行场景切换前的清理工作
        OnBeforeSceneUnload();
    }

    /// <summary>
    /// 场景卸载前调用
    /// 子类可以重写此方法来执行自定义清理逻辑
    /// </summary>
    protected virtual void OnBeforeSceneUnload()
    {
        // 子类实现
    }

    /// <summary>
    /// 重启场景
    /// </summary>
    public void RestartMode()
    {
        OnModeRestart();
    }

    #endregion

    #region Persistent Data Management

    /// <summary>
    /// 加载场景持久化数据
    /// </summary>
    public virtual void LoadScenePersistentData()
    {
        string sceneName = gameObject.scene.name;
        EnhancedLog.Debug("GameMode", $"Loading persistent data for scene: {sceneName}");
        
        // 尝试从 PersistentDataManager 加载场景数据
        // 注意: 你需要确保 PersistentDataManager 有相应的加载方法
        // 这里暂时注释掉,子类可以重写这个方法
        
        // 通知新游戏
        OnNewGame();
    }

    private ScenePersistentData persistentData;
    /// <summary>
    /// 应用持久化数据到场景
    /// 子类可以重写此方法来自定义数据应用逻辑
    /// </summary>
    public virtual IEnumerator ApplyPersistentData(ScenePersistentData data)
    {
        persistentData = data;
        yield return null;
    }

    /// <summary>
    /// 保存场景持久化数据（协程）
    /// </summary>
    public virtual IEnumerable SavePersistentData()
    {
        if (isSaving)
        {
            Debug.LogWarning($"[GameMode] Already saving, skipping duplicate save request");
            yield return null;
        }
        
        isSaving = true;
        EnhancedLog.Info("GameMode", $"Saving persistent data for scene: {gameObject.scene.name}");

        var handle = PersistentDataManager.Instance.SaveScene();
        yield return handle;
        
        if (handle.Current is ScenePersistentData savedData)
        {
            PersistentData = savedData;
            OnSavePersistentData(savedData);
            EnhancedLog.Debug("GameMode", $"Saved {savedData.ObjectDatas.Count} persistent objects");
        }
        
        isSaving = false;
        yield return null;
    }

    /// <summary>
    /// 保存持久化数据完成时调用
    /// 子类可以重写此方法来执行额外的保存逻辑
    /// </summary>
    protected virtual void OnSavePersistentData(ScenePersistentData persistentData)
    {
        // 子类实现
    }

    #endregion

    #region Game Events (Override in subclass)

    /// <summary>
    /// 新游戏开始时调用
    /// </summary>
    protected virtual void OnNewGame()
    {
        EnhancedLog.Info("GameMode", "New game started");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnNewGame();
            }
        }
    }

    /// <summary>
    /// 加载游戏时调用
    /// </summary>
    protected virtual void OnLoadGame()
    {
        EnhancedLog.Info("GameMode", "Game loaded");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnLoadGame();
            }
        }
    }

    /// <summary>
    /// 玩家死亡时调用
    /// </summary>
    protected virtual void OnPlayerDeath(UnitBase player, UnitBase killer = null)
    {
        EnhancedLog.Info("GameMode", $"Player died: {player?.name ?? "null"}");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnPlayerDeath(player, killer);
            }
        }
    }

    /// <summary>
    /// 玩家重生时调用
    /// </summary>
    protected virtual void OnPlayerRespawn(UnitBase player)
    {
        EnhancedLog.Info("GameMode", $"Player respawned: {player?.name ?? "null"}");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnPlayerRespawn(player);
            }
        }
    }

    /// <summary>
    /// 关卡完成时调用
    /// </summary>
    protected virtual void OnLevelComplete()
    {
        EnhancedLog.Info("GameMode", "Level completed");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnLevelComplete();
            }
        }
    }

    /// <summary>
    /// 关卡失败时调用
    /// </summary>
    protected virtual void OnLevelFailed()
    {
        EnhancedLog.Info("GameMode", "Level failed");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnLevelFailed();
            }
        }
    }

    /// <summary>
    /// 到达检查点时调用
    /// </summary>
    protected virtual void OnCheckpointReached(string checkpointId)
    {
        EnhancedLog.Debug("GameMode", $"Checkpoint reached: {checkpointId}");
        
        // 通知所有组件
        foreach (var component in modeComponents)
        {
            if (component != null)
            {
                component.OnCheckpointReached(checkpointId);
            }
        }
    }

    #endregion

    #region Debug GUI

    private void OnGUI()
    {
        if (!Enable) return;
        OnDebugGUI();
    }

    /// <summary>
    /// 子类重写此方法来绘制调试 GUI
    /// 仅在 Enable 为 true 时调用
    /// </summary>
    protected virtual void OnDebugGUI()
    {
    }

    #endregion

    private void OnValidate()
    {
        gameObject.name = GetType().Name;
    }
}