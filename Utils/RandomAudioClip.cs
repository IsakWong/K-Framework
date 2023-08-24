using System.Collections.Generic;
using UnityEngine;

public class RandomAudioClip : MonoBehaviour
{
    // Start is called before the first frame update
    public List<AudioClip> mAudios;
    public float mDelay = 0;
    public float volume = 1;
    public bool Is3D = true;
    public bool IsLoop = false;
    public float LoopDuration = 0.0f;
    private AudioSource mAudioSource;
    public bool AutoPlay = true;
    private bool _played = false;

    void Awake()
    {
        mAudioSource = gameObject.GetComponent<AudioSource>();
        if (mAudioSource == null)
            mAudioSource = gameObject.AddComponent<AudioSource>();
        if (Is3D)
        {
            mAudioSource.spatialBlend = 1.0f;
            mAudioSource.dopplerLevel = 0;
            mAudioSource.minDistance = 5.0f;
            mAudioSource.maxDistance = 20.0f;
        }
    }

    public void Stop()
    {
        mAudioSource.Stop();
    }


    void Play()
    {
        if (mAudios.Count == 0 || mAudioSource == null)
            return;
        mAudioSource.clip = mAudios[Random.Range(0, mAudios.Count)];
        mAudioSource.volume = volume;
        mAudioSource.Play();
        if (IsLoop)
        {
            Invoke("Play", LoopDuration);
        }
    }

    void FixedUpdate()
    {
        if (_played)
            return;
        if (AutoPlay)
        {
            if (mDelay > 0)
            {
                Invoke("Play", mDelay);
            }
            else
            {
                Play();
            }
        }

        _played = true;
    }
}