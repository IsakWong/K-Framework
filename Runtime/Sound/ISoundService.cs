using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理服务接口
/// 提供音效播放、音乐播放、Mixer 控制、BGM Ducking 等能力
/// </summary>
public interface ISoundService
{
    // ─── 音效 ───

    SoundBuilder CreateSoundBuilder();
    SoundEmitter PlaySound(AudioClip clip, float volume = 1.0f, SoundCategory category = null);
    SoundEmitter PlaySound3D(AudioClip clip, Vector3 pos, SoundCategory category = null);
    void PlaySoundLimitFrame(AudioClip clip, Vector3 pos, SoundCategory category = null);
    bool CanPlaySound(SoundCategory category);
    SoundEmitter Get();
    void ReturnToPool(SoundEmitter soundEmitter);

    // ─── 并发控制 ───

    float GetEffectiveVolume(SoundCategory category, float baseVolume);
    void TrackSoundStart(SoundCategory category);
    void TrackSoundEnd(SoundCategory category);

    // ─── 音乐 ───

    void PlayMusic(AudioClip clip);
    void PopTrack();

    // ─── Mixer 控制 ───

    bool SetMixerVolume(string exposedParam, float linearVolume);
    float GetMixerVolume(string exposedParam);
    void TransitionToSnapshot(AudioMixerSnapshot snapshot, float duration = 0.5f);
    void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float duration = 0.5f);

    // ─── BGM Ducking ───

    void DuckBGM(float duration = 0.5f);
    void UnduckBGM();

    // ─── 全局控制 ───

    void StopAll();
    void Clear();
    void InitAudioListener(GameObject obj);
    float MusicVolume { get; set; }
}
