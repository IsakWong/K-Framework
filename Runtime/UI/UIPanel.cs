using System;
using Cysharp.Threading.Tasks;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public enum UIPanelKind
{
    [LabelText("全屏独占")]
    Fullscreen,
    [LabelText("叠加层")]
    Overlay,
}

public class UIPanel : MonoBehaviour
{
    [LabelText("面板类型")]
    [Tooltip("Fullscreen=栈式独占，压栈时隐藏上一张Fullscreen；Overlay=与Fullscreen共存，多层可并列")]
    public UIPanelKind Kind = UIPanelKind.Fullscreen;

    [HideInInspector]
    [Tooltip("旧字段，仅用于预制体迁移；请改用 Kind")]
    public bool FullscreenPanel = true;

    [LabelText("父级面板")]
    [Tooltip("可选归属Panel，父Panel关闭时子Panel会被级联关闭")]
    public UIPanel ParentPanel;

    [LabelText("返回键关闭")]
    [Tooltip("是否响应返回键关闭")]
    public bool PopWhenBack = false;

    [LabelText("可见")]
    public bool Visible = false;

    [LabelText("可交互")]
    public bool Interactable = true;

    [LabelText("面板开始显示信号")]
    public KSignal OnPanelBeginShow = new();

    [LabelText("面板开始隐藏信号")]
    public KSignal OnPanelBeginHide = new();

    [LabelText("面板显示完成信号")]
    public KSignal OnPanelShow = new();

    [LabelText("面板隐藏完成信号")]
    public KSignal OnPanelHide = new();

    [LabelText("显示特效")]
    public MMF_Player ShowFx;

    [LabelText("隐藏特效")]
    public MMF_Player HideFx;

    [LabelText("面板动画")]
    [Tooltip("覆盖全局动画，null 则沿用 UIManager.PanelAnimation，再 fallback 到 ShowFx/HideFx")]
    public UIAnimation PanelAnimation;

    // ShowFx/HideFx 自动包装缓存
    private UIAnimationMMF _mmfAnimation;

    private UIAnimation GetEffectiveAnimation()
    {
        if (PanelAnimation != null) return PanelAnimation;
        if (UIManager.Instance?.PanelAnimation != null) return UIManager.Instance.PanelAnimation;
        if (ShowFx != null || HideFx != null)
        {
            _mmfAnimation ??= new UIAnimationMMF(ShowFx, HideFx);
            return _mmfAnimation;
        }
        return null;
    }

    [LabelText("启动时显示")]
    public bool ShowOnStart = false;

    [LabelText("静音游戏声音")]
    public bool MuteGameplay = false;

    [LabelText("显示音效")]
    public AudioClip ShowAudio;

    [LabelText("隐藏音效")]
    public AudioClip HideAudio;

    [LabelText("背景音乐")]
    public AudioClip BGM;

    protected Subscriber subscriber = new();

    public virtual void OnGlobalButtonPress(KeyCode code)
    {
    }

    protected void Awake()
    {
        UIManager.Instance.AddUI(this);
    }

    public void Start()
    {
        if (ShowOnStart)
        {
            ShowPanelAsync().Forget();
        }
    }

    private void OnValidate()
    {
        // 旧预制体迁移：FullscreenPanel bool 同步到 Kind 枚举
        Kind = FullscreenPanel ? UIPanelKind.Fullscreen : UIPanelKind.Overlay;
    }

    public void Pop()
    {
        UIManager.Instance.PopUIAsync(this).Forget();
    }

    public virtual async UniTask ShowPanelAsync()
    {
        Visible = true;
        Interactable = true;
        if (ShowAudio) SoundManager.Instance.PlaySound(ShowAudio);
        gameObject.SetActive(true);
        OnPanelBeginShow?.Invoke();

        // 优先级：Panel 自身 > UIManager 全局 > ShowFx/HideFx 自动包装
        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            anim.OnShowStart?.Invoke(this);
            await anim.PlayShowAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnShowEnd?.Invoke(this);

            cg.blocksRaycasts = true;
        }

        OnShow();
    }

    public virtual async UniTask HidePanelAsync()
    {
        Visible = false;
        Interactable = false;
        if (HideAudio) SoundManager.Instance.PlaySound(HideAudio);
        OnPanelBeginHide?.Invoke();

        // 优先级：Panel 自身 > UIManager 全局 > ShowFx/HideFx 自动包装
        var anim = GetEffectiveAnimation();
        if (anim != null)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            anim.OnHideStart?.Invoke(this);
            await anim.PlayHideAsync(cg, this.GetCancellationTokenOnDestroy());
            anim.OnHideEnd?.Invoke(this);
        }

        OnHide();
    }

    protected virtual void OnShow()
    {
        OnPanelShow?.Invoke();
        if (BGM)
            SoundManager.Instance.PlayMusic(BGM);
    }

    protected virtual void OnHide()
    {
        OnPanelHide?.Invoke();
        subscriber.DisconnectAll();
        gameObject.SetActive(false);
        if (BGM)
            SoundManager.Instance.PopTrack();
    }
}
