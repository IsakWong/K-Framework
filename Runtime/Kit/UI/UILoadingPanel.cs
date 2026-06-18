using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingPanel : UIPanel
{
    public float MinLoadTime = 1.0f;

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

    protected override void OnOpen()
    {
        base.OnOpen();
        PlayShowAnimation();
    }

    protected override UIAnimation GetEffectiveAnimation()
    {
        return null;
    }

    protected override void OnClose()
    {
        base.OnClose();
        _loadingCloseCallback?.Invoke();
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
        rt.DOAnchorPosX(0, ShowDuration).SetEase(ShowEase);
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
        rt.DOAnchorPosX(w, HideDuration).SetEase(HideEase)
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
