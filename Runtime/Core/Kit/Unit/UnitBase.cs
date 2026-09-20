using System;
using UnityEngine;

/// <summary>
/// 游戏单位生命周期契约：Spawn → Die → Delete。
/// </summary>
public interface IGameplayLifeCycle
{
    void Spawn();
    void Die();
    void Delete();
}

/// <summary>
/// 宿主（表现层）回调接口。
///
/// 纯 C# 内核 <see cref="UnitBase"/> 只认这个接口，不认识 MonoBehaviour / GameObject / Transform。
/// 内核负责状态流转、计时器与事件；一旦需要触碰引擎对象（改名 / SetActive / 延迟删除 /
/// 销毁或回池 / 挂点 VFX / 世界坐标），一律经本接口回调宿主。
/// </summary>
public interface IUnitHost
{
    /// <summary>宿主显示名 —— 供内核日志使用。</summary>
    string HostName { get; }

    /// <summary>宿主世界坐标（读走 transform.position，写回 transform.position）。</summary>
    Vector3 WorldPosition { get; set; }

    /// <summary>是否默认启用逻辑更新 —— 内核 OnSpawn 判定时读取。</summary>
    bool DefaultEnableLogic { get; }

    /// <summary>内核进入 Alive 后的表现钩子。</summary>
    void OnHostSpawned();

    /// <summary>内核进入 Dying 后的表现钩子：死亡表现（SetActive 切换 / 子 VFX 回收）+ 调度延迟删除。</summary>
    void OnHostDying();

    /// <summary>内核进入 Dead 后的表现钩子。</summary>
    void OnHostDied();

    /// <summary>内核进入 Deleted 后的表现钩子：调度 GameObject 销毁或回池。</summary>
    void OnHostDeleted();

    /// <summary>表现层每帧 tick（由内核 OnLogic 转发）。</summary>
    void OnHostLogic();

    /// <summary>内核生命周期状态已变化 —— 宿主据此同步改名等表现。</summary>
    void OnHostStateChanged(UnitLifecycleState state);
}

/// <summary>
/// Unit lifecycle states: None -> Spawning -> Alive -> Dying -> Dead -> Deleting -> Deleted
/// </summary>
public enum UnitLifecycleState
{
    None,       // Initial state, before Spawn is called
    Spawning,   // Spawn() called, waiting for next frame to execute OnSpawn()
    Alive,      // OnSpawn() executed, unit is active
    Dying,      // Die() called, waiting for next frame to execute OnDie()
    Dead,       // OnDie() executed, waiting for Delete()
    Deleting,   // Delete() called, waiting for next frame to execute OnDelete()
    Deleted     // OnDelete() executed, GameObject will be destroyed
}

/// <summary>
/// 单位逻辑内核 —— 纯 C# 对象（非 MonoBehaviour），可脱离场景构造。
///
/// 职责划分（表现/逻辑分离）：
///  · 本类承载：生命周期状态机、运行时计时器、逻辑开关、三个事件信号。
///  · 宿主 <see cref="IUnitHost"/> 承载：改名 / SetActive / Invoke 延迟删除 / Destroy 或回池 /
///    挂点 VFX / Gizmos / transform 读写。
///
/// 内核通过 <see cref="Host"/> 回调宿主，宿主通过持有本对象（组合）暴露转发门面，
/// 二者**不是继承关系**（C# 单继承位已被 MonoBehaviour 占用）。
/// </summary>
public class UnitBase : IGameplayLifeCycle
{
    #region Host

    /// <summary>表现层宿主。由宿主在创建内核后立即注入；无宿主时（未挂载）为 null。</summary>
    public IUnitHost Host { get; internal set; }

    /// <summary>宿主显示名（无宿主时回退到类型名）。</summary>
    public string Name => Host != null ? Host.HostName : GetType().Name;

    #endregion

    #region Events

    public KSignal onLogic = new();
    public KSignal<UnitBase> OnUnitDie = new();
    public KSignal OnUnitSpawn = new();

    #endregion

    #region Lifecycle State

    private UnitLifecycleState _lifecycleState = UnitLifecycleState.None;

    /// <summary>
    /// Current lifecycle state of this unit
    /// </summary>
    public UnitLifecycleState LifecycleState => _lifecycleState;

    /// <summary>
    /// Is the unit spawned and alive (can be damaged, can move, etc.)
    /// </summary>
    public bool IsAlive => _lifecycleState == UnitLifecycleState.Alive;

