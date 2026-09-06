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
    /// <summary>播放打开动画，完成后返回</summary>
    UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default);

    /// <summary>播放关闭动画，完成后返回</summary>
    UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
}

/// <summary>
/// UI 动画抽象基类，提供动画生命周期信号和公共参数。
/// </summary>
public abstract class UIAnimation : IUIAnimation
{
    [LabelText("动画时长")]
    public float Duration = 0.3f;

    [LabelText("打开动画开始")]
    public KSignal<UIPanel> OnOpenStart = new();

    [LabelText("打开动画结束")]
    public KSignal<UIPanel> OnOpenEnd = new();

    [LabelText("关闭动画开始")]
    public KSignal<UIPanel> OnCloseStart = new();

    [LabelText("关闭动画结束")]
    public KSignal<UIPanel> OnCloseEnd = new();

    public abstract UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
    public abstract UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default);
}
