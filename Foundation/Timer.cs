using System;
using UnityEngine;

public class KTimer
{
    private float duration;

    private float elapsedTime;

    public float Duration
    {
        get => duration;
        set => duration = value;
    }

    public bool IsRunning => isRunning;

    private bool isRunning;

    public bool IsLooping
    {
        get => isLooping;
        set => isLooping = value;
    }

    public bool isLooping;

    public Action OnTimerComplete
    {
        get => onTimerComplete;
        set => onTimerComplete = value;
    }

    private Action onTimerComplete;

    public KTimer(float duration, Action onTimerComplete = null, bool isLooping = false)
    {
        this.duration = duration;
        this.onTimerComplete = onTimerComplete;
        this.isLooping = isLooping;
    }

    public void Reset()
    {
        isRunning = true;
        elapsedTime = 0.0f;
    }

    
    public void Start()
    {
        isRunning = true;
        elapsedTime = 0.0f;
    }
    
    public void Stop()
    {
        isRunning = false;
        elapsedTime = 0.0f;
    }

    public void TimerTick(float deltaTime)
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += deltaTime;

        if (elapsedTime >= duration)
        {
            onTimerComplete?.Invoke();

            if (isLooping)
            {
                elapsedTime = 0.0f;
            }
            else
            {
                isRunning = false;
                elapsedTime = 0.0f;
            }
        }
    }
}