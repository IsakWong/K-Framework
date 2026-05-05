using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 基于 DOTween 的 UI 淡入淡出 + 上浮动画。
/// 打开时从下方上浮并淡入，关闭时下浮并淡出。
/// </summary>
public class UIAnimationFade : UIAnimation
{
    [LabelText("缓动曲线")]
    public Ease Ease = Ease.OutCubic;

    [LabelText("上浮距离")]
    public float FloatDistance = 60f;

    [LabelText("位移缓动曲线")]
    public Ease FloatEase = Ease.OutCubic;

    public override async UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        var rect = canvasGroup.GetComponent<RectTransform>();
        var originY = rect.anchoredPosition.y;

        canvasGroup.alpha = 0f;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, originY - FloatDistance);

        var tcs = new UniTaskCompletionSource();
        int completed = 0;
        void TryComplete()
        {
            if (++completed >= 2) tcs.TrySetResult();
        }

        var fadeTween = canvasGroup.DOFade(1f, Duration).SetEase(Ease);
        fadeTween.OnComplete(TryComplete);
        fadeTween.OnKill(TryComplete);

        var moveTween = rect.DOAnchorPosY(originY, Duration).SetEase(FloatEase);
        moveTween.OnComplete(TryComplete);
        moveTween.OnKill(TryComplete);

        await tcs.Task;
    }

    public override async UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        var rect = canvasGroup.GetComponent<RectTransform>();
        var originY = rect.anchoredPosition.y;

        canvasGroup.alpha = 1f;

        var tcs = new UniTaskCompletionSource();
        int completed = 0;
        void TryComplete()
        {
            if (++completed >= 2) tcs.TrySetResult();
        }

        var fadeTween = canvasGroup.DOFade(0f, Duration).SetEase(Ease);
        fadeTween.OnComplete(TryComplete);
        fadeTween.OnKill(TryComplete);

        var moveTween = rect.DOAnchorPosY(originY - FloatDistance, Duration).SetEase(FloatEase);
        moveTween.OnComplete(TryComplete);
        moveTween.OnKill(TryComplete);

        await tcs.Task;

        // 恢复原始锚点位置，防止多次开关后 Y 值累积偏移
        // 此时 alpha=0，用户不可见
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, originY);
    }
}
