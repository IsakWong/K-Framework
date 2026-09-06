using System;
using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using UnityEngine;


public class KTime : KSingleton<KTime>, ITimeMonitor
{
    public static float scaleDeltaTime = 0.0f;
    public float DeltaTime => scaleDeltaTime;
}

public class KTimer
{
    /// <summary>
    /// 定时器间隔
    /// </summary>
    public float Interval
    {
        get => _interval;
        set => _interval = value;
    }

    private float _interval;
    private float _elapsedTime;

    /// <summary>
    /// 当前已经过的时间
    /// </summary>
    public float ElapsedTime => _elapsedTime;

    /// <summary>
    /// 距离下次触发的剩余时间
    /// </summary>
    public float RemainingTime => Mathf.Max(0f, _interval - _elapsedTime);

    /// <summary>
    /// 当前周期的进度 (0~1)
    /// </summary>
    public float Progress => _interval > 0f ? Mathf.Clamp01(_elapsedTime / _interval) : 1f;

    public bool IsLooping => _loops < Loops || _loops < 0;

    /// <summary>
    /// 循环次数，-1为无限循环，1为循环1次
    /// </summary>
    public int Loops = 1;

    private int _loops = 0;

    /// <summary>
    /// 是否在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    private bool _isRunning;

    /// <summary>
    /// 是否结束
    /// </summary>
    public bool IsFinished => _isFinished;

    private bool _isFinished = false;

    /// <summary>
    /// 是否移除（下一帧）
    /// </summary>
    public bool IsRemoving => _isRemoving;

    private bool _isRemoving = false;


    public Action OnTimeout;
    public Action OnFinish;

    public KTimer(float interval, Action onTimeout = null, int loops = 1)
    {
        _interval = interval;
        OnTimeout = onTimeout;
        Loops = loops;
    }

    public void Reset()
    {
        _isRunning = true;
        _isFinished = false;
        _elapsedTime = 0.0f;
    }

    public KTimer Start()
    {
        _isRunning = true;
        _isFinished = false;
        _loops = 0;
        _elapsedTime = 0.0f;
        return this;
    }

    public void Pause()
    {
        _isRunning = false;
    }

    public void Stop()
    {
        _isRunning = false;
        _elapsedTime = 0.0f;
        _isRemoving = true;
    }

    public void OnLogic(float deltaTime)
    {
        if (!_isRunning)
        {
            return;
        }

        _elapsedTime += deltaTime;
        if (_elapsedTime >= _interval)
        {
            _loops++;
            OnTimeout?.Invoke();
            if (_loops < Loops || Loops <= 0)
            {
                _elapsedTime = 0.0f;
            }
            else
            {
                _isRunning = false;
                _isFinished = true;
                OnFinish?.Invoke();
                _elapsedTime = 0.0f;
            }
        }
    }
}

/// <summary>
/// Timer的管理器，要调用Logic
/// </summary>
public class KTimerManager
{
    public KTimer AddTimer(float duration, Action onTimerComplete = null, int loops = 1)
    {
        var timer = new KTimer(duration, onTimerComplete, loops);
        timer.Start();
        _toAdd.Add(timer);
        return timer;
    }

    public void StopAllTimer()
    {
        _internalTimers.ForEach((timer) =>
        {
            timer.Stop();
            _toRemove.Add(timer);
        });
    }

    public void RemoveTimer(KTimer timer)
    {
        _toRemove.Add(timer);
    }

    protected List<KTimer> _internalTimers = new();
    private List<KTimer> _toRemove = new();
    private List<KTimer> _toAdd = new();


    public void OnLogic(float logic)
    {
        // 合并新增和待移除
        if (_toAdd.Count > 0)
        {
            _internalTimers.AddRange(_toAdd);
            _toAdd.Clear();
        }
        if (_toRemove.Count > 0)
        {
            foreach (var timer in _toRemove)
                _internalTimers.Remove(timer);
            _toRemove.Clear();
        }
        
        var logicTime = logic;
        for (int i = _internalTimers.Count - 1; i >= 0; i--)
        {
            var timer = _internalTimers[i];
            timer.OnLogic(logicTime);
            if (timer.IsRemoving || (timer.IsLooping && timer.IsFinished))
            {
                _internalTimers.RemoveAt(i);
            }
        }
    }
}