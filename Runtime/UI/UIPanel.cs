using System;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public enum UIPanelKind
{
    [LabelText("全屏独占")]
    Fullscreen,
    [LabelText("叠加层")]
    Overlay,
}

/// <summary>
/// UI 面板基类。
///
/// 对外只暴露字段、属性、生命周期信号与可重写回调；所有"打开/关闭/置前"操作必须走 <see cref="UIManager"/>。
/// 实际的生命周期方法（OpenAsyncInternal / CloseAsyncInternal / SuspendAsyncInternal / ResumeAsyncInternal）
/// 标记为 <c>internal</c>，仅 UIManager 调用，避免业务侧绕过容器导致状态不一致。
/// </summary>
public class UIPanel : MonoBehaviour
{
    // ════════════════════════════════════════════════
    // Inspector 字段
    // ════════════════════════════════════════════════

    [LabelText("面板类型")]
    [Tooltip("Fullscreen=栈式独占，压栈时隐藏上一张Fullscreen；Overlay=与Fullscreen共存，多层可并列")]
    public UIPanelKind Kind = UIPanelKind.Fullscreen;

    [LabelText("父级面板")]
    [Tooltip("可选归属Panel，父Panel关闭时子Panel会被级联关闭")]
    public UIPanel ParentPanel;

    [LabelText("挂起时保留状态")]
    [Tooltip("仅 Fullscreen 生效。开启后被压栈时走 Suspend（仅 SetActive(false)，保留订阅与 BGM）；关闭走 Close（断订阅、Pop BGM）。")]
    public bool KeepAliveOnSuspend = false;

    [LabelText("返回键关闭")]
    [Tooltip("是否响应返回键关闭")]
    public bool PopWhenBack = false;

    [LabelText("启动时打开")]
    public bool OpenOnStart = false;

    [LabelText("可见")]
    public bool Visible = false;

    [LabelText("可交互")]
    public bool Interactable = true;

    [LabelText("静音游戏声音")]
    public bool MuteGameplay = false;

    [LabelText("打开音效")]
    public AudioClip OpenAudio;

    [LabelText("关闭音效")]
    public AudioClip CloseAudio;

    [LabelText("背景音乐")]
    public AudioClip BGM;

    [LabelText("打开特效")]
    public MMF_Player OpenFx;

    [LabelText("关闭特效")]
    public MMF_Player CloseFx;

    [LabelText("面板动画")]
    [Tooltip("覆盖全局动画，null 则沿用 UIManager.PanelAnimation，再 fallback 到 OpenFx/CloseFx")]
    public UIAnimation PanelAnimation;

    // ════════════════════════════════════════════════
    // 生命周期信号
    // ════════════════════════════════════════════════

    [LabelText("面板开始打开信号")]
    public KSignal OnPanelBeginOpen = new();

    [LabelText("面板打开完成信号")]
    public KSignal OnPanelOpen = new();

    [LabelText("面板开始关闭信号")]
    public KSignal OnPanelBeginClose = new();

    [LabelText("面板关闭完成信号")]
    public KSignal OnPanelClose = new();

    [LabelText("面板开始挂起信号")]
    public KSignal OnPanelBeginSuspend = new();

    [LabelText("面板挂起完成信号")]
    public KSignal OnPanelSuspend = new();

    [LabelText("面板开始恢复信号")]
    public KSignal OnPanelBeginResume = new();

    [LabelText("面板恢复完成信号")]
    public KSignal OnPanelResume = new();

    // ════════════════════════════════════════════════
    // 内部状态
    // ════════════════════════════════════════════════

    /// <summary>OpenFx/CloseFx 自动包装缓存</summary>
    private UIAnimationMMF _mmfAnimation;

    protected Subscriber subscriber = new();

    // ════════════════════════════════════════════════
    // 公开虚方法（业务可重写但不应直接调用）
    // ════════════════════════════════════════════════

    /// <summary>
    /// 全局按键透传给当前栈顶 Panel（由 UIManager 派发，当前未启用调度器）。
    /// </summary>
    public virtual void OnGlobalButtonPress(KeyCode code)
    {
    }

    // ════════════════════════════════════════════════
    // Unity 生命周期
    // ════════════════════════════════════════════════

    protected void Awake()
    {
        UIManager.Instance.AddUI(this);
    }

    public void Start()
    {
        if (OpenOnStart)
        {
            UIManager.Instance.PushAsync(this).Forget();
        }
    }

