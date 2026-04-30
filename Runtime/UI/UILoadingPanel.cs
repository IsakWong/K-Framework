using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UILoadingPanel : UIPanel
{
    public float MinLoadTime = 1.0f;

    public Image LoadingMask;
    public Animator Animator;
    public Text LoadingText;

    public void SetLoadingTip(string text)
    {
        if (LoadingText)
            LoadingText.text = text;
    }

    protected override void OnClose()
    {
        base.OnClose();
        _loadingCloseCallback?.Invoke();
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
            UIManager.Instance.CloseAsync(this).Forget();
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
