using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 基于 DOTween 的 UI 淡入淡出动画。
/// 通过 CanvasGroup.alpha 实现，OnComplete/OnKill 统一用 UniTaskCompletionSource 等待。
/// 如需更复杂的入场特效，可继承 UIAnimation 实现 MMF_Player 子类。
/// </summary>
public class UIAnimationFade : UIAnimation
{
    [LabelText("缓动曲线")]
    public Ease Ease = Ease.OutCubic;

    public override async UniTask PlayShowAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        canvasGroup.alpha = 0f;
        var tcs = new UniTaskCompletionSource();
        var tween = canvasGroup.DOFade(1f, Duration).SetEase(Ease);
        tween.OnComplete(() => tcs.TrySetResult());
        tween.OnKill(() => tcs.TrySetResult());
        await tcs.Task;
    }

    public override async UniTask PlayHideAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        canvasGroup.alpha = 1f;
        var tcs = new UniTaskCompletionSource();
        var tween = canvasGroup.DOFade(0f, Duration).SetEase(Ease);
        tween.OnComplete(() => tcs.TrySetResult());
        tween.OnKill(() => tcs.TrySetResult());
        await tcs.Task;
    }
}
