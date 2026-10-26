using UnityEngine;
using UnityEngine.Audio;

public class SoundBuilder
{
    private readonly SoundService soundManager;
    private Vector3 position = Vector3.zero;
    private bool use3D;
    private float spatialOverride = -1f;

    public SoundBuilder(SoundService soundManager)
    {
        this.soundManager = soundManager;
    }

    public SoundBuilder With3DPosition(Vector3 position)
    {
        this.position = position;
        use3D = true;
        spatialOverride = 0.5f;
        return this;
    }

    public SoundBuilder WithSpatialBlend(float blend)
    {
        spatialOverride = blend;
        return this;
    }

    private AudioMixerGroup overrideGroup;

    public SoundBuilder OverrideMixerGroup(AudioMixerGroup group)
    {
        overrideGroup = group;
        return this;
    }

    private float overridePitch = -1;

    public SoundBuilder WithPitch(float pitch)
    {
        overridePitch = pitch;
        return this;
    }

    private float overrideVolume = -1;

    public SoundBuilder OverrideVolume(float volume)
    {
        overrideVolume = volume;
        return this;
    }

    private AudioClip overrideClip;

    public SoundBuilder OverrideClip(AudioClip clip)
    {
        overrideClip = clip;
        return this;
    }

    private string throttleKey;
    private float throttleCooldown;

    /// <summary>
    /// 同名逻辑音效的最小间隔（秒）：窗口内同 key 只放行一次，被挡下的请求直接丢弃。
    /// 多条目随机变体（受击、打断）共用同一个 key，避免一次事件叠成爆音。
    /// </summary>
    public SoundBuilder Throttle(string key, float cooldown)
    {
        throttleKey = key;
        throttleCooldown = cooldown;
        return this;
    }

    /// <summary>
    /// 播放音效。category 提供 Mixer 路由 + 并发控制 + AudioSource 默认参数。
    /// 为 null 时使用 SoundService.DefaultSoundData 且不限流。
    /// </summary>
    public SoundEmitter Play(SoundCategory category = null)
    {
        // 先过分类限流（纯检查），再消耗 throttle 窗口，避免被分类挡下的请求白占冷却。
        if (!soundManager.CanPlaySound(category))
            return null;

        if (!soundManager.TryPassThrottle(throttleKey, throttleCooldown))
            return null;

        // Resolve AudioSource parameters: category.defaults → fallback to DefaultSoundData
        SoundData audioParams = category != null ? category.defaults : soundManager.DefaultSoundData;

        var soundEmitter = soundManager.Get();
        if (soundEmitter == null) return null;

        soundEmitter.Initialize(audioParams);
        soundEmitter.Category = category;

        // Clip (must be provided via OverrideClip or the emitter has no clip)
        if (overrideClip != null)
            soundEmitter.audioSource.clip = overrideClip;

        // Mixer group: override > category > none
        var mixerGroup = overrideGroup ?? category?.mixerGroup;
        if (mixerGroup != null)
            soundEmitter.audioSource.outputAudioMixerGroup = mixerGroup;

        // Volume: base → override → concurrent decay
        float baseVol = overrideVolume >= 0 ? overrideVolume : audioParams.volume;
        soundEmitter.audioSource.volume = soundManager.GetEffectiveVolume(category, baseVol) * soundManager.SFXVolume;

        // Spatial
        if (spatialOverride >= 0f)
            soundEmitter.audioSource.spatialBlend = spatialOverride;

        if (use3D)
            soundEmitter.transform.position = position;

        soundEmitter.transform.parent = soundManager.transform;

        // Random pitch from category config
        if (category != null && category.randomPitchRange > 0f)
            soundEmitter.WithRandomPitch(category.randomPitchRange);

        // Explicit pitch override (takes precedence over default + random)
        if (overridePitch >= 0f)
            soundEmitter.audioSource.pitch = overridePitch;

        // Frequent sound tracking
        if (category != null && category.frequentSound)
            soundEmitter.Node = soundManager.FrequentSoundEmitters.AddLast(soundEmitter);

        // Track concurrency
        soundManager.TrackSoundStart(category);

        soundEmitter.Play();
        return soundEmitter;
    }
}