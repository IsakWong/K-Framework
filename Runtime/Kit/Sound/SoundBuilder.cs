using UnityEngine;
using UnityEngine.Audio;

public class SoundBuilder
{
    private readonly SoundManager soundManager;
    private Vector3 position = Vector3.zero;
    private bool use3D;
    private float spatialOverride = -1f;

    public SoundBuilder(SoundManager soundManager)
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

    /// <summary>
    /// 播放音效。category 提供 Mixer 路由 + 并发控制 + AudioSource 默认参数。
    /// 为 null 时使用 SoundManager.DefaultSoundData 且不限流。
    /// </summary>
    public SoundEmitter Play(SoundCategory category = null)
    {
        if (!soundManager.CanPlaySound(category))
            return null;

        // Resolve AudioSource parameters: category.defaults → fallback to DefaultSoundData
        SoundData audioParams = category != null ? category.defaults : soundManager.DefaultSoundData;

        var soundEmitter = soundManager.Get();
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
        soundEmitter.audioSource.volume = soundManager.GetEffectiveVolume(category, baseVol);

        // Spatial
        if (spatialOverride >= 0f)
            soundEmitter.audioSource.spatialBlend = spatialOverride;

        if (use3D)
            soundEmitter.transform.position = position;

        soundEmitter.transform.parent = soundManager.transform;

        // Random pitch from category config
        if (category != null && category.randomPitchRange > 0f)
            soundEmitter.WithRandomPitch(category.randomPitchRange);

        // Frequent sound tracking
        if (category != null && category.frequentSound)
            soundEmitter.Node = soundManager.FrequentSoundEmitters.AddLast(soundEmitter);

        // Track concurrency
        soundManager.TrackSoundStart(category);

        soundEmitter.Play();
        return soundEmitter;
    }
}