    // ════════════════════════════════════════════════
    // 内部生命周期方法（仅 UIManager 调用）
    // ════════════════════════════════════════════════

    /// <summary>
    /// 打开：完整生命周期，触发 OnOpen，播 OpenAudio、BGM 等。
    /// </summary>
    internal async UniTask OpenAsyncInternal()
    {
        Visible = true;
        Interactable = true;
        if (OpenAudio) SoundManager.Instance.PlaySound(OpenAudio);
        gameObject.SetActive(true);
        OnPanelBeginOpen?.Invoke();

        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetOrAddCanvasGroup();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            anim.OnOpenStart?.Invoke(this);
            await anim.PlayOpenAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnOpenEnd?.Invoke(this);

            cg.blocksRaycasts = true;
        }

        OnOpen();
    }

    /// <summary>
    /// 关闭：完整生命周期，触发 OnClose，断订阅、Pop BGM。
    /// </summary>
    internal async UniTask CloseAsyncInternal()
    {
        Visible = false;
        Interactable = false;
        if (CloseAudio) SoundManager.Instance.PlaySound(CloseAudio);
        OnPanelBeginClose?.Invoke();

        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetOrAddCanvasGroup();
            cg.blocksRaycasts = false;

            anim.OnCloseStart?.Invoke(this);
            await anim.PlayCloseAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnCloseEnd?.Invoke(this);
        }

        OnClose();
    }

    /// <summary>
    /// 挂起：保留订阅与 BGM，仅淡出并 SetActive(false)。
    /// 仅当 <see cref="KeepAliveOnSuspend"/> 为 true 时由 UIManager 调用。
    /// </summary>
    internal async UniTask SuspendAsyncInternal()
    {
        Visible = false;
        Interactable = false;
        OnPanelBeginSuspend?.Invoke();

        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetOrAddCanvasGroup();
            cg.blocksRaycasts = false;

            anim.OnCloseStart?.Invoke(this);
            await anim.PlayCloseAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnCloseEnd?.Invoke(this);
        }

        // 不动 subscriber、不动 BGM；仅停止 Update/协程
        gameObject.SetActive(false);

        OnSuspend();
        OnPanelSuspend?.Invoke();
    }

    /// <summary>
    /// 恢复：从挂起状态回到栈顶。
    /// </summary>
    internal async UniTask ResumeAsyncInternal()
    {
        Visible = true;
        Interactable = true;
        gameObject.SetActive(true);
        OnPanelBeginResume?.Invoke();

        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetOrAddCanvasGroup();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            anim.OnOpenStart?.Invoke(this);
            await anim.PlayOpenAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnOpenEnd?.Invoke(this);

            cg.blocksRaycasts = true;
        }

        OnResume();
        OnPanelResume?.Invoke();
    }

    // ════════════════════════════════════════════════
    // 生命周期回调（业务重写）
    // ════════════════════════════════════════════════

    /// <summary>面板完整打开后调用（动画播完）。默认行为：触发 OnPanelOpen 信号、播 BGM。</summary>
    protected virtual void OnOpen()
    {
        OnPanelOpen?.Invoke();
        if (BGM)
            SoundManager.Instance.PlayMusic(BGM);
    }

    /// <summary>面板完整关闭后调用（动画播完）。默认行为：触发 OnPanelClose、断订阅、SetActive(false)、Pop BGM。</summary>
    protected virtual void OnClose()
    {
        OnPanelClose?.Invoke();
        subscriber.DisconnectAll();
        gameObject.SetActive(false);
        if (BGM)
            SoundManager.Instance.PopTrack();
    }

    /// <summary>挂起完成后调用。默认空实现 —— 不断订阅、不动 BGM。</summary>
    protected virtual void OnSuspend()
    {
    }

    /// <summary>从挂起状态恢复完成后调用。默认空实现。</summary>
    protected virtual void OnResume()
    {
    }

    // ════════════════════════════════════════════════
    // 工具
    // ════════════════════════════════════════════════

    private UIAnimation GetEffectiveAnimation()
    {
        if (PanelAnimation != null) return PanelAnimation;
        if (UIManager.Instance?.PanelAnimation != null) return UIManager.Instance.PanelAnimation;
        if (OpenFx != null || CloseFx != null)
        {
            _mmfAnimation ??= new UIAnimationMMF(OpenFx, CloseFx);
            return _mmfAnimation;
        }
        return null;
    }

    private CanvasGroup GetOrAddCanvasGroup()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        return cg;
    }
}
