using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all UnitBase instances in the game.
/// Handles deferred lifecycle operations (Spawn, Die, Delete) and logic updates.
/// Provides global time scale management for all units.
/// </summary>
public class UnitModule : TModule<UnitModule>
{
    #region Unit Collections

    // All active units (spawned but not deleted)
    private readonly List<UnitBase> _units = new();
    
    // Units that have logic enabled (subset of _units)
    private List<UnitBase> _logicUnits = new();
    
    // Pending operations queues
    public List<UnitBase> _toSpawnUnits = new();
    public List<UnitBase> _toDieUnits = new();
    public List<UnitBase> _toDeleteUnits = new();
    public List<UnitBase> logicDirtyUnits = new();

    #endregion

    #region Global Time Scale Management

    /// <summary>
    /// 全局时间缩放倍率（影响所有 Unit）
    /// </summary>
    [Header("时间缩放管理")]
    [SerializeField, Range(0f, 5f), Tooltip("全局时间缩放倍率")]
    private float _globalTimeScale = 1f;
    
    /// <summary>
    /// 临时时间缩放状态
    /// </summary>
    private struct TemporaryTimeScale
    {
        public float TargetScale;     // 目标缩放值
        public float Duration;        // 持续时间
        public float ElapsedTime;     // 已经过时间
        public float PreviousScale;   // 之前的缩放值
        public bool IsActive;         // 是否激活中
        
        public TemporaryTimeScale(float targetScale, float duration, float previousScale)
        {
            TargetScale = targetScale;
            Duration = duration;
            ElapsedTime = 0f;
            PreviousScale = previousScale;
            IsActive = true;
        }
    }
    
    private TemporaryTimeScale _tempTimeScale;
    
    /// <summary>
    /// 当前有效的全局时间缩放值
    /// </summary>
    public float GlobalTimeScale => _globalTimeScale;
    
    /// <summary>
    /// 是否有临时时间缩放正在进行
    /// </summary>
    public bool HasTemporaryTimeScale => _tempTimeScale.IsActive;
    
    /// <summary>
    /// 临时时间缩放的剩余时间
    /// </summary>
    public float TemporaryTimeScaleRemaining => _tempTimeScale.IsActive ? 
        Mathf.Max(0f, _tempTimeScale.Duration - _tempTimeScale.ElapsedTime) : 0f;
        
    // 事件回调
    public System.Action<float> OnGlobalTimeScaleChanged;
    public System.Action<float, float> OnTemporaryTimeScaleStarted; // (targetScale, duration)
    public System.Action OnTemporaryTimeScaleEnded;

    #endregion

    #region Events

    public Action<UnitBase> OnAddUnit;
    public Action<UnitBase> OnRemoveUnit;

    #endregion

    #region Public Properties

    public IReadOnlyList<UnitBase> UnitList => _units;
    public IReadOnlyCollection<UnitBase> LogicUnitList => _logicUnits;

    #endregion

    #region Main Update Loop

    public void FixedUpdate()
    {
        UpdateGlobalTimeScale();
        ProcessLifecycleOperations();
        ProcessLogicUpdates();
    }

    #endregion

    #region Global Time Scale Methods

    /// <summary>
    /// 设置全局时间缩放
    /// </summary>
    /// <param name="timeScale">时间缩放值（0为暂停，1为正常，>1为加速，<1为减速）</param>
    /// <param name="updateUnits">是否立即更新所有 Unit 的时间缩放</param>
    public void SetGlobalTimeScale(float timeScale, bool updateUnits = true)
    {
        float oldScale = _globalTimeScale;
        _globalTimeScale = Mathf.Max(0f, timeScale);
        
        if (Mathf.Abs(oldScale - _globalTimeScale) > 0.001f)
        {
            OnGlobalTimeScaleChanged?.Invoke(_globalTimeScale);
            
            if (updateUnits)
            {
                UpdateAllUnitsTimeScale();
            }
            
            Debug.Log($"[UnitModule] Global time scale changed from {oldScale:F2} to {_globalTimeScale:F2}");
        }
    }
    
    /// <summary>
    /// 设置临时时间缩放（会在指定时间后自动恢复）
    /// </summary>
    /// <param name="targetScale">目标时间缩放值</param>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="updateUnits">是否立即更新所有 Unit 的时间缩放</param>
    public void SetTemporaryTimeScale(float targetScale, float duration, bool updateUnits = true)
    {
        if (duration <= 0f)
        {
            Debug.LogWarning("[UnitModule] Temporary time scale duration must be greater than 0");
            return;
        }
        
        // 保存当前缩放值作为恢复目标
        float previousScale = _tempTimeScale.IsActive ? _tempTimeScale.PreviousScale : _globalTimeScale;
        
        // 设置临时缩放状态
        _tempTimeScale = new TemporaryTimeScale(Mathf.Max(0f, targetScale), duration, previousScale);
        
        // 立即应用新的时间缩放
        _globalTimeScale = _tempTimeScale.TargetScale;
        
        OnTemporaryTimeScaleStarted?.Invoke(targetScale, duration);
        OnGlobalTimeScaleChanged?.Invoke(_globalTimeScale);
        
        if (updateUnits)
        {
            UpdateAllUnitsTimeScale();
        }
        
        Debug.Log($"[UnitModule] Temporary time scale set to {targetScale:F2} for {duration:F2} seconds");
    }
    
