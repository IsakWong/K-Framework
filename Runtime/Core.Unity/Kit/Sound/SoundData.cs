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
    // 音量
    public float volume = 1f;
    // 音调
    public float pitch = 1f;
    // 左右声道平衡 
    public float panStereo;
    // 空间混响
    public float spatialBlend = 1f;
    // 混响区混合比例
    public float reverbZoneMix = 1f;
    // 多普勒效应强度
    public float dopplerLevel = 0.0f;
    public float spread;

    public float minDistance = 4f;
    public float maxDistance = 30f;

    public bool ignoreListenerVolume;
    public bool ignoreListenerPause;

    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
}