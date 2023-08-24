using System;
using DG.Tweening;
using K1.Gameplay;
using UnityEngine;


public class AudioModule : KModule
{
    // Start is called before the first frame update
    public AudioSource mAudioOrigin;
    public AudioClip mWarningAudioClip;

    public AudioListener AudioListener;
    public GameObject ListenerTarget;


    public void AttachAudioListener(GameObject obj)
    {
        ListenerTarget = obj;
    }

    public override void OnInit()
    {
        var listenerObject = new GameObject();
        listenerObject.name = "Listener";
        listenerObject.transform.SetParent(transform);
        AudioListener = listenerObject.AddComponent<AudioListener>();
    }

    private void LateUpdate()
    {
        if (ListenerTarget)
        {
            AudioListener.transform.position = ListenerTarget.transform.position;
            AudioListener.transform.rotation = ListenerTarget.transform.rotation;
        }
    }

    public void Start()
    {
        // KGameCore.SystemAt<GameplayModule>().AnyEvent.OnGetStuned += (unit, characterUnit, time) =>
        // {
        //     KGameCore.SystemAt<AudioModule>().PlayAudio(unit.mCharacterConfig.StunAudio.RandomAccess());
        // };
        // KGameCore.SystemAt<GameplayModule>().AnyEvent.OnBreakAction += (unit, characterUnit) =>
        // {
        //     KGameCore.SystemAt<AudioModule>().PlayAudio(unit.mCharacterConfig.BreakActionAudio.RandomAccess());
        // };
    }

    private AudioSource BgmAudioSource = null;

    public void SwitchBgm(AudioClip clip)
    {
        if (BgmAudioSource)
        {
            BgmAudioSource.DOFade(0, 1.0f);
            Destroy(BgmAudioSource, 2.0f);
        }

        if (clip)
        {
            var newAudio = Instantiate(mAudioOrigin.gameObject);
            newAudio.transform.SetParent(transform.parent);
            var audio = newAudio.GetComponent<AudioSource>();
            audio.clip = clip;
            audio.volume = 1.0f;
            audio.loop = true;
            audio.Play();
            newAudio.transform.SetParent(transform);
            BgmAudioSource = audio;
        }
        else
        {
            BgmAudioSource = null;
        }
    }

    public AudioSource PlayAudio(AudioClip clip, float volume = 1.0f, bool autoDestroy = true)
    {
        if (clip == null)
            return null;
        var newAudio = Instantiate(mAudioOrigin.gameObject);
        newAudio.name = clip.name;
        newAudio.transform.SetParent(transform.parent);
        var audio = newAudio.GetComponent<AudioSource>();
        audio.volume = volume;
        audio.PlayOneShot(clip);
        newAudio.transform.SetParent(transform);
        if (autoDestroy)
        {
            Destroy(newAudio, clip.length + 0.1f);
        }

        return audio;
    }

    public AudioSource PlayAudioAtPosition(AudioClip clip, Vector3 pos, bool autoDestroy = true)
    {
        if (clip == null)
            return null;
        var newAudio = Instantiate(mAudioOrigin.gameObject);
        newAudio.name = clip.name;
        newAudio.transform.SetParent(transform.parent);
        var audio = newAudio.GetComponent<AudioSource>();
        audio.minDistance = 5.0f;
        audio.maxDistance = 30.0f;
        audio.volume = 1.0f;
        audio.dopplerLevel = 0;
        audio.PlayOneShot(clip);
        if (autoDestroy)
            Destroy(newAudio, clip.length + 0.1f);
        newAudio.transform.SetParent(transform);
        newAudio.transform.position = pos;
        return audio;
    }

    public void PlayAudioWithSubtitle(AudioClip clip, string subtitleTitle, string content)
    {
        var newAudio = Instantiate(mAudioOrigin.gameObject);
        newAudio.transform.SetParent(transform.parent);
        var audio = newAudio.GetComponent<AudioSource>();
        audio.dopplerLevel = 0;
        audio.PlayOneShot(clip);
        Destroy(newAudio, clip.length + 0.1f);
        newAudio.transform.SetParent(transform);
    }
}