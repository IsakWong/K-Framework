using System;
using Framework.Foundation;
using UnityEngine;

/// <summary>UI Canvas 服务接口 —— CanvasInstance 以接口形式注册到 ServiceLocator，支持替换实现。</summary>
public interface ICanvasService
{
    Canvas BehaviourInstance { get; }
    RectTransform HUDParent { get; }
}

/// <summary>
/// 全局 UI Canvas 单例（挂在场景 Canvas 上）。
/// 迁移自 InstanceBehaviour：继承 PersistentSingleton，实现 ICanvasService。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasInstance : PersistentSingleton<CanvasInstance>, ICanvasService
{
    public Canvas BehaviourInstance => GetComponent<Canvas>();

    [SerializeField] private RectTransform _hudParent;

    public RectTransform HUDParent
    {
        get => _hudParent;
        set => _hudParent = value;
    }

    protected override void Awake()
    {
        if (_hudParent == null)
        {
            var hudParentGameObject = new GameObject("[HUDParent]", typeof(RectTransform));
            HUDParent = hudParentGameObject.GetComponent<RectTransform>();
            HUDParent.sizeDelta = new Vector2(0, 0);
            HUDParent.anchorMin = Vector2.zero;
            HUDParent.anchorMax = Vector2.one;
            HUDParent.localScale = Vector3.one;
            HUDParent.SetParent(transform, false);
        }
        base.Awake();
    }

    protected override void OnServiceInit()
    {
        ServiceLocator.Register<ICanvasService>(this);
    }
}
