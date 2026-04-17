using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class RandomAudioClip : MonoBehaviour
{
    public List<AudioClip> mAudios;
    public float mDelay = 0;
    public float volume = 1;
    public bool Is3D = true;
    public bool IsLoop = false;
    public float LoopDuration = 0.0f;
    public AudioMixerGroup OverrideMixerGroup;

    [NonSerialized] public SoundEmitter soundEmitter;
    public bool AutoPlay = true;
    public bool LimitFrame = false;

    public void Stop()
    {
        if (soundEmitter != null)
            soundEmitter.Stop();
    }

    private void Play()
    {
        if (mAudios == null || mAudios.Count == 0) return;

        var clip = mAudios.RandomAccess();
        var sb = SoundManager.Instance.CreateSoundBuilder();

        if (Is3D)
            sb.With3DPosition(transform.position);

        if (OverrideMixerGroup != null)
            sb.OverrideMixerGroup(OverrideMixerGroup);

        sb.OverrideVolume(volume);
        sb.OverrideClip(clip);

        soundEmitter = sb.Play();

        if (IsLoop && LoopDuration > 0f)
            Invoke(nameof(Play), LoopDuration);
    }

    private void OnEnable()
    {
        if (!AutoPlay) return;

        if (mDelay > 0)
            Invoke(nameof(Play), mDelay);
        else
            Play();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Play));
    }

    private void Update()
    {
        if (Is3D && soundEmitter != null)
            soundEmitter.transform.position = transform.position;
    }
}