    /// <summary>
    /// Is the unit spawned (OnSpawn has been called)
    /// </summary>
    public bool IsSpawned => _lifecycleState >= UnitLifecycleState.Alive;

    /// <summary>
    /// Is the unit marked for deletion or already deleted
    /// </summary>
    public bool IsDeleted => _lifecycleState >= UnitLifecycleState.Deleted;

    /// <summary>
    /// 设置生命周期状态：改名等表现由宿主钩子承接。
    /// </summary>
    private void SetLifecycleState(UnitLifecycleState newState)
    {
        _lifecycleState = newState;
        Host?.OnHostStateChanged(newState);
    }

    #endregion

    #region World Position (forwarded to host)

    public Vector3 WorldPosition
    {
        get => Host != null ? Host.WorldPosition : Vector3.zero;
        set
        {
            if (Host != null) Host.WorldPosition = value;
        }
    }

    #endregion

    #region Logic Enable/Disable

    public bool EnableOnLogic { get; private set; } = false;

    /// <summary>
    /// Enable or disable logic updates for this unit
    /// </summary>
    public void SetLogicEnable(bool val)
    {
        if (_lifecycleState == UnitLifecycleState.Deleting || _lifecycleState == UnitLifecycleState.Deleted)
            return;

        if (EnableOnLogic == val)
            return;

        EnableOnLogic = val;

        if (UnitModule.Instance != null && !UnitModule.Instance.logicDirtyUnits.Contains(this))
        {
            UnitModule.Instance.logicDirtyUnits.Add(this);
        }
    }

    #endregion

    #region Cross-Scene Persistence

    /// <summary>
    /// 跨场景持久化标志。为 true 时宿主的销毁流程不会销毁 GameObject，且 UnitModule Shutdown 会跳过此 Unit。
    /// </summary>
    public bool PreventDestroy { get; set; }

    #endregion

    #region Lifecycle - Spawn

