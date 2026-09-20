using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 单位表现宿主 —— MonoBehaviour，承接 <see cref="UnitBase"/> 内核剥离出来的全部引擎职责：
/// 改名 / SetActive 切换 / Invoke 延迟删除 / Destroy 或回池 / 跨场景持久化标记 /
/// 挂点 VFX / Gizmos / transform 读写。
///
/// 与内核是**组合**关系（非继承，C# 单继承位已被 MonoBehaviour 占用）：
///  · <see cref="Core"/> 取内核；<c>Core.Host</c> 取回宿主。
///  · 子类经覆写 <see cref="CreateCore"/> 提供自己的内核类型。
///
/// 对外调用面（IsAlive / Spawn / Die / Delete / TimerManager / 事件 …）由转发门面代理到内核，
/// 因此原有调用点无需改动。
/// </summary>
[DisallowMultipleComponent]
public class UnityUnit : MonoBehaviour, IUnitHost, IPoolable
{
    #region Inspector Fields

    [FormerlySerializedAs("EnableLogic")]
    [LabelText("默认启用逻辑更新")]
    public bool DefaultEnableLogic = false;

    [HideInInspector]
    public bool SpawnOnStart = true;

    [LabelText("死亡后单位删除延迟")]
    public float DeleteDelay = 3.0f;

    [LabelText("删除后Destroy延迟")]
    public float UnityDestroyDelay = 1.0f;

    [LabelText("死亡禁用物体")]
    public List<GameObject> DieDisableGameObjects = new();

    [LabelText("死亡启用物体")]
    public List<GameObject> DieEnableGameObjects = new();

    [LabelText("可选中")]
    public bool Selectable = true;

    /// <summary>
    /// Whether this unit supports object pool recycling.
    /// Default is false — subclasses must override to opt-in.
    /// When true, _Destroy returns the unit to the pool instead of destroying it.
    /// </summary>
    public virtual bool Recyclable => false;

    #endregion

    #region Core

    /// <summary>逻辑内核。与宿主一同创建、一同回收（非序列化字段，不参与 Instantiate 复制）。</summary>
    public UnitBase Core { get; private set; }

    /// <summary>
    /// 提供本宿主配对的逻辑内核类型。子类覆写以返回自己的 <see cref="UnitBase"/> 派生内核。
    /// </summary>
    protected virtual UnitBase CreateCore() => new UnitBase();

    #endregion

    #region Forwarding Facade (to Core)

    public string Name => Core != null ? Core.Name : gameObject.name;

    public UnitLifecycleState LifecycleState => Core != null ? Core.LifecycleState : UnitLifecycleState.None;

    public bool IsAlive => Core != null && Core.IsAlive;

    public bool IsSpawned => Core != null && Core.IsSpawned;

    public bool IsDeleted => Core != null && Core.IsDeleted;

    public bool EnableOnLogic => Core != null && Core.EnableOnLogic;

    public bool PreventDestroy
    {
        get => Core != null && Core.PreventDestroy;
        set
        {
            if (Core != null) Core.PreventDestroy = value;
        }
    }

    public float SelfTimeScale
    {
        get => Core != null ? Core.SelfTimeScale : 1.0f;
        set
        {
            if (Core != null) Core.SelfTimeScale = value;
        }
    }

    public float ScaledDeltaTime => Core != null ? Core.ScaledDeltaTime : 0f;

    public KTimerManager TimerManager => Core != null ? Core.TimerManager : null;

    public KSignal onLogic => Core != null ? Core.onLogic : null;

    public KSignal<UnitBase> OnUnitDie => Core != null ? Core.OnUnitDie : null;

    public KSignal OnUnitSpawn => Core != null ? Core.OnUnitSpawn : null;

    public Vector3 WorldPosition
    {
        get => transform.position;
        set => transform.position = value;
    }

    public void Spawn() => Core?.Spawn();

    public void Die() => Core?.Die();

    public void Delete() => Core?.Delete();

    public void SetLogicEnable(bool val) => Core?.SetLogicEnable(val);

    public void ResetLifecycleState() => Core?.ResetLifecycleState();

