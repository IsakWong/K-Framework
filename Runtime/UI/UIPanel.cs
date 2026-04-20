using System;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIPanel : MonoBehaviour
{
    [LabelText("全屏独占面板")]
    [Tooltip("全屏独占UI，会让上一个UI临时隐藏，全屏UI退出后恢复上一个UI")]
    public bool FullscreenPanel = true;

    [LabelText("父级面板")]
    [Tooltip("可选归属Panel，如果ParentPanel被关闭，子Panel也会被关闭")]
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
    public virtual void OnGlobalButtonPress(KeyCode code)
    {
    }

    protected void Awake()
    {
        UIManager.Instance.AddUI(this);
    }
    
    [LabelText("启动时显示")]
    public bool ShowOnStart = false;
    
    public void Start()
    {
        if (ShowOnStart)
        {
            ShowPanel();    
        }
        
    }

    public void Pop()
    {
        UIManager.Instance.PopUI();
    }
    
    protected Subscriber subscriber = new();
    
    [LabelText("静音游戏声音")]
    public bool MuteGameplay = false;
    
    [LabelText("显示音效")]
    public AudioClip ShowAudio;
    
    [LabelText("隐藏音效")]
    public AudioClip HideAudio;

    public virtual void ShowPanel(float duration = -1)
    {
        Visible = true;        
        Interactable = true;
        SoundManager.Instance.PlaySound(ShowAudio ? ShowAudio : null);
        gameObject.SetActive(true);
        OnPanelBeginShow?.Invoke();
        if (ShowFx)
        {
            ShowFx?.PlayFeedbacks();
            ShowFx?.Events?.OnComplete.AddListener(() =>
            {
                OnShow();
                ShowFx.Events.OnComplete = null;             
            });
        }
        else
        {
            OnShow();    
        }
        
    }

    [LabelText("背景音乐")]
    public AudioClip BGM;
    
    protected virtual void OnShow()
    {
        OnPanelShow?.Invoke();
        if(BGM)
            SoundManager.Instance.PlayMusic(BGM);
    }

    protected virtual void OnHide()
    {
        OnPanelHide?.Invoke();
        subscriber.DisconnectAll();
        gameObject.SetActive(false);
        if(BGM)
            SoundManager.Instance.PopTrack();
    }

    protected void OnPanelDestroy()
    {
        return;
    }
    
    public virtual void HidePanel(bool destroyAfterHide = false)
    {
        Visible = false;
        Interactable = false;
        SoundManager.Instance.PlaySound(HideAudio ? HideAudio : null);
        OnPanelBeginHide?.Invoke();
        if (HideFx)
        {
            HideFx?.PlayFeedbacks();
            HideFx?.Events?.OnComplete.AddListener(() =>
            {
                OnHide();
                HideFx.Events.OnComplete = null;             
            });    
        }
        else
        {
            OnHide();    
        }
        
        
    }
}