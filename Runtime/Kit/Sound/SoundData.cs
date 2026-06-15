using System;
using UnityEngine;

/// <summary>
/// AudioSource 参数模板（纯数据，无身份）
/// 用于初始化 SoundEmitter 的 AudioSource 属性。
/// 不含 clip（调用时传入）、不含 mixerGroup（由 SoundCategory 提供）、不含并发策略。
/// </summary>
[Serializable]
public class SoundData
{
    public bool loop;
    public bool playOnAwake = true;

    [Header("AudioSource Settings")]
    public bool mute;
    public bool bypassEffects;
    public bool bypassListenerEffects;
    public bool bypassReverbZones;

    public int priority = 128;
    public float volume = 1f;
    public float pitch = 1f;
    public float panStereo;
    public float spatialBlend = 1f;
    public float reverbZoneMix = 1f;
    public float dopplerLevel = 0.0f;
    public float spread;

    public float minDistance = 4f;
    public float maxDistance = 30f;

    public bool ignoreListenerVolume;
    public bool ignoreListenerPause;

    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
}