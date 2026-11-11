using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KFramework
{
public class UILoadingPanel : UIPanel
{
    protected new void Awake()
    {
        base.Awake();
        // Service：全局覆盖层，永远在 Service 层最上，不参与 Window 栈，CloseAll 不会清掉它
        Layer = UILayer.Service;
    }

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

    [Header("过场字幕（可选，未绑定则 PlayCaptionAsync 直接返回 false）")]
    [Tooltip("字幕根节点（CanvasGroup），渐黑盖屏期间显示，层级天然在遮罩之上")]
    public CanvasGroup CaptionGroup;
    [Tooltip("字幕正文，逐字显现")]
    public TMP_Text CaptionText;
    [Tooltip("逐字 8bit 音效")]
    public AudioClip CaptionBlipSfx;
    [Tooltip("逐字音效分类：决定混音路由与限流")]
    public SoundCategory CaptionBlipCategory;
    [Range(0f, 1f)] public float CaptionBlipVolume = 0.5f;
    [Tooltip("音高随机幅度，避免每字音高一致显得机械")]
    public float CaptionBlipPitchRange = 0.06f;

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
    private Tween _captionTween;
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
        StopCaption();
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

    /// <summary>
    /// <see cref="WaitShowComplete"/> 的 UniTask 版，供 async 调用方直接 await。
    /// 等待入场滑入动画播完（已播完或被中断则立即通过）。
    /// </summary>
    public async UniTask WaitShowCompleteAsync()
    {
        while (_showTween != null && _showTween.IsActive() && !_showTween.IsComplete())
            await UniTask.Yield(PlayerLoopTiming.Update);
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
        // 上一轮渐亮退场可能把 alpha 淡到 0，重新打开时恢复不透明，保证滑入可见
        LoadingMask.color = WithAlpha(LoadingMask.color, 1f);
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

    /// <summary>
    /// 滑出色块并等待退场动画完成后关闭面板（幂等：重复调用只会关一次）。
    /// 对应 SceneLoadGate.RevealAsync 的「关」动作。
    /// </summary>
    public async UniTask RevealAsync()
    {
        var tcs = new UniTaskCompletionSource();
        PlayHideAnimation(() => tcs.TrySetResult());
        await tcs.Task;

        var ui = UIService.Instance;
        if (ui != null && ui.IsOpen(this))
            await ui.CloseAsync(this);
    }

    /// <summary>
    /// 渐黑盖屏：取消滑入、立刻铺满全屏并隐藏内容，在 duration 内把遮罩淡到全黑。
    /// 对应 SceneLoadGate.CoverFadeAsync，用于传送等不播滑入的快速切换。
    /// </summary>
    public async UniTask CoverFadeAsync(float duration)
    {
        if (LoadingMask == null)
        {
            Debug.LogWarning("[UILoadingPanel] CoverFadeAsync: LoadingMask 为空");
            return;
        }

        _showTween?.Kill();
        _hideTween?.Kill();
        _contentTween?.Kill();
        KillDots();

        if (ContentGroup != null)
            ContentGroup.alpha = 0f;

        LoadingMask.rectTransform.anchoredPosition = Vector2.zero;
        LoadingMask.color = WithAlpha(LoadingMask.color, 0f);

        if (duration <= 0f)
        {
            LoadingMask.color = WithAlpha(LoadingMask.color, 1f);
            return;
        }

        await WaitTweenAsync(LoadingMask.DOFade(1f, duration).SetEase(Ease.Linear));
    }

    /// <summary>
    /// 渐亮撤屏：在 duration 内把遮罩淡到透明。不关闭面板，关闭由调用方负责。
    /// 对应 SceneLoadGate.RevealFadeAsync；下次打开时 PlayShowAnimation 会恢复遮罩不透明度。
    /// </summary>
    public async UniTask RevealFadeAsync(float duration)
    {
        if (LoadingMask == null)
            return;

        if (duration <= 0f)
        {
            LoadingMask.color = WithAlpha(LoadingMask.color, 0f);
            return;
        }

        await WaitTweenAsync(LoadingMask.DOFade(0f, duration).SetEase(Ease.Linear));
    }

    // 过场字幕的淡入/淡出时长。逐字间隔与停留由调用方按文案节奏传入。
    private const float CaptionFadeInDuration = 0.15f;
    private const float CaptionFadeOutDuration = 0.25f;

    /// <summary>
    /// 在遮罩上逐字打出过场字幕：每露出一个非空白字符响一声 8bit，打完停留 hold 秒后淡出。
    /// 面板或字幕节点未绑定时返回 false，由调用方兜底；ct 取消时中断并抛 <see cref="OperationCanceledException"/>。
    /// 播放中再次调用会打断上一条，上一条的 await 立即返回 false。
    /// </summary>
    public async UniTask<bool> PlayCaptionAsync(string caption, float charInterval, float hold, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(caption) || CaptionText == null || CaptionGroup == null)
            return false;

        ct.ThrowIfCancellationRequested();

        // 打断上一条：Kill 会放行它的 await，并按 false 收尾
        StopCaption();

        CaptionGroup.gameObject.SetActive(true);
        CaptionGroup.alpha = 0f;
        CaptionText.text = caption;
        CaptionText.maxVisibleCharacters = 0;
        CaptionText.ForceMeshUpdate();

        int total = CaptionText.textInfo.characterCount;
        float interval = Mathf.Max(0.01f, charInterval);

        var sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(CaptionGroup.DOFade(1f, CaptionFadeInDuration).SetEase(Ease.OutQuad));
        if (total > 0)
            sequence.Append(CreateCaptionTypewriterTween(total, interval));
        sequence.AppendInterval(Mathf.Max(0f, hold));
        sequence.Append(CaptionGroup.DOFade(0f, CaptionFadeOutDuration).SetEase(Ease.InQuad));

        var completion = new UniTaskCompletionSource();
        sequence.OnComplete(() => completion.TrySetResult());
        sequence.OnKill(() => completion.TrySetResult());
        _captionTween = sequence;

        using (ct.Register(StopCaption))
        {
            await completion.Task;
        }

        ct.ThrowIfCancellationRequested();

        // 仍指向本次序列才说明是正常播完；被打断时 StopCaption 已经收尾
        bool completed = _captionTween == sequence;
        if (completed)
        {
            _captionTween = null;
            HideCaption();
        }

        return completed;
    }

    /// <summary>字幕按字推进，每露出一个非空白字符响一声 8bit。</summary>
    private Tween CreateCaptionTypewriterTween(int total, float interval)
    {
        int shown = 0;
        return DOTween.To(() => 0f, progress =>
            {
                int visible = Mathf.Clamp(Mathf.CeilToInt(progress * total), 0, total);
                if (visible <= shown)
                    return;

                bool speak = false;
                for (int i = shown; i < visible; i++)
                {
                    // 不能读 characterInfo.isVisible：它受 maxVisibleCharacters 影响，
                    // 此处刚改过、textInfo 还没重算，新露出的字符 isVisible 仍为 false，
                    // 会导致逐字音全部哑掉（文字照常显示）。
                    char c = CaptionText.textInfo.characterInfo[i].character;
                    if (!char.IsWhiteSpace(c) && !char.IsControl(c))
                    {
                        speak = true;
                        break;
                    }
                }

                shown = visible;
                CaptionText.maxVisibleCharacters = visible;
                if (speak)
                    PlayCaptionBlip();
            }, 1f, total * interval)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => CaptionText.maxVisibleCharacters = int.MaxValue);
    }

    /// <summary>每字一声 8bit 方波，音高在 <see cref="CaptionBlipPitchRange"/> 内随机。</summary>
    private void PlayCaptionBlip()
    {
        if (CaptionBlipSfx == null || SoundService.Instance == null)
            return;

        var emitter = SoundService.Instance.PlaySound(CaptionBlipSfx, CaptionBlipVolume, CaptionBlipCategory);
        emitter?.WithRandomPitch(CaptionBlipPitchRange);
    }

    /// <summary>打断当前字幕并立即收尾（关闭面板、被新字幕顶替、取消时调用）。</summary>
    private void StopCaption()
    {
        var tween = _captionTween;
        _captionTween = null;
        tween?.Kill();
        HideCaption();
    }

    private void HideCaption()
    {
        if (CaptionGroup != null)
        {
            CaptionGroup.alpha = 0f;
            CaptionGroup.gameObject.SetActive(false);
        }

        if (CaptionText != null)
            CaptionText.maxVisibleCharacters = int.MaxValue;
    }

    /// <summary>
    /// 等待 tween 结束（完成或被 Kill 都放行）。
    /// 不依赖 UniTask.DOTween 扩展——其 <c>ToUniTask</c> 受 UNITASK_DOTWEEN_SUPPORT 宏控制，
    /// 本项目 DOTween 是预编译插件而非 UPM 包，宏不会定义。
    /// </summary>
    private static async UniTask WaitTweenAsync(Tween tween)
    {
        if (tween == null || !tween.IsActive())
            return;

        var tcs = new UniTaskCompletionSource();
        tween.OnComplete(() => tcs.TrySetResult());
        tween.OnKill(() => tcs.TrySetResult());
        await tcs.Task;
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
}
