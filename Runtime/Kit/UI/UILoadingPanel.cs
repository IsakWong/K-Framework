using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingPanel : UIPanel
{
    public float MinLoadTime = 3.0f;

    public Image LoadingMask;
    public Animator Animator;
    public Text LoadingText;

    [Header("色块动画")]
    public float ShowDuration = 0.35f;
    public float HideDuration = 0.3f;
    public Ease ShowEase = Ease.OutCubic;
    public Ease HideEase = Ease.InCubic;

    public void SetLoadingTip(string text)
    {
        if (LoadingText)
            LoadingText.text = text;
    }

    private float _openedAt = float.MinValue;

    // 入场/退出动画的 tween 引用。hide 前先结束 show，避免两者争抢同一属性导致退出动画异常
    private Tween _showTween;
    private Tween _hideTween;

    /// <summary>
    /// 自本次打开起已展示的时长（秒）。供关闭方据此保证最短展示时间。
    /// </summary>
    public float ShownDuration => _openedAt < 0f ? 0f : Time.time - _openedAt;

    protected override void OnOpen()
    {
        base.OnOpen();
        _openedAt = Time.time;
        PlayShowAnimation();
    }

    protected override UIAnimation GetEffectiveAnimation()
    {
        return null;
    }

    protected override void OnClose()
    {
        _openedAt = float.MinValue;
        base.OnClose();
        _loadingCloseCallback?.Invoke();
    }

    /// <summary>
    /// 等待入场动画完整播完（已播完或被中断则立即通过）。
    /// 供调用方在播放退出动画前调用，保证遮罩先盖满屏幕再滑出。
    /// </summary>
    public IEnumerator WaitShowComplete()
    {
        while (_showTween != null && _showTween.IsActive() && !_showTween.IsComplete())
            yield return null;
    }

    public void PlayShowAnimation()
    {
        if (LoadingMask == null)
        {
            Debug.LogWarning("[UILoadingPanel] PlayShowAnimation: LoadingMask 为空");
            return;
        }
        var rt = LoadingMask.rectTransform;
        float w = rt.rect.width > 1 ? rt.rect.width : Screen.width;
        Debug.Log($"[UILoadingPanel] 色块从左侧滑入, width={w}, rectWidth={rt.rect.width}");
        rt.anchoredPosition = new Vector2(-w, 0);
        _showTween?.Kill();
        _showTween = rt.DOAnchorPosX(0, ShowDuration).SetEase(ShowEase);
    }

    public void PlayHideAnimation(Action onComplete = null)
    {
        if (LoadingMask == null)
        {
            Debug.LogWarning("[UILoadingPanel] PlayHideAnimation: LoadingMask 为空");
            onComplete?.Invoke();
            return;
        }
        var rt = LoadingMask.rectTransform;
        float w = rt.rect.width > 1 ? rt.rect.width : Screen.width;
        Debug.Log($"[UILoadingPanel] 色块向右侧滑出, width={w}");

        // 先结束仍在进行的入场动画，避免它与退出 tween 争抢同一属性，
        // 否则退出动画会被入场动画拉回/打断，表现为"没反向滑出就直接消失"
        _showTween?.Kill();
        _hideTween?.Kill();
        _hideTween = rt.DOAnchorPosX(w, HideDuration).SetEase(HideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private IEnumerator _task;
    private Action _loadingCloseCallback;

    private IEnumerator WaitTask(bool autoHideUI)
    {
        float startTime = Time.time;
        yield return new WaitForSeconds(0.3f);
        yield return _task;
        float remainTime = MinLoadTime - (Time.time - startTime);
        if (remainTime > 0)
            yield return new WaitForSeconds(remainTime);
        if (autoHideUI)
        {
            PlayHideAnimation(() => UIManager.Instance.CloseAsync(this).Forget());
        }
        yield return null;
    }

    /// <summary>
    /// 开始执行后台任务，完成后自动关闭 LoadingPanel
    /// </summary>
    public void BeginTask(IEnumerator task, Action loadingCloseCallback = null, bool autoHideUI = true)
    {
        _loadingCloseCallback = loadingCloseCallback;
        _task = task;
        if (_task != null)
            StartCoroutine(WaitTask(autoHideUI));
    }
}
