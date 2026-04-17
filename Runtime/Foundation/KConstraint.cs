// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2025/12/23

using System;
using UnityEngine;

/// <summary>
/// 约束组件
/// 支持位置、旋转、缩放的跟随，可以独立控制每个约束
/// </summary>
public class KConstraint : MonoBehaviour
{
    #region Constraint Settings
    
    [Header("目标设置")]
    [Tooltip("跟随的目标 Transform")]
    public Transform Target;
    
    [Header("位置约束")]
    [Tooltip("启用位置跟随")]
    public bool ConstrainPosition = true;
    
    [Tooltip("位置偏移")]
    public Vector3 PositionOffset = Vector3.zero;
    
    [Tooltip("X 轴跟随")]
    public bool ConstrainPositionX = true;
    
    [Tooltip("Y 轴跟随")]
    public bool ConstrainPositionY = true;
    
    [Tooltip("Z 轴跟随")]
    public bool ConstrainPositionZ = true;
    
    [Tooltip("位置跟随速度（0 = 立即跟随）")]
    [Range(0f, 50f)]
    public float PositionFollowSpeed = 0f;
    
    [Header("旋转约束")]
    [Tooltip("启用旋转跟随")]
    public bool ConstrainRotation = false;
    
    [Tooltip("旋转偏移（欧拉角）")]
    public Vector3 RotationOffset = Vector3.zero;
    
    [Tooltip("X 轴旋转跟随")]
    public bool ConstrainRotationX = true;
    
    [Tooltip("Y 轴旋转跟随")]
    public bool ConstrainRotationY = true;
    
    [Tooltip("Z 轴旋转跟随")]
    public bool ConstrainRotationZ = true;
    
    [Tooltip("旋转跟随速度（0 = 立即跟随）")]
    [Range(0f, 50f)]
    public float RotationFollowSpeed = 0f;
    
    [Header("缩放约束")]
    [Tooltip("启用缩放跟随")]
    public bool ConstrainScale = false;
    
    [Tooltip("缩放偏移（乘法）")]
    public Vector3 ScaleOffset = Vector3.one;
    
    [Tooltip("X 轴缩放跟随")]
    public bool ConstrainScaleX = true;
    
    [Tooltip("Y 轴缩放跟随")]
    public bool ConstrainScaleY = true;
    
    [Tooltip("Z 轴缩放跟随")]
    public bool ConstrainScaleZ = true;
    
    [Tooltip("缩放跟随速度（0 = 立即跟随）")]
    [Range(0f, 50f)]
    public float ScaleFollowSpeed = 0f;
    
    [Header("高级选项")]
    [Tooltip("使用世界空间坐标")]
    public bool UseWorldSpace = true;
    
    [Tooltip("更新时机")]
    public UpdateTiming Timing = UpdateTiming.LateUpdate;
    
    [Tooltip("仅在目标存在时激活")]
    public bool ActivateOnlyWhenTargetExists = false;
    
    #endregion
    
    #region Private Fields
    
    private Transform _transform;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    private bool _initialized = false;
    
    #endregion
    
    #region Enums
    
    public enum UpdateTiming
    {
        Update,
        LateUpdate,
        FixedUpdate
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        _transform = transform;
        CacheInitialTransform();
    }
    
    private void Update()
    {
        if (Timing == UpdateTiming.Update)
        {
            UpdateConstraints(Time.deltaTime);
        }
    }
    
    public void LateUpdate()
    {
        if (Timing == UpdateTiming.LateUpdate)
        {
            UpdateConstraints(Time.deltaTime);
        }
    }
    
    private void FixedUpdate()
    {
        if (Timing == UpdateTiming.FixedUpdate)
        {
            UpdateConstraints(Time.fixedDeltaTime);
        }
    }
    
    #endregion
    
    #region Initialization
    
