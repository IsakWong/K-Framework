using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 一套动作的基类，支持协程执行方式。
/// </summary>
[Serializable]
public class KActionBase
{
    /// <summary>
    /// 时间点插入位置（-1 表示顺序执行，>= 0 表示在指定时间点插入）
    /// </summary>
    public float InsertPoint = -1;
    
    /// <summary>
    /// 由执行序列在运行时注入的拥有者（通常为触发该序列的 MonoBehaviour，比如 KTrigger）
    /// </summary>
    [NonSerialized]
    public MonoBehaviour Owner;

    public virtual float GetDuration()
    {
        return 1;
    }
    
    /// <summary>
    /// 执行 Action 的协程
    /// </summary>
    public virtual IEnumerator ExecuteCoroutine()
    {
        yield break;
    }
}

[Serializable]
public class KActionSequence
{
    [NonSerialized] public MonoBehaviour Owner;

    [SerializeReference]
    public List<KActionBase> AllActions = new List<KActionBase>();
    
    private List<KActionBase> normalActions;
    private List<KActionBase> insertActions;

    /// <summary>
    /// 初始化并分类 Actions
    /// </summary>
    public void Initialize()
    {
        normalActions = new List<KActionBase>();
        insertActions = new List<KActionBase>();

        foreach (var action in AllActions)
        {
            if (action == null) continue;

            // 通过 InsertPoint 判断是否为时间点插入
            if (action.InsertPoint >= 0)
            {
                insertActions.Add(action);
            }
            else
            {
                normalActions.Add(action);
            }
        }

        // normalActions 保持 List 中的顺序

        // 排序时间轴插入的 Actions（按 InsertPoint）
        insertActions.Sort((a, b) => a.InsertPoint.CompareTo(b.InsertPoint));
    }

    /// <summary>
    /// 执行所有 Actions（使用 Owner 启动协程）
    /// </summary>
    public void Execute(MonoBehaviour ownerMono)
    {
        Owner = ownerMono;
        Initialize();
        ownerMono.StartCoroutine(ExecuteWithContext());
    }

    /// <summary>
    /// 执行所有 Actions（需要传入 MonoBehaviour 来启动协程）
    /// </summary>
    public IEnumerator ExecuteWithContext()
    {
        // 1. 先执行所有顺序 Actions
        foreach (var action in normalActions)
        {
            if (action == null) continue;
            yield return Owner.StartCoroutine(action.ExecuteCoroutine());
        }

    }

    /// <summary>
    /// 使用指定的 MonoBehaviour 执行时间轴插入的 Actions
    /// </summary>
    private IEnumerator ExecuteInsertActionsWithContext()
    {
        float startTime = UnityEngine.Time.time;
        int insertIndex = 0;

        // 计算最大结束时间
        float maxEndTime = 0f;
        foreach (var action in insertActions)
        {
            float endTime = action.InsertPoint + action.GetDuration();
            if (endTime > maxEndTime)
            {
                maxEndTime = endTime;
            }
        }

        // 按时间点启动 Actions
        while (insertIndex < insertActions.Count || UnityEngine.Time.time - startTime < maxEndTime)
        {
            float elapsedTime = UnityEngine.Time.time - startTime;

            // 启动所有到达时间点的 Actions
            while (insertIndex < insertActions.Count && insertActions[insertIndex].InsertPoint <= elapsedTime)
            {
                var action = insertActions[insertIndex];
                if (action != null)
                {
                    Owner.StartCoroutine(action.ExecuteCoroutine());
                }

                insertIndex++;
            }

            yield return null;
        }

        // 确保等待到最后一个 Action 完成
        float remainingTime = maxEndTime - (UnityEngine.Time.time - startTime);
        if (remainingTime > 0)
        {
            yield return new UnityEngine.WaitForSeconds(remainingTime);
        }
    }

    /// <summary>
    /// 获取总持续时间
    /// </summary>
    public float GetTotalDuration()
    {
        Initialize();

        float totalDuration = 0f;

        // 顺序执行的 Actions 时间累加
        foreach (var action in normalActions)
        {
            if (action != null)
            {
                totalDuration += action.GetDuration();
            }
        }

        // 时间轴插入的 Actions 取最大结束时间
        float maxInsertEndTime = 0f;
        foreach (var action in insertActions)
        {
            if (action != null)
            {
                float endTime = action.InsertPoint + action.GetDuration();
                if (endTime > maxInsertEndTime)
                {
                    maxInsertEndTime = endTime;
                }
            }
        }

        totalDuration += maxInsertEndTime;

        return totalDuration;
    }

    /// <summary>
    /// 添加顺序执行的 Action
    /// </summary>
    public void AddNormalAction(KActionBase action)
    {
        action.InsertPoint = -1;
        AllActions.Add(action);
    }

    /// <summary>
    /// 添加时间点插入的 Action
    /// </summary>
    public void AddInsertAction(KActionBase action, float insertPoint)
    {
        action.InsertPoint = insertPoint;
        AllActions.Add(action);
    }

    /// <summary>
    /// 清空所有 Actions
    /// </summary>
    public void Clear()
    {
        AllActions.Clear();
        if (normalActions != null) normalActions.Clear();
        if (insertActions != null) insertActions.Clear();
    }
}