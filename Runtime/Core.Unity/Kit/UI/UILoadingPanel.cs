using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingPanel : UIPanel
{
    public float MinLoadTime = 0.5f;

    [Header("绑定")]
    public Image LoadingMask;
    [Tooltip("内容容器。绑定后入场时先隐藏，遮罩铺满后再淡入；退场时先淡出")]
    public CanvasGroup ContentGroup;
    [Tooltip("加载提示文本，SetLoadingTip / SetStageTips 写入此处")]
    public TMP_Text LoadingText;

    [Header("加载进度（可选，未绑定对应控件则自动隐藏该部分）")]
    [Tooltip("进度条填充（Image Type=Filled），SetProgress 驱动其 fillAmount")]
    public Image LoadingFill;
    [Tooltip("进度百分比文本（如 \"45%\"），SetProgress 自动更新")]
    public TMP_Text ProgressText;

    [Header("加载符号（可选）")]
    [Tooltip("呼吸点图标，依次错开做 alpha 呼吸")]
    public Graphic[] LoadingDots;

    [Header("色块动画")]
    public float ShowDuration = 0.35f;
    public float HideDuration = 0.3f;
    public Ease ShowEase = Ease.OutCubic;
    public Ease HideEase = Ease.InCubic;

    [Header("内容与进度")]
    [Tooltip("内容淡入时长")]
    public float ContentFadeInDuration = 0.2f;
    [Tooltip("内容淡出时长")]
    public float ContentFadeOutDuration = 0.15f;
    [Tooltip("显示进度追赶目标值所需的时长（秒）；0 = 直接跳变")]
    public float ProgressSmoothDuration = 0.2f;
    [Tooltip("呼吸点最暗透明度")]
    [Range(0f, 1f)] public float DotMinAlpha = 0.25f;
    [Tooltip("呼吸点单次明暗时长")]
    public float DotFadeDuration = 0.45f;
    [Tooltip("相邻呼吸点的启动间隔")]
    public float DotStagger = 0.15f;

    public void SetLoadingTip(string text)
    {
        if (LoadingText)
            LoadingText.text = text;
    }

    /// <summary>
    /// 设置分阶段提示词数组。之后每次 SetProgress 推进时，
    /// 提示文字按进度均分段落自动切换到对应阶段文案（最后一段由 100% 触发）。
    /// 传 null / 空数组则退回纯进度模式（不改提示文字）。
    /// </summary>
    public void SetStageTips(string[] tips)
    {
        _stageTips = tips;
        _shownStage = -1;
        if (_stageTips is { Length: > 0 } && LoadingText != null)
            LoadingText.text = _stageTips[0];
    }

    /// <summary>
    /// 更新加载进度。归一化 0~1：显示值平滑追赶目标值，
    /// 驱动进度条填充与百分比文本；设置了阶段提示词时随进度自动切换文案。
    /// 未绑定 LoadingFill / ProgressText 时自动忽略对应部分。
    /// 供下载 / 预载 / 后台任务等任意带进度流程复用——调用方只需换算成 0~1。
    /// </summary>
    public void SetProgress(float progress)
    {
        _targetProgress = Mathf.Clamp01(progress);
        if (ProgressSmoothDuration <= 0.001f)
        {
            _displayProgress = _targetProgress;
            ApplyProgressVisual();
        }
    }

    /// <summary>
    /// 同时设置加载提示文字与进度（便捷组合，可复用）。
    /// </summary>
    public void SetLoading(string tip, float progress)
    {
        SetLoadingTip(tip);
        SetProgress(progress);
    }

    /// <summary>分阶段提示词数组（由 SetStageTips 注入，SetProgress 按进度段切换文案）</summary>
    private string[] _stageTips;

    private float _openedAt = float.MinValue;
    private float _displayProgress;
    private float _targetProgress;
    private int _shownPercent = -1;
    private int _shownStage = -1;

    // 入场/退出动画的 tween 引用。hide 前先结束 show，避免两者争抢同一属性导致退出动画异常
    private Tween _showTween;
    private Tween _hideTween;
    private Tween _contentTween;
    private Tween[] _dotTweens;

    /// <summary>
    /// 自本次打开起已展示的时长（秒）。供关闭方据此保证最短展示时间。
    /// </summary>
    public float ShownDuration => _openedAt < 0f ? 0f : Time.time - _openedAt;

    protected override void OnOpen()
    {
        base.OnOpen();
        _openedAt = Time.time;
        ResetProgressVisual();
        PlayShowAnimation();
    }

    protected override UIAnimation GetEffectiveAnimation()
    {
        return null;
    }

    protected override void OnClose()
    {
        _openedAt = float.MinValue;
        _contentTween?.Kill();
        KillDots();
        base.OnClose();
        _loadingCloseCallback?.Invoke();
    }

    private void Update()
    {
        if (Mathf.Approximately(_displayProgress, _targetProgress))
            return;
        float speed = ProgressSmoothDuration <= 0.001f
            ? 1f
            : Time.unscaledDeltaTime / ProgressSmoothDuration;
        _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress, speed);
        ApplyProgressVisual();
    }

    private void ResetProgressVisual()
    {
        _displayProgress = 0f;
        _targetProgress = 0f;
        _shownPercent = -1;
        _shownStage = -1;
        ApplyProgressVisual();
    }

    /// <summary>把当前显示进度同步到进度条、百分比与阶段文案（仅在数值真的变化时刷新文本）</summary>
    private void ApplyProgressVisual()
    {
        if (LoadingFill != null)
            LoadingFill.fillAmount = _displayProgress;

        if (ProgressText != null)
        {
            int percent = Mathf.RoundToInt(_displayProgress * 100f);
            if (percent != _shownPercent)
            {
                _shownPercent = percent;
                ProgressText.text = percent + "%";
            }
        }

        if (_stageTips is { Length: > 0 } && LoadingText != null)
        {
            int idx = Mathf.Min(_stageTips.Length - 1, Mathf.FloorToInt(_displayProgress * _stageTips.Length));
            if (idx != _shownStage)
            {
                _shownStage = idx;
                LoadingText.text = _stageTips[idx];
            }
        }
    }

    /// <summary>
    /// 等待入场动画完整播完（已播完或被中断则立即通过）。
    /// 供调用方在播放退出动画前调用，保证遮罩先盖满屏幕再滑出。
    /// </summary>
    public IEnumerator WaitShowComplete()
    {
        // 只等遮罩滑入：内容淡入属于装饰，不应拖延调用方的场景加载
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

        if (ContentGroup != null)
        {
            ContentGroup.alpha = 0f;
            _contentTween?.Kill();
            // 遮罩快要铺满时才浮出内容，避免文字跟着色块一起被拖进来
            _contentTween = ContentGroup.DOFade(1f, ContentFadeInDuration)
                .SetDelay(Mathf.Max(0f, ShowDuration - 0.1f))
                .SetEase(Ease.OutQuad);
        }

        PlayDots();
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
        KillDots();

        if (ContentGroup != null)
        {
            _contentTween?.Kill();
            _contentTween = ContentGroup.DOFade(0f, ContentFadeOutDuration).SetEase(Ease.InQuad);
        }

        _hideTween = rt.DOAnchorPosX(w, HideDuration).SetEase(HideEase)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void PlayDots()
    {
        KillDots();
        if (LoadingDots == null || LoadingDots.Length == 0)
            return;

        _dotTweens = new Tween[LoadingDots.Length];
        for (int i = 0; i < LoadingDots.Length; i++)
        {
            Graphic dot = LoadingDots[i];
            if (dot == null)
                continue;
            dot.color = WithAlpha(dot.color, 1f);
            _dotTweens[i] = dot.DOFade(DotMinAlpha, DotFadeDuration)
                .SetDelay(i * DotStagger)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void KillDots()
    {
        if (_dotTweens == null)
            return;
        for (int i = 0; i < _dotTweens.Length; i++)
            _dotTweens[i]?.Kill();
        _dotTweens = null;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
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
            PlayHideAnimation(() => UIService.Instance.CloseAsync(this).Forget());
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
