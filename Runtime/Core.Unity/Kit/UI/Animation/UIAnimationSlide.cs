using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 基于 DOTween 的 UI 侧滑动画。
/// 打开时从侧边滑入，关闭时反向滑出。
/// </summary>
public class UIAnimationSlide : UIAnimation
{
    public enum SlideDirection
    {
        [LabelText("右 → 左")]
        RightToLeft,
        [LabelText("左 → 右")]
        LeftToRight,
        [LabelText("上 → 下")]
        TopToBottom,
        [LabelText("下 → 上")]
        BottomToTop,
    }

    [LabelText("滑动方向")]
    public SlideDirection Direction = SlideDirection.RightToLeft;

    [LabelText("缓动曲线")]
    public Ease Ease = Ease.OutCubic;

    [LabelText("同时淡入淡出")]
    public bool WithFade = true;

    public override async UniTask PlayOpenAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        var rect = canvasGroup.GetComponent<RectTransform>();
        var originPos = rect.anchoredPosition;
        var offset = GetSlideOffset(rect);

        if (WithFade)
            canvasGroup.alpha = 0f;

        rect.anchoredPosition = originPos + offset;

        var tcs = new UniTaskCompletionSource();
        int total = WithFade ? 2 : 1;
        int completed = 0;
        void TryComplete()
        {
            if (++completed >= total) tcs.TrySetResult();
        }

        var moveTween = rect.DOAnchorPos(originPos, Duration).SetEase(Ease);
        moveTween.OnComplete(TryComplete);
        moveTween.OnKill(TryComplete);

        if (WithFade)
        {
            var fadeTween = canvasGroup.DOFade(1f, Duration).SetEase(Ease);
            fadeTween.OnComplete(TryComplete);
            fadeTween.OnKill(TryComplete);
        }

        await tcs.Task;
    }

    public override async UniTask PlayCloseAsync(CanvasGroup canvasGroup, CancellationToken ct = default)
    {
        var rect = canvasGroup.GetComponent<RectTransform>();
        var originPos = rect.anchoredPosition;
        var offset = GetSlideOffset(rect);

        var tcs = new UniTaskCompletionSource();
        int total = WithFade ? 2 : 1;
        int completed = 0;
        void TryComplete()
        {
            if (++completed >= total) tcs.TrySetResult();
        }

        var moveTween = rect.DOAnchorPos(originPos + offset, Duration).SetEase(Ease);
        moveTween.OnComplete(TryComplete);
        moveTween.OnKill(TryComplete);

        if (WithFade)
        {
            var fadeTween = canvasGroup.DOFade(0f, Duration).SetEase(Ease);
            fadeTween.OnComplete(TryComplete);
            fadeTween.OnKill(TryComplete);
        }

        await tcs.Task;

        rect.anchoredPosition = originPos;
    }

    private Vector2 GetSlideOffset(RectTransform rect)
    {
        var size = rect.rect.size;
        return Direction switch
        {
            SlideDirection.RightToLeft => new Vector2(size.x, 0f),
            SlideDirection.LeftToRight => new Vector2(-size.x, 0f),
            SlideDirection.TopToBottom => new Vector2(0f, -size.y),
            SlideDirection.BottomToTop => new Vector2(0f, size.y),
            _ => Vector2.zero,
        };
    }
}