    private void CacheInitialTransform()
    {
        if (UseWorldSpace)
        {
            _initialPosition = _transform.position;
            _initialRotation = _transform.rotation;
            _initialScale = _transform.lossyScale;
        }
        else
        {
            _initialPosition = _transform.localPosition;
            _initialRotation = _transform.localRotation;
            _initialScale = _transform.localScale;
        }
        
        _initialized = true;
    }
    
    /// <summary>
    /// 重置初始变换
    /// </summary>
    public void ResetInitialTransform()
    {
        CacheInitialTransform();
    }
    
    #endregion
    
    #region Update Constraints
    
    private void UpdateConstraints(float deltaTime)
    {
        // 检查目标是否存在
        if (Target == null)
        {
            if (ActivateOnlyWhenTargetExists)
            {
                gameObject.SetActive(false);
            }
            return;
        }
        
        if (ActivateOnlyWhenTargetExists && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // 更新位置约束
        if (ConstrainPosition)
        {
            UpdatePositionConstraint(deltaTime);
        }
        
        // 更新旋转约束
        if (ConstrainRotation)
        {
            UpdateRotationConstraint(deltaTime);
        }
        
        // 更新缩放约束
        if (ConstrainScale)
        {
            UpdateScaleConstraint(deltaTime);
        }
    }
    
    #endregion
    
    #region Position Constraint
    
    private void UpdatePositionConstraint(float deltaTime)
    {
        Vector3 targetPosition;
        Vector3 currentPosition;
        
        if (UseWorldSpace)
        {
            targetPosition = Target.position + PositionOffset;
            currentPosition = _transform.position;
        }
        else
        {
            targetPosition = Target.localPosition + PositionOffset;
            currentPosition = _transform.localPosition;
        }
        
        // 选择性跟随轴
        Vector3 newPosition = currentPosition;
        
        if (ConstrainPositionX)
        {
            newPosition.x = targetPosition.x;
        }
        
        if (ConstrainPositionY)
        {
            newPosition.y = targetPosition.y;
        }
        
        if (ConstrainPositionZ)
        {
            newPosition.z = targetPosition.z;
        }
        
        // 应用跟随速度
        if (PositionFollowSpeed > 0f)
        {
            newPosition = Vector3.Lerp(currentPosition, newPosition, PositionFollowSpeed * deltaTime);
        }
        
        // 应用位置
        if (UseWorldSpace)
        {
            _transform.position = newPosition;
        }
        else
        {
            _transform.localPosition = newPosition;
        }
    }
    
    #endregion
    
    #region Rotation Constraint
    
    private void UpdateRotationConstraint(float deltaTime)
    {
        Quaternion targetRotation;
        Quaternion currentRotation;
        
        // 获取目标旋转并应用偏移
        Quaternion offsetRotation = Quaternion.Euler(RotationOffset);
        
        if (UseWorldSpace)
        {
            targetRotation = Target.rotation * offsetRotation;
            currentRotation = _transform.rotation;
        }
        else
        {
            targetRotation = Target.localRotation * offsetRotation;
            currentRotation = _transform.localRotation;
        }
        
        // 转换为欧拉角以便选择性跟随
        Vector3 targetEuler = targetRotation.eulerAngles;
        Vector3 currentEuler = currentRotation.eulerAngles;
        Vector3 newEuler = currentEuler;
        
        if (ConstrainRotationX)
        {
            newEuler.x = targetEuler.x;
        }
        
        if (ConstrainRotationY)
        {
            newEuler.y = targetEuler.y;
        }
        
        if (ConstrainRotationZ)
        {
            newEuler.z = targetEuler.z;
        }
        
        Quaternion newRotation = Quaternion.Euler(newEuler);
        
        // 应用跟随速度
        if (RotationFollowSpeed > 0f)
        {
            newRotation = Quaternion.Slerp(currentRotation, newRotation, RotationFollowSpeed * deltaTime);
        }
        
        // 应用旋转
        if (UseWorldSpace)
        {
            _transform.rotation = newRotation;
        }
        else
        {
            _transform.localRotation = newRotation;
        }
    }
    
    #endregion
    
    #region Scale Constraint
    
    private void UpdateScaleConstraint(float deltaTime)
    {
        Vector3 targetScale;
        Vector3 currentScale;
        
        if (UseWorldSpace)
        {
            targetScale = Target.lossyScale;
            currentScale = _transform.lossyScale;
            
            // 世界空间缩放需要特殊处理
            // 因为 lossyScale 是只读的，我们需要通过 localScale 来间接影响
            Vector3 targetLocalScale = new Vector3(
                targetScale.x * ScaleOffset.x,
                targetScale.y * ScaleOffset.y,
                targetScale.z * ScaleOffset.z
            );
            
            // 获取父物体的缩放
            Vector3 parentScale = _transform.parent != null ? _transform.parent.lossyScale : Vector3.one;
            
            // 计算需要的 localScale
            targetLocalScale = new Vector3(
                targetLocalScale.x / Mathf.Max(parentScale.x, 0.0001f),
                targetLocalScale.y / Mathf.Max(parentScale.y, 0.0001f),
                targetLocalScale.z / Mathf.Max(parentScale.z, 0.0001f)
            );
            
            currentScale = _transform.localScale;
            targetScale = targetLocalScale;
        }
        else
        {
            targetScale = new Vector3(
                Target.localScale.x * ScaleOffset.x,
                Target.localScale.y * ScaleOffset.y,
                Target.localScale.z * ScaleOffset.z
            );
            currentScale = _transform.localScale;
        }
        
        // 选择性跟随轴
        Vector3 newScale = currentScale;
        
        if (ConstrainScaleX)
        {
            newScale.x = targetScale.x;
        }
        
        if (ConstrainScaleY)
        {
            newScale.y = targetScale.y;
        }
        
        if (ConstrainScaleZ)
        {
            newScale.z = targetScale.z;
        }
        
        // 应用跟随速度
        if (ScaleFollowSpeed > 0f)
        {
            newScale = Vector3.Lerp(currentScale, newScale, ScaleFollowSpeed * deltaTime);
        }
        
        // 应用缩放（总是使用 localScale）
        _transform.localScale = newScale;
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 设置目标
    /// </summary>
    public void SetTarget(Transform target)
    {
        Target = target;
    }
    
    /// <summary>
    /// 启用/禁用所有约束
    /// </summary>
    public void SetConstraintsEnabled(bool enabled)
    {
        ConstrainPosition = enabled;
        ConstrainRotation = enabled;
        ConstrainScale = enabled;
    }
    
    /// <summary>
    /// 立即跟随到目标位置（无插值）
    /// </summary>
    public void SnapToTarget()
    {
        if (Target == null) return;
        
        float originalPosSpeed = PositionFollowSpeed;
        float originalRotSpeed = RotationFollowSpeed;
        float originalScaleSpeed = ScaleFollowSpeed;
        
        PositionFollowSpeed = 0f;
        RotationFollowSpeed = 0f;
        ScaleFollowSpeed = 0f;
        
        UpdateConstraints(0f);
        
        PositionFollowSpeed = originalPosSpeed;
        RotationFollowSpeed = originalRotSpeed;
        ScaleFollowSpeed = originalScaleSpeed;
    }
    
    /// <summary>
    /// 重置到初始变换
    /// </summary>
    public void ResetToInitial()
    {
        if (!_initialized) return;
        
        if (UseWorldSpace)
        {
            _transform.position = _initialPosition;
            _transform.rotation = _initialRotation;
        }
        else
        {
            _transform.localPosition = _initialPosition;
            _transform.localRotation = _initialRotation;
            _transform.localScale = _initialScale;
        }
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (Target == null) return;
        
        // 绘制连线
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, Target.position);
        
        // 绘制目标位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Target.position, 0.1f);
        
        // 绘制约束信息
        if (ConstrainPosition)
        {
            Vector3 targetPos = UseWorldSpace ? Target.position + PositionOffset : Target.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPos, 0.05f);
        }
    }
    
    #endregion
}