    public KTimer AddTimer(float duration, Action onTimerComplete = null, int loops = 1)
        => Core?.AddTimer(duration, onTimerComplete, loops);

    #endregion

    #region Presentation Hooks (subclass overrides)

    /// <summary>内核进入 Alive 时的表现钩子。</summary>
    public virtual void OnSpawn()
    {
    }

    /// <summary>内核进入 Dead 时的表现钩子。</summary>
    public virtual void OnDie()
    {
    }

    /// <summary>内核每逻辑帧 tick 的表现钩子（由 <see cref="UnitBase.OnLogic"/> 转发）。</summary>
    public virtual void OnLogic()
    {
    }

    #endregion

    #region IUnitHost

    string IUnitHost.HostName => gameObject.name;

    Vector3 IUnitHost.WorldPosition
    {
        get => transform.position;
        set => transform.position = value;
    }

    bool IUnitHost.DefaultEnableLogic => DefaultEnableLogic;

    void IUnitHost.OnHostSpawned() => OnSpawn();

    void IUnitHost.OnHostDied() => OnDie();

    void IUnitHost.OnHostLogic() => OnLogic();

    /// <summary>
    /// 死亡表现 + 调度延迟删除。内核已停表并置 Dying，此处只做引擎侧副作用。
    /// </summary>
    void IUnitHost.OnHostDying()
    {
        // 死亡瞬间的显隐切换
        foreach (var go in DieEnableGameObjects)
        {
            if (go != null) go.SetActive(true);
        }
        foreach (var go in DieDisableGameObjects)
        {
            if (go != null) go.SetActive(false);
        }

        // 回收挂点 VFX
        var temp = subVFX.ToArray();
        subVFX.Clear();
        foreach (var vfx in temp)
        {
            if (vfx != null) vfx.Die();
        }

        // Schedule deletion
        if (DeleteDelay > 0)
        {
            Invoke(nameof(Delete), DeleteDelay);
        }
        else
        {
            Delete();
        }
    }

    /// <summary>内核进入 Deleted 后，调度 GameObject 销毁或回池。</summary>
    void IUnitHost.OnHostDeleted()
    {
        // Schedule Unity GameObject destruction
        if (UnityDestroyDelay > 0)
        {
            Invoke(nameof(_Destroy), UnityDestroyDelay);
        }
        else
        {
            _Destroy();
        }
    }

    /// <summary>内核状态变更后同步 GameObject 名字（追加状态后缀）。</summary>
    void IUnitHost.OnHostStateChanged(UnitLifecycleState state) => UpdateGameObjectName();

    #endregion

    #region Name

    /// <summary>
    /// 更新GameObject名字，添加状态后缀
    /// </summary>
    private void UpdateGameObjectName()
    {
        if (gameObject == null || Core == null) return;

        // 移除旧的状态后缀（如果有）
        string currentName = gameObject.name;
        int bracketIndex = currentName.LastIndexOf('[');
        if (bracketIndex > 0)
        {
            currentName = currentName.Substring(0, bracketIndex).TrimEnd();
        }

        // 添加新的状态后缀
        gameObject.name = $"{currentName} [{Core.LifecycleState}]";
    }

    #endregion

    #region Destroy / Pool

    public void _Destroy()
    {
        if (Core != null && Core.PreventDestroy) return;

        // Only recycle if the subclass opts in AND the instance came from a pool
        if (Recyclable && PoolManager.Instance != null && PoolManager.Instance.IsPooled(gameObject))
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by the pool system when this unit is taken from the pool.
    /// Resets core state, restores visual state, then auto-spawns if configured.
    /// </summary>
    public virtual void OnGetFromPool()
    {
        // 内核状态复位
        Core?.ResetForPool();

        // Reset visual state
        foreach (var go in DieDisableGameObjects)
            if (go != null) go.SetActive(true);
        foreach (var go in DieEnableGameObjects)
            if (go != null) go.SetActive(false);

        // Clear sub VFX
        subVFX.Clear();

        // Auto-spawn if configured
        if (SpawnOnStart)
        {
            Spawn();
        }
    }

    /// <summary>
    /// Called by the pool system when this unit is returned to the pool.
    /// </summary>
    public virtual void OnReturnToPool()
    {
        // 内核清理（逻辑开关 / 计时器 / 事件）
        Core?.CleanupForPool();

        // Clear sub VFX
        subVFX.Clear();
    }

    #endregion

    #region Socket System

    protected List<Vfx> subVFX = new();

    public virtual Vector3 GetSocketWorldPosition(string name)
    {
        return WorldPosition;
    }

    public virtual Transform GetSocketTransform(string name)
    {
        return transform;
    }

    public virtual Vfx CreateSocketVisual(GameObject visualPrefab, string socket = "", float lifeTime = -1f)
    {
        var socketTransform = GetSocketTransform(socket);

        // Use VfxManager (pool-backed) if available, fallback to raw Instantiate
        Vfx visual;
        if (VfxManager.Instance != null)
        {
            visual = VfxManager.Instance.Get(visualPrefab, socketTransform.position, socketTransform.rotation, socketTransform);
        }
        else
        {
            var result = Instantiate(visualPrefab, socketTransform.position, socketTransform.rotation);
            visual = result.GetComponent<Vfx>();
            if (visual == null)
                visual = result.AddComponent<Vfx>();
            result.transform.SetParent(socketTransform, true);
        }

        if (lifeTime != -1f)
        {
            visual.mLifeTime = lifeTime;
        }

        visual.EventDestroy += () => { subVFX.Remove(visual); };
        subVFX.Add(visual);
        return visual;
    }

    public virtual void RemoveSocketVisual(Vfx visual)
    {
        if (visual != null) visual.Die();
    }

    #endregion

    #region Unity Lifecycle

    private Transform componentsTransform;

    protected virtual void Awake()
    {
        if (gameObject.name.Contains("(Clone)"))
            gameObject.name = gameObject.name.Replace("(Clone)", "");

        // __Components__ 容器（历史结构约定，保留）
        componentsTransform = transform.Find("__Components__");
        if (componentsTransform == null)
        {
            componentsTransform = new GameObject("__Components__").transform;
            componentsTransform.SetParent(transform, false);
        }

        // 创建并接管内核
        Core = CreateCore();
        if (Core != null)
        {
            Core.Host = this;
        }

        if (SpawnOnStart)
        {
            Spawn();
        }
    }

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    /// <summary>
    /// Enable/disable gizmo visualization in editor
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Core == null) return;

        // Draw state indicator sphere
        Color stateColor = GetGizmoColor(Core.LifecycleState);
        Gizmos.color = stateColor;

        Vector3 pos = transform.position;
        Gizmos.DrawWireSphere(pos, 0.3f);

        // Draw logic enabled indicator
        if (Application.isPlaying && Core.EnableOnLogic)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pos + Vector3.up * 0.5f, Vector3.one * 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Core == null) return;

        // Draw more detailed info when selected
        Color stateColor = GetGizmoColor(Core.LifecycleState);
        Gizmos.color = stateColor;

        Vector3 pos = transform.position;

        // Draw larger sphere
        Gizmos.DrawWireSphere(pos, 0.5f);

        // Draw state text
        UnityEditor.Handles.BeginGUI();
        Vector3 screenPos = UnityEditor.HandleUtility.WorldToGUIPoint(pos + Vector3.up);
        GUI.color = stateColor;
        GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 40, 100, 20),
            Core.LifecycleState.ToString(),
            new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        GUI.color = Color.white;
        UnityEditor.Handles.EndGUI();
    }

    private Color GetGizmoColor(UnitLifecycleState state)
    {
        switch (state)
        {
            case UnitLifecycleState.None: return Color.gray;
            case UnitLifecycleState.Spawning: return Color.yellow;
            case UnitLifecycleState.Alive: return Color.green;
            case UnitLifecycleState.Dying: return new Color(1f, 0.5f, 0f); // Orange
            case UnitLifecycleState.Dead: return Color.red;
            case UnitLifecycleState.Deleting: return new Color(0.5f, 0f, 0f); // Dark red
            case UnitLifecycleState.Deleted: return Color.black;
            default: return Color.white;
        }
    }
#endif

    #endregion
}
