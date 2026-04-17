using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UILoadingPanel : UIPanel
{
 
    public float MinLoadTime = 1.0f;

    public Image LoadingMask;
    public Animator Animator;
    public Text LoadingText;
    private void Start()
    {
    }

    public void SetHealth(float value)
    {
        // Update UI
    }

    public void SwitchMask()
    {
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.5f);
        seq.Play();
    }

    public override void HidePanel(bool destroyAfterHide = false)
    {
        base.HidePanel(destroyAfterHide);
        var rect = transform as RectTransform;
    }

    public void SetLoadingTip(string text)
    {
        if(LoadingText)
            LoadingText.text = text;
    }
    public override void ShowPanel(float duration = -1)
    {
        base.ShowPanel(duration);
        ShowFx?.PlayFeedbacks();
    }

    protected override void OnHide()
    {
        base.OnHide();
        LoadingCloseCallback?.Invoke();
    }

    private IEnumerator _task;
    

    private IEnumerator WaitTask(bool autoHideUI)
    {
        float startTime = Time.time;
        yield return new WaitForSeconds(0.3f);
        yield return _task;
        float remainTime = MinLoadTime - (Time.time - startTime);
        if(remainTime > 0)
            yield return new WaitForSeconds(remainTime);
        if (autoHideUI)
        {
            UIManager.Instance.PopUI(this);
        }
        yield return null;
    }

    private Action LoadingCloseCallback;
    
    /// <summary>
    /// 开始执行任务（通用接口）
    /// </summary>
    public void BeginTask(IEnumerator task, Action loadingCloseCallback=null)
    {
        LoadingCloseCallback = loadingCloseCallback;
        _task = task;
        if(_task != null)
            StartCoroutine("WaitTask");
    }
    
    /// <summary>
    /// 开始加载场景
    /// </summary>
    [Obsolete("Use SceneManager.Instance.LoadSceneWithLoading() instead")]
    public void BeginLoadScene(AssetReference NextLevel, bool autoHideLoading = true, ScenePersistentData persistentData = null, Action onLoadComplete = null)
    {
        LoadingCloseCallback = onLoadComplete;
        _task = SceneManager.Instance.LoadSceneCoroutine(NextLevel, persistentData);
        StartCoroutine(WaitTask(autoHideLoading));
    }


}