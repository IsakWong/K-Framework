// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2025/10/17

using System;
using UnityEngine;

/// <summary>
/// GameMode组件基类,由GameMode管理的游戏状态组件
/// 提供玩家死亡、存档读档等生命周期回调
/// </summary>
public abstract class IGameModeEventListener : MonoBehaviour
{
    [Header("Component Settings")] [Tooltip("是否自动初始化")]
    public bool autoInitialize = true;

    protected GameMode gameMode;
    protected Subscriber subscriber = new();

    protected bool isInitialized = false;
    protected bool isStarted = false;

    #region Unity Lifecycle
        
    protected virtual void OnDestroy()
    {
        OnComponentDestroy();
        subscriber.DisconnectAll();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 初始化组件（只会调用一次）
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning($"[GameModeComponent] {GetType().Name} already initialized!");
            return;
        }

        OnInitialize();
        isInitialized = true;
        Debug.Log($"[GameModeComponent] {GetType().Name} initialized");
    }

    public void Initialized()
    {
        OnInitialized();
    }
    
    /// <summary>
    /// 组件初始化时调用（子类重写）
    /// </summary>
    protected virtual void OnInitialize()
    {
    }
    protected virtual void OnInitialized()
    {
    }

    #endregion

    #region Lifecycle Callbacks

    /// <summary>
    /// 组件启动时调用
    /// </summary>
    public virtual void OnModeStart()
    {
        if (isStarted)
        {
            Debug.LogWarning($"[GameModeComponent] {GetType().Name} already started!");
            return;
        }

        isStarted = true;
        Debug.Log($"[GameModeComponent] {GetType().Name} started");
    }

    /// <summary>
    /// 组件销毁时调用
    /// </summary>
    public virtual void OnComponentDestroy()
    {
    }

    #endregion

    #region Game State Callbacks

    /// <summary>
    /// 当玩家死亡时调用
    /// </summary>
    /// <param name="player">死亡的玩家角色</param>
    /// <param name="killer">造成死亡的单位（可能为null）</param>
    public virtual void OnPlayerDeath(UnitBase player, UnitBase killer = null)
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Player died - {player.name}");
    }

    /// <summary>
    /// 当玩家重生时调用
    /// </summary>
    /// <param name="player">重生的玩家角色</param>
    public virtual void OnPlayerRespawn(UnitBase player)
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Player respawned - {player.name}");
    }

    /// <summary>
    /// 当关卡重新开始时调用
    /// </summary>
    public virtual void OnLevelRestart()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Level restarted");
    }

    /// <summary>
    /// 当关卡完成时调用
    /// </summary>
    public virtual void OnLevelComplete()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Level completed");
    }

    /// <summary>
    /// 当关卡失败时调用
    /// </summary>
    public virtual void OnLevelFailed()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Level failed");
    }

    #endregion

    #region Save/Load Callbacks

    /// <summary>
    /// 保存游戏数据时调用
    /// </summary>
    /// <returns>返回要保存的数据（JSON格式）</returns>
    public virtual string OnSaveGameData()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Saving game data");
        return "{}";
    }

    /// <summary>
    /// 加载游戏数据时调用
    /// </summary>
    /// <param name="data">加载的数据（JSON格式）</param>
    public virtual void OnLoadGameData(string data)
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Loading game data");
    }

    /// <summary>
    /// 当创建新存档时调用
    /// </summary>
    public virtual void OnNewGame()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: New game started");
    }

    /// <summary>
    /// 当读档时调用
    /// </summary>
    public virtual void OnLoadGame()
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Game loaded");
    }

    #endregion

    #region Checkpoint Callbacks

    /// <summary>
    /// 当到达检查点时调用
    /// </summary>
    /// <param name="checkpointId">检查点ID</param>
    public virtual void OnCheckpointReached(string checkpointId)
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Checkpoint reached - {checkpointId}");
    }

    /// <summary>
    /// 当从检查点重生时调用
    /// </summary>
    /// <param name="checkpointId">检查点ID</param>
    public virtual void OnCheckpointRespawn(string checkpointId)
    {
        Debug.Log($"[GameModeComponent] {GetType().Name}: Respawning from checkpoint - {checkpointId}");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 检查组件是否已初始化
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 检查组件是否已启动
    /// </summary>
    public bool IsStarted()
    {
        return isStarted;
    }

    /// <summary>
    /// 获取关联的GameMode
    /// </summary>
    public GameMode GetGameMode()
    {
        return gameMode;
    }

    #endregion
}