    /// <summary>
    /// 取消当前的临时时间缩放，立即恢复
    /// </summary>
    /// <param name="updateUnits">是否立即更新所有 Unit 的时间缩放</param>
    public void CancelTemporaryTimeScale(bool updateUnits = true)
    {
        if (!_tempTimeScale.IsActive)
        {
            return;
        }
        
        // 恢复到之前的时间缩放
        _globalTimeScale = _tempTimeScale.PreviousScale;
        _tempTimeScale.IsActive = false;
        
        OnTemporaryTimeScaleEnded?.Invoke();
        OnGlobalTimeScaleChanged?.Invoke(_globalTimeScale);
        
        if (updateUnits)
        {
            UpdateAllUnitsTimeScale();
        }
        
        Debug.Log($"[UnitModule] Temporary time scale cancelled, restored to {_globalTimeScale:F2}");
    }
    
    /// <summary>
    /// 更新临时时间缩放状态
    /// </summary>
    private void UpdateGlobalTimeScale()
    {
        if (!_tempTimeScale.IsActive)
            return;
        
        _tempTimeScale.ElapsedTime += Time.fixedDeltaTime;
        
        // 检查是否到期
        if (_tempTimeScale.ElapsedTime >= _tempTimeScale.Duration)
        {
            // 恢复到之前的时间缩放
            _globalTimeScale = _tempTimeScale.PreviousScale;
            _tempTimeScale.IsActive = false;
            
            OnTemporaryTimeScaleEnded?.Invoke();
            OnGlobalTimeScaleChanged?.Invoke(_globalTimeScale);
            
            UpdateAllUnitsTimeScale();
            
            Debug.Log($"[UnitModule] Temporary time scale expired, restored to {_globalTimeScale:F2}");
        }
    }
    
    /// <summary>
    /// 更新所有 Unit 的时间缩放（将全局缩放应用到每个 Unit 的 SelfTimeScale）
    /// </summary>
    private void UpdateAllUnitsTimeScale()
    {
        foreach (var unit in _units)
        {
            if (unit != null && !unit.IsDeleted)
            {
                // 注意：这里我们不直接覆盖 SelfTimeScale，而是提供一个新的属性来计算最终的时间缩放
                // 这样每个 Unit 仍然可以有自己的 SelfTimeScale，同时受到全局缩放影响
            }
        }
    }
    
    /// <summary>
    /// 获取某个 Unit 的最终时间缩放值（全局缩放 * 自身缩放）
    /// </summary>
    /// <param name="unit">目标 Unit</param>
    /// <returns>最终的时间缩放值</returns>
    public float GetFinalTimeScale(UnitBase unit)
    {
        if (unit == null)
            return _globalTimeScale;
        
        return _globalTimeScale * unit.SelfTimeScale;
    }
    
    /// <summary>
    /// 获取某个 Unit 的最终 ScaledDeltaTime
    /// </summary>
    /// <param name="unit">目标 Unit</param>
    /// <returns>最终的 ScaledDeltaTime</returns>
    public float GetFinalScaledDeltaTime(UnitBase unit)
    {
        if (unit == null)
            return KTime.scaleDeltaTime * _globalTimeScale;
        
        float finalTimeScale = GetFinalTimeScale(unit);
        if (finalTimeScale == 0)
            return 0;
        
        return KTime.scaleDeltaTime * finalTimeScale;
    }

    #endregion

    #region Lifecycle Processing

    /// <summary>
    /// Process all pending lifecycle operations (Spawn, Die, Delete)
    /// </summary>
    private void ProcessLifecycleOperations()
    {
        // 1. Process Spawn operations
        ProcessSpawnQueue();
        
        // 2. Process Die operations
        ProcessDieQueue();
        
        // 3. Process Delete operations
        ProcessDeleteQueue();
        
        // 4. Process logic enable/disable changes
        ProcessLogicDirtyUnits();
    }

    /// <summary>
    /// Process units waiting to spawn
    /// </summary>
    private void ProcessSpawnQueue()
    {
        if (_toSpawnUnits.Count == 0)
            return;

        var temp = _toSpawnUnits.ToArray();
        _toSpawnUnits.Clear();

        foreach (var unit in temp)
        {
            if (unit == null)
            {
                Debug.LogWarning("[UnitModule] Null unit in spawn queue");
                continue;
            }

            if (unit.LifecycleState != UnitLifecycleState.Spawning)
            {
                Debug.LogWarning($"[UnitModule] Unit '{unit.gameObject.name}' in spawn queue has invalid state {unit.LifecycleState}");
                continue;
            }

            unit.OnSpawn();
            _units.Add(unit);
            OnAddUnit?.Invoke(unit);
        }
    }

