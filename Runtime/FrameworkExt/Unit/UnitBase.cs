using DG.Tweening;
using MoreMountains.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public interface IGameplayLifeCycle
{
    void Spawn();
    void Die();
    void Delete();
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

[DisallowMultipleComponent]
public class UnitBase : MonoBehaviour, IGameplayLifeCycle, IPoolable
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
    public List<GameObject> DieDisableGameObjects=new();
    
    [LabelText("死亡启用物体")]
    public List<GameObject> DieEnableGameObjects=new();

    [LabelText("可选中")]
    public bool Selectable = true;
    
    /// <summary>
    /// Whether this unit supports object pool recycling.
    /// Default is false — subclasses must override to opt-in.
    /// When true, _Destroy returns the unit to the pool instead of destroying it.
    /// </summary>
    public virtual bool Recyclable => false;
    
    #endregion

    #region Events

    [NonSerialized] public KSignal onLogic = new();
    [NonSerialized] public KSignal<UnitBase> OnUnitDie = new();
    [NonSerialized] public KSignal OnUnitSpawn = new();

    #endregion

    #region Lifecycle State

    private UnitLifecycleState _lifecycleState = UnitLifecycleState.None;
    private string _baseGameObjectName; // 保存原始名字（不带状态后缀）
    
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
    /// 设置生命周期状态并更新GameObject名字
    /// </summary>
    private void SetLifecycleState(UnitLifecycleState newState)
    {
        _lifecycleState = newState;
        UpdateGameObjectName();
    }

    /// <summary>
    /// 更新GameObject名字，添加状态后缀
    /// </summary>
    private void UpdateGameObjectName()
    {
        if (gameObject == null) return;
        
        // 移除旧的状态后缀（如果有）
        string currentName = gameObject.name;
        int bracketIndex = currentName.LastIndexOf('[');
        if (bracketIndex > 0)
        {
            currentName = currentName.Substring(0, bracketIndex).TrimEnd();
        }
        
        // 添加新的状态后缀
        gameObject.name = $"{currentName} [{_lifecycleState}]";
    }

    #endregion

    #region Transform Properties

    public Vector3 WorldForward => transform.forward;

    public Vector3 WorldPosition
    {
        get => transform.position;
        set => transform.position = value;
    }

    #endregion

    #region Components

    [HideInInspector]
    [NonSerialized]
    public List<UnitComponent> UnitComponents = new();

    public T AddUnitComponent<T>() where T : UnitComponent
    {
        var compGO = new GameObject(typeof(T).Name);
        var t = compGO.AddComponent<T>();
        InitUnitComponent(t);
        return t;
    }

    public T RequireUnitComponent<T>() where T : UnitComponent
    {
        var existing = GetUnitComponent<T>();
        if (existing != null)
            return existing;
        return AddUnitComponent<T>();
    }

    public T GetUnitComponent<T>() where T : UnitComponent
    {
        return UnitComponents.OfType<T>().FirstOrDefault();
    }

    protected UnitComponent InitUnitComponent(UnitComponent comp)
    {
        if (!comp)
            return null;
        comp.Owner = this;
        if (UnitComponents.Contains(comp))
            return comp;
        UnitComponents.Add(comp);
        return comp;
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

    #region Lifecycle - Spawn

    /// <summary>
    /// Request to spawn this unit. Will be executed on next UnitModule tick.
    /// </summary>
    public virtual void Spawn()
    {
        if (_lifecycleState != UnitLifecycleState.None)
        {
            Debug.LogWarning($"[UnitBase] Cannot spawn unit '{gameObject.name}' - already in state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Spawning);
        KGameCore.RequireSystem<UnitModule>()._toSpawnUnits.Add(this);
    }

    /// <summary>
    /// Called by UnitModule when spawning is processed. Override to add custom spawn logic.
    /// </summary>
    public virtual void OnSpawn()
    {
        if (_lifecycleState != UnitLifecycleState.Spawning)
        {
            Debug.LogError($"[UnitBase] OnSpawn called on unit '{gameObject.name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Alive);
        
        if (DefaultEnableLogic)
        {
            SetLogicEnable(true);
        }

        // 通知所有 UnitComponent
        foreach (var comp in UnitComponents)
        {
            if (comp != null) comp.OnOwnerSpawn();
        }

        OnUnitSpawn?.Invoke();
    }

    #endregion

    #region Lifecycle - Die

    /// <summary>
    /// Request to kill this unit. Will be executed on next UnitModule tick.
    /// </summary>
    public void Die()
    {
        if (_lifecycleState != UnitLifecycleState.Alive)
        {
            if (_lifecycleState != UnitLifecycleState.Spawning)
            {
                Debug.LogWarning($"[UnitBase] Cannot die unit '{gameObject.name}' - in state {_lifecycleState}");
            }
            return;
        }

        SetLifecycleState(UnitLifecycleState.Dying);
        
        // Disable visual elements immediately
        foreach (var go in DieEnableGameObjects)
        {
            if (go != null) go.SetActive(true);
        }
        foreach (var go in DieDisableGameObjects)
        {
            if (go != null) go.SetActive(false);
        }

        // Stop all timers
        TimerManager.StopAllTimer();
        
        // Kill sub VFX
        var temp = subVFX.ToArray();
        subVFX.Clear();
        foreach (var vfx in temp)
        {
            if (vfx != null) vfx.Die();
        }

        UnitModule.Instance._toDieUnits.Add(this);
        
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

    /// <summary>
    /// Called by UnitModule when die is processed. Override to add custom die logic.
    /// </summary>
    public virtual void OnDie()
    {
        if (_lifecycleState != UnitLifecycleState.Dying)
        {
            Debug.LogError($"[UnitBase] OnDie called on unit '{gameObject.name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Dead);
        
        // 通知所有 UnitComponent
        var comps = UnitComponents.ToArray();
        foreach (var comp in comps)
        {
            if (comp != null) comp.OnOwnerDie();
        }
        
        // End all components
        foreach (var comp in comps)
        {
            if (comp != null) comp.End();
        }
        
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
            Debug.LogWarning($"[UnitBase] Delete called on alive unit '{gameObject.name}' - calling Die first");
            Die();
            return;
        }

        SetLifecycleState(UnitLifecycleState.Deleting);
        UnitModule.Instance._toDeleteUnits.Add(this);
    }

    /// <summary>
    /// Called by UnitModule when deletion is processed. Override to add custom cleanup logic.
    /// </summary>
    public void OnDelete()
    {
        if (_lifecycleState != UnitLifecycleState.Deleting)
        {
            Debug.LogError($"[UnitBase] OnDelete called on unit '{gameObject.name}' in invalid state {_lifecycleState}");
            return;
        }

        SetLifecycleState(UnitLifecycleState.Deleted);
        
        // Disable logic if still enabled
        if (EnableOnLogic)
        {
            SetLogicEnable(false);
        }
        
        // Clear components
        UnitComponents.Clear();
        
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

    public void _Destroy()
    {
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

    #endregion

    #region IPoolable

    /// <summary>
    /// Called by the pool system when this unit is taken from the pool.
    /// Resets lifecycle state so it can be re-spawned.
    /// </summary>
    public virtual void OnGetFromPool()
    {
        // Reset lifecycle state
        _lifecycleState = UnitLifecycleState.None;
        EnableOnLogic = false;
        SelfTimeScale = 1.0f;

        // Reset timers
        TimerManager = new KTimerManager();

        // Reset visual state
        foreach (var go in DieDisableGameObjects)
            if (go != null) go.SetActive(true);
        foreach (var go in DieEnableGameObjects)
            if (go != null) go.SetActive(false);

        // Clear sub VFX
        subVFX.Clear();

        // Re-initialize child UnitComponents
        UnitComponents.Clear();
        if (componentsTransform != null)
        {
            for (int i = 0; i < componentsTransform.childCount; i++)
            {
                InitUnitComponent(componentsTransform.GetChild(i).GetComponent<UnitComponent>());
            }
        }

        // Auto-spawn if configured
        if (SpawnOnStart)
        {
            Spawn();
        }
    }

    /// <summary>
    /// Called by the pool system when this unit is returned to the pool.
    /// Performs cleanup.
    /// </summary>
    public virtual void OnReturnToPool()
    {
        // Disable logic
        EnableOnLogic = false;

        // Stop all timers
        TimerManager.StopAllTimer();

        // Clear components
        var comps = UnitComponents.ToArray();
        foreach (var comp in comps)
        {
            if (comp != null) comp.End();
        }
        UnitComponents.Clear();

        // Clear events (avoid leaking references across pool cycles)
        onLogic = new KSignal();
        OnUnitDie = new KSignal<UnitBase>();
        OnUnitSpawn = new KSignal();

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

    #region Timer System

    public KTimerManager TimerManager = new();
    [HideInInspector]
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
    /// Called every frame by UnitModule if EnableOnLogic is true
    /// </summary>
    public virtual void OnLogic()
    {
        TimerManager.OnLogic(ScaledDeltaTime);
        
        onLogic?.Invoke();
        
        var components = UnitComponents.ToArray();
        foreach (var unitComponent in components)
        {
            if (unitComponent != null && unitComponent.enabled)
            {
                unitComponent.Logic(KTime.scaleDeltaTime);
            }
        }
    }

    #endregion

    #region Unity Lifecycle

    private Transform componentsTransform;
    protected void Awake()
    {
        if(gameObject.name.Contains("(Clone)"))
            gameObject.name = gameObject.name.Replace("(Clone)","");
        
        componentsTransform = transform.Find("__Components__");
        if (componentsTransform == null)
        {
            componentsTransform = new GameObject("__Components__").transform;
            componentsTransform.SetParent(transform, false);
        }

        for (int i = 0; i < componentsTransform.childCount; i++)
        {
            InitUnitComponent(componentsTransform.GetChild(i).GetComponent<UnitComponent>());
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
    
        
        // Draw state indicator sphere
        Color stateColor = GetGizmoColor(_lifecycleState);
        Gizmos.color = stateColor;
        
        Vector3 pos = transform.position;
        Gizmos.DrawWireSphere(pos, 0.3f);
        
        // Draw logic enabled indicator
        if (Application.isPlaying && EnableOnLogic)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(pos + Vector3.up * 0.5f, Vector3.one * 0.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
     
        
        // Draw more detailed info when selected
        Color stateColor = GetGizmoColor(_lifecycleState);
        Gizmos.color = stateColor;
        
        Vector3 pos = transform.position;
        
        // Draw larger sphere
        Gizmos.DrawWireSphere(pos, 0.5f);
        
        // Draw state text
        UnityEditor.Handles.BeginGUI();
        Vector3 screenPos = UnityEditor.HandleUtility.WorldToGUIPoint(pos + Vector3.up);
        GUI.color = stateColor;
        GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 40, 100, 20), 
            _lifecycleState.ToString(), 
            new GUIStyle() { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 12 });
        GUI.color = Color.white;
        UnityEditor.Handles.EndGUI();
        
        // Draw components if enabled
        if (Application.isPlaying && UnitComponents.Count > 0)
        {
            Gizmos.color = Color.yellow;
            float angle = 0f;
            float angleStep = 360f / UnitComponents.Count;
            
            foreach (var comp in UnitComponents)
            {
                if (comp == null) continue;
                
                float rad = angle * Mathf.Deg2Rad;
                Vector3 compPos = pos + new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * 0.7f;
                Gizmos.DrawLine(pos, compPos);
                Gizmos.DrawWireCube(compPos, Vector3.one * 0.1f);
                angle += angleStep;
            }
        }
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