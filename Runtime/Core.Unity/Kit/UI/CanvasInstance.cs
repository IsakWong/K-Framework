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
/// 全局 UI Canvas 单例（挂在场景 Canvas 上）。场景有配置就用场景的，没有才自动创建。
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

    protected override bool TryReplaceExisting(CanvasInstance existing)
    {
        // 已有跨场景 CanvasInstance（含先前场景并入的子物体）：保留它。
        // 把本场景 Canvas 的子物体 reparent 进去后返回 false，由基类销毁本对象。
        // 跳过运行时自动创建的 [HUDParent]（空容器，避免重复）。
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child == _hudParent) continue;
            child.SetParent(existing.transform, false); // UI 保持本地坐标，避免因 Canvas 缩放差异漂移
        }

        gameObject.SetActive(false); // 立即隐藏，避免帧末销毁前双 Canvas 渲染
        return false;
    }

    private void Start()
    {
        if (ServiceLocator.TryGet<ICameraService>(out var cameraService))
        {
            GetComponent<Canvas>().worldCamera = cameraService.BehaviourInstance;
        }
    }

    protected override void OnServiceInit()
    {
        ServiceLocator.Register<ICanvasService>(this);
    }
}