    /// <summary>
    /// Process units waiting to die
    /// </summary>
    private void ProcessDieQueue()
    {
        if (_toDieUnits.Count == 0)
            return;

        var temp = _toDieUnits.ToArray();
        _toDieUnits.Clear();

        foreach (var unit in temp)
        {
            if (unit == null)
            {
                Debug.LogWarning("[UnitModule] Null unit in die queue");
                continue;
            }

            if (unit.LifecycleState != UnitLifecycleState.Dying)
            {
                Debug.LogWarning($"[UnitModule] Unit '{unit.gameObject.name}' in die queue has invalid state {unit.LifecycleState}");
                continue;
            }

            unit.OnDie();
        }
    }

    /// <summary>
    /// Process units waiting to be deleted
    /// </summary>
    private void ProcessDeleteQueue()
    {
        if (_toDeleteUnits.Count == 0)
            return;

        var temp = _toDeleteUnits.ToArray();
        _toDeleteUnits.Clear();

        foreach (var unit in temp)
        {
            if (unit == null)
            {
                Debug.LogWarning("[UnitModule] Null unit in delete queue");
                continue;
            }

            // Remove from main unit list
            _units.Remove(unit);
            
            // Remove from logic units if present
            _logicUnits.Remove(unit);
            
            // Remove from dirty list if present
            logicDirtyUnits.Remove(unit);
            
            // Call OnDelete
            unit.OnDelete();
            
            OnRemoveUnit?.Invoke(unit);
        }
        // Safety assertions in debug builds
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (var unit in temp)
        {
            if (unit == null) continue;
            
            Debug.Assert(!_logicUnits.Contains(unit), 
                $"Unit '{unit.gameObject.name}' still in logic units after deletion");
            Debug.Assert(!_units.Contains(unit), 
                $"Unit '{unit.gameObject.name}' still in units list after deletion");
            Debug.Assert(!logicDirtyUnits.Contains(unit), 
                $"Unit '{unit.gameObject.name}' still in dirty list after deletion");
        }
        #endif
    }

    /// <summary>
    /// Process units with logic enable/disable changes
    /// </summary>
    private void ProcessLogicDirtyUnits()
    {
        if (logicDirtyUnits.Count == 0)
            return;

        foreach (var unit in logicDirtyUnits)
        {
            if (unit == null || unit.IsDeleted)
                continue;

            if (unit.EnableOnLogic)
            {
                // Add to logic units if not already present
                if (!_logicUnits.Contains(unit))
                {
                    _logicUnits.Add(unit);
                }
            }
            else
            {
                // Remove from logic units if present
                _logicUnits.Remove(unit);
            }
        }

        logicDirtyUnits.Clear();
    }

    #endregion

    #region Logic Updates

    /// <summary>
    /// Update all units that have logic enabled
    /// </summary>
    private void ProcessLogicUpdates()
    {
        if (_logicUnits.Count == 0)
            return;

        // Create array copy to allow modifications during iteration
        var temp = _logicUnits.ToArray();
        
        foreach (var unit in temp)
        {
            // Skip null or deleted units
            if (unit == null || unit.IsDeleted)
                continue;
            unit.OnLogic();
        }
    }

    /// <summary>
    /// Alternative method name for compatibility
    /// </summary>
    public void ManualPreLogic()
    {
        ProcessLifecycleOperations();
    }

    /// <summary>
    /// Alternative method name for compatibility
    /// </summary>
    public void ManualLogic()
    {
        ProcessLogicUpdates();
    }

    #endregion

    #region Module Lifecycle

    public override bool RequestShutdown()
    {
        // Delete all remaining units
        if (_units.Count > 0)
        {
            var unitsToDelete = _units.ToArray();
            foreach (var unit in unitsToDelete)
            {
                if(unit == null)
                    _units.Remove(unit);
                
                if (unit != null && !unit.IsDeleted)
                {
                    unit.Delete();
                }
            }

            // Give units one frame to clean up
            return false;
        }

        return true;
    }

    #endregion

    #region Debug/Utility

    /// <summary>
    /// Get statistics about current unit state
    /// </summary>
    public string GetStatistics()
    {
        return $"Units: {_units.Count} | Logic Units: {_logicUnits.Count} | " +
               $"Spawn Queue: {_toSpawnUnits.Count} | Die Queue: {_toDieUnits.Count} | " +
               $"Delete Queue: {_toDeleteUnits.Count} | Dirty: {logicDirtyUnits.Count}";
    }

    #endregion
}