    /// <summary>
    /// Request to spawn this unit. Will be executed on next UnitModule tick.
    /// </summary>
    public virtual void Spawn()
    {
        if (_lifecycleState != UnitLifecycleState.None)
        {
            Debug.LogWarning($"[UnitBase] Cannot spawn unit '{Name}' - already in state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Spawning);
        UnitModule.Instance._toSpawnUnits.Add(this);
    }

    /// <summary>
    /// Called by UnitModule when spawning is processed. Override to add custom spawn logic.
    /// </summary>
    public virtual void OnSpawn()
    {
        if (_lifecycleState != UnitLifecycleState.Spawning)
        {
            Debug.LogError($"[UnitBase] OnSpawn called on unit '{Name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Alive);

        if (Host != null && Host.DefaultEnableLogic)
        {
            SetLogicEnable(true);
        }

        // 表现层钩子（子类覆写 UnityUnit.OnSpawn）
        Host?.OnHostSpawned();

        OnUnitSpawn?.Invoke();
    }

    #endregion

    #region Lifecycle - Die

    /// <summary>
    /// Request to kill this unit. Will be executed on next UnitModule tick.
    ///
    /// 内核只做状态流转与计时器清理；死亡表现（SetActive 切换 / 子 VFX 回收）与
    /// 延迟删除的调度交由宿主 OnHostDying 完成。
    /// </summary>
    public void Die()
    {
        if (_lifecycleState != UnitLifecycleState.Alive)
        {
            if (_lifecycleState != UnitLifecycleState.Spawning)
            {
                Debug.LogWarning($"[UnitBase] Cannot die unit '{Name}' - in state {_lifecycleState}");
            }
            return;
        }

        SetLifecycleState(UnitLifecycleState.Dying);

        // Stop all timers
        TimerManager.StopAllTimer();

        // 表现层钩子：死亡表现 + 调度销毁或回池
        Host?.OnHostDying();

        UnitModule.Instance._toDieUnits.Add(this);
    }

    /// <summary>
    /// Called by UnitModule when die is processed. Override to add custom die logic.
    /// </summary>
    public virtual void OnDie()
    {
        if (_lifecycleState != UnitLifecycleState.Dying)
        {
            Debug.LogError($"[UnitBase] OnDie called on unit '{Name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Dead);

        // 表现层钩子（子类覆写 UnityUnit.OnDie）
        Host?.OnHostDied();

        OnUnitDie?.Invoke(this);
    }

    #endregion

    #region Lifecycle - Delete

    /// <summary>
    /// Request to delete this unit. Will be executed on next UnitModule tick.
    /// </summary>
    public void Delete()
    {
        if (_lifecycleState == UnitLifecycleState.Deleting || _lifecycleState == UnitLifecycleState.Deleted)
        {
            return; // Already deleting or deleted
        }

        if (_lifecycleState == UnitLifecycleState.Alive)
        {
            Debug.LogWarning($"[UnitBase] Delete called on alive unit '{Name}' - calling Die first");
            Die();
            return;
        }

        SetLifecycleState(UnitLifecycleState.Deleting);
        UnitModule.Instance._toDeleteUnits.Add(this);
    }

    /// <summary>
    /// 场景切换 / 模块关闭时的静默清理。跳过 Die → OnDie 流程（不触发死亡信号、爆炸等游戏逻辑）。
    /// 直接进入 Dead → Deleting → OnDelete 清理链，仅做资源释放。
    /// </summary>
    public void ShutdownCleanup()
    {
        if (_lifecycleState >= UnitLifecycleState.Deleting)
            return;

        if (_lifecycleState == UnitLifecycleState.Alive)
            SetLifecycleState(UnitLifecycleState.Dead);

        if (_lifecycleState != UnitLifecycleState.Dead)
            return;

        SetLifecycleState(UnitLifecycleState.Deleting);
        UnitModule.Instance._toDeleteUnits.Add(this);
    }

    /// <summary>
    /// Called by UnitModule when deletion is processed.
    /// 内核只做状态与逻辑开关收口；GameObject 销毁 / 回池由宿主 OnHostDeleted 调度。
    /// </summary>
    public void OnDelete()
    {
        if (_lifecycleState != UnitLifecycleState.Deleting)
        {
            Debug.LogError($"[UnitBase] OnDelete called on unit '{Name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Deleted);

        // Disable logic if still enabled
        if (EnableOnLogic)
        {
            SetLogicEnable(false);
        }

        // 表现层钩子：调度 GameObject 销毁或回池
        Host?.OnHostDeleted();
    }

    #endregion

    #region Pool

    /// <summary>
    /// 池复用的状态复位（由宿主 IPoolable.OnGetFromPool 调用）。
    /// 只复位内核态；SetActive / 子 VFX / 组件容器等引擎操作由宿主自理。
    /// </summary>
    public virtual void ResetForPool()
    {
        _lifecycleState = UnitLifecycleState.None;
        EnableOnLogic = false;
        SelfTimeScale = 1.0f;

        // Reset timers
        TimerManager = new KTimerManager();
    }

    /// <summary>
    /// 重置生命周期状态到 None，用于跨场景保留后重新注册到新 UnitModule。
    /// </summary>
    public void ResetLifecycleState()
    {
        _lifecycleState = UnitLifecycleState.None;
        EnableOnLogic = false;
        Host?.OnHostStateChanged(_lifecycleState);
    }

    /// <summary>
    /// 回池前的内核清理（由宿主 IPoolable.OnReturnToPool 调用）。
    /// </summary>
    public virtual void CleanupForPool()
    {
        // Disable logic
        EnableOnLogic = false;

        // Stop all timers
        TimerManager.StopAllTimer();

        // Clear events (avoid leaking references across pool cycles)
        onLogic = new KSignal();
        OnUnitDie = new KSignal<UnitBase>();
        OnUnitSpawn = new KSignal();
    }

    #endregion

    #region Timer System

    public KTimerManager TimerManager { get; private set; } = new();

    public float SelfTimeScale = 1.0f;

    public float ScaledDeltaTime
    {
        get
        {
            // 使用 UnitModule 的全局时间缩放管理
            if (UnitModule.Instance != null)
            {
                return UnitModule.Instance.GetFinalScaledDeltaTime(this);
            }

            // 回退到原来的逻辑（如果 UnitModule 不可用）
            if (SelfTimeScale == 0)
            {
                return 0;
            }
            return KTime.scaleDeltaTime * SelfTimeScale;
        }
    }

    public KTimer AddTimer(float duration, Action onTimerComplete = null, int loops = 1)
    {
        return TimerManager.AddTimer(duration, onTimerComplete, loops);
    }

    #endregion

    #region Logic Update

    /// <summary>
    /// Called by UnitModule every fixed tick if EnableOnLogic is true.
    /// 内核推进计时器并广播事件，随后转发给宿主做表现 tick。
    /// </summary>
    public virtual void OnLogic()
    {
        TimerManager.OnLogic(ScaledDeltaTime);

        onLogic?.Invoke();

        // 表现层 tick（子类覆写 UnityUnit.OnLogic）
        Host?.OnHostLogic();
    }

    #endregion
}
