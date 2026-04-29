using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// UI 动画接口，定义 Panel 显隐动画协议。
/// 子类可实现 DOTween、MMF_Player、自定义过渡等。
/// </summary>
public interface IUIAnimation
{
    /// <summary>播放显示动画，完成后返回</summary>
    UniTask PlayShowAsync(CanvasGroup canvasGroup, CancellationToken ct = default);

    /// <summary>播放隐藏动画，完成后返回</summary>
    UniTask PlayHideAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
}

/// <summary>
/// UI 动画抽象基类，提供动画生命周期信号和公共参数。
/// </summary>
public abstract class UIAnimation : IUIAnimation
{
    [LabelText("动画时长")]
    public float Duration = 0.3f;

    [LabelText("显示动画开始")]
    public KSignal<UIPanel> OnShowStart = new();

    [LabelText("显示动画结束")]
    public KSignal<UIPanel> OnShowEnd = new();

    [LabelText("隐藏动画开始")]
    public KSignal<UIPanel> OnHideStart = new();

    [LabelText("隐藏动画结束")]
    public KSignal<UIPanel> OnHideEnd = new();

    public abstract UniTask PlayShowAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
    public abstract UniTask PlayHideAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
}
