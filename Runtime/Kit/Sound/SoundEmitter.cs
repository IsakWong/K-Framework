using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class SoundEmitter : MonoBehaviour, IPoolable
{
    public SoundData Data { get; private set; }
    public SoundCategory Category { get; set; }
    public LinkedListNode<SoundEmitter> Node { get; set; }

    /// <summary>
    /// 当此 Emitter 播放完毕或被 Stop 时，通知管理器回收
    /// </summary>
    public Action<SoundEmitter> OnFinished { get; set; }

    public AudioSource audioSource;
    private Coroutine playingCoroutine;

    private void Awake()
    {
        audioSource = gameObject.GetOrAdd<AudioSource>();
    }

    // ─── IPoolable ───

    public void OnGetFromPool()
    {
        stopped = false;
        playingCoroutine = null;
    }

    public void OnReturnToPool()
    {
        if (playingCoroutine != null)
        {
            StopCoroutine(playingCoroutine);
            playingCoroutine = null;
        }
        audioSource.Stop();
        audioSource.clip = null;
        Category = null;
        Node = null;
        OnFinished = null;
        Data = null;
    }

    /// <summary>
    /// 用 SoundData 模板初始化 AudioSource 参数（不含 clip 和 mixerGroup）
    /// </summary>
    public void Initialize(SoundData data)
    {
        stopped = false;
        Data = data;

        audioSource.loop = data.loop;
        audioSource.playOnAwake = data.playOnAwake;

        audioSource.mute = data.mute;
        audioSource.bypassEffects = data.bypassEffects;
        audioSource.bypassListenerEffects = data.bypassListenerEffects;
        audioSource.bypassReverbZones = data.bypassReverbZones;

        audioSource.priority = data.priority;
        audioSource.volume = data.volume;
        audioSource.pitch = data.pitch;
        audioSource.panStereo = data.panStereo;
        audioSource.spatialBlend = data.spatialBlend;
        audioSource.reverbZoneMix = data.reverbZoneMix;
        audioSource.dopplerLevel = data.dopplerLevel;
        audioSource.spread = data.spread;

        audioSource.minDistance = data.minDistance;
        audioSource.maxDistance = data.maxDistance;

        audioSource.ignoreListenerVolume = data.ignoreListenerVolume;
        audioSource.ignoreListenerPause = data.ignoreListenerPause;

        audioSource.rolloffMode = data.rolloffMode;
    }

    public void Play()
    {
        if (playingCoroutine != null)
        {
            StopCoroutine(playingCoroutine);
        }

        audioSource.Play();
        if (!audioSource.loop)
        {
            playingCoroutine = StartCoroutine(WaitForSoundToEnd());
        }
    }

    private IEnumerator WaitForSoundToEnd()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        Stop();
    }

    private bool stopped;

    public void Stop(float duration = 0.2f)
    {
        if (stopped) return;

        if (playingCoroutine != null)
        {
            StopCoroutine(playingCoroutine);
            playingCoroutine = null;
        }

        stopped = true;
        audioSource.DOFade(0, duration).OnComplete(() =>
        {
            audioSource.Stop();
            OnFinished?.Invoke(this);
        });
    }

    public void WithRandomPitch(float range)
    {
        if (range > 0f)
            audioSource.pitch += Random.Range(-range, range);
    }
}