using System;
using System.Collections.Generic;
using DG.Tweening;
using Framework.Config;
using Framework.Foundation;
using UnityEngine;
using UnityEngine.Audio;


public class SoundManager : PersistentSingleton<SoundManager>, ISoundService
{
    // ─── SFX Pool (via PoolManager) ───
    private readonly List<SoundEmitter> activeSoundEmitters = new();
    public readonly LinkedList<SoundEmitter> FrequentSoundEmitters = new();

    private SoundEmitter soundEmitterPrefab;
    [SerializeField] private int poolPreloadCount = 10;
    [SerializeField] private int maxSoundInstances = 30;

    [Tooltip("未指定 SoundCategory 时使用的默认 AudioSource 参数")]
    [SerializeField] public SoundData DefaultSoundData = new();

    // ─── Per-SoundCategory Concurrency Tracking ───
    // Key = SoundCategory.GetInstanceID(), tracks active count and last play time
    private readonly Dictionary<int, int> _activeCountByCategory = new();
    private readonly Dictionary<int, float> _lastPlayTimeByCategory = new();

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
        if (!AudioListener)
        {
            var listenerObject = new GameObject("[AudioListener]");
            AudioListener = listenerObject.AddComponent<AudioListener>();
            DontDestroyOnLoad(listenerObject);
        }
    }

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<ISoundService>(this);
    }

    public SoundBuilder CreateSoundBuilder()
    {
        return new SoundBuilder(this);
    }

    /// <summary>
    /// 检查音效是否可以播放（并发数、冷却时间、全局限制）
    /// category 为 null 时始终允许播放
    /// </summary>
    public bool CanPlaySound(SoundCategory category)
    {
        if (category == null) return true;
        int key = category.GetInstanceID();

        // Per-category cooldown
        if (category.cooldown > 0f)
        {
            if (_lastPlayTimeByCategory.TryGetValue(key, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < category.cooldown)
                    return false;
            }
        }

        // Per-category concurrent limit
        if (category.maxConcurrent > 0)
        {
            _activeCountByCategory.TryGetValue(key, out int count);
            if (count >= category.maxConcurrent)
                return false;
        }

        // Global frequent sound limit
        if (category.frequentSound)
        {
            if (FrequentSoundEmitters.Count >= maxSoundInstances)
            {
                var oldest = FrequentSoundEmitters.First;
                if (oldest?.Value != null)
                {
                    oldest.Value.Stop();
                    return true;
                }
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 计算并发衰减后的实际音量
    /// </summary>
    public float GetEffectiveVolume(SoundCategory category, float baseVolume)
    {
        if (category == null || category.volumeDecayPerInstance <= 0f)
            return baseVolume;

        int key = category.GetInstanceID();
        _activeCountByCategory.TryGetValue(key, out int count);
        float decayed = baseVolume - category.volumeDecayPerInstance * count;
        return Mathf.Max(decayed, 0.05f);
    }

    /// <summary>
    /// 记录音效开始播放
    /// </summary>
    public void TrackSoundStart(SoundCategory category)
    {
        if (category == null) return;
        int key = category.GetInstanceID();
        _activeCountByCategory.TryGetValue(key, out int count);
        _activeCountByCategory[key] = count + 1;
        _lastPlayTimeByCategory[key] = Time.unscaledTime;
    }

    /// <summary>
    /// 记录音效结束播放
    /// </summary>
    public void TrackSoundEnd(SoundCategory category)
    {
        if (category == null) return;
        int key = category.GetInstanceID();
        if (_activeCountByCategory.TryGetValue(key, out int count))
        {
            _activeCountByCategory[key] = Mathf.Max(0, count - 1);
        }
    }

    public SoundEmitter Get()
    {
        var emitter = PoolManager.Instance.Get(soundEmitterPrefab);
        emitter.OnFinished = HandleEmitterFinished;
        activeSoundEmitters.Add(emitter);
        return emitter;
    }

    public void ReturnToPool(SoundEmitter soundEmitter)
    {
        // Domain tracking — must happen before IPoolable.OnReturnToPool clears Node
        if (soundEmitter.Node?.List != null)
            FrequentSoundEmitters.Remove(soundEmitter.Node);

        activeSoundEmitters.Remove(soundEmitter);
        PoolManager.Instance.Release(soundEmitter);
    }

    public void StopAll()
    {
        var temp = ListPool<SoundEmitter>.Get();
        temp.AddRange(activeSoundEmitters);
        foreach (var soundEmitter in temp)
        {
            soundEmitter.Stop();
        }
        ListPool<SoundEmitter>.Release(temp);
    }

    private void InitializePool()
    {
        soundEmitterPrefab = GameCoreConfig.Instance().DefaultSoundEmitter.GetComponent<SoundEmitter>();
        if (poolPreloadCount > 0)
            PoolManager.Instance.Preload(soundEmitterPrefab, poolPreloadCount);
    }

    private void HandleEmitterFinished(SoundEmitter emitter)
    {
        TrackSoundEnd(emitter.Category);
        ReturnToPool(emitter);
    }

    // ─── AudioListener ───

    public AudioListener AudioListener;
    public GameObject ListenerTarget;

    public void InitAudioListener(GameObject obj)
    {
        ListenerTarget = obj;
    }

    private void LateUpdate()
    {
        if (ListenerTarget)
        {
            AudioListener.transform.position = ListenerTarget.transform.position;
        }
    }

    // ─── Music System ───

    private const float crossFadeTime = 1.5f;
    private float fading;
    private AudioSource currentMusic;
    private AudioSource previousMusic;
    private readonly LinkedList<AudioSource> musicStack = new();
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    public void Clear()
    {
        foreach (var source in musicStack)
        {
            if (source) Destroy(source);
        }
        musicStack.Clear();
        currentMusic = null;
        previousMusic = null;
    }

    public void PopTrack()
    {
        previousMusic = musicStack.Last?.Value;
        if (previousMusic)
        {
            musicStack.RemoveLast();
            var toDestroy = previousMusic;
            previousMusic = null;

            currentMusic = musicStack.Last?.Value;
            if (currentMusic)
                currentMusic.Play();

            fading = 0.001f;

            DOVirtual.DelayedCall(crossFadeTime + 0.1f, () =>
            {
                if (toDestroy) Destroy(toDestroy);
            });
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (currentMusic && currentMusic.clip == clip) return;

        previousMusic = musicStack.Last?.Value;

        currentMusic = gameObject.AddComponent<AudioSource>();
        currentMusic.clip = clip;
        currentMusic.outputAudioMixerGroup = musicMixerGroup;
        currentMusic.loop = true;
        currentMusic.volume = 0;
        currentMusic.bypassListenerEffects = true;
        currentMusic.Play();
        musicStack.AddLast(currentMusic);

        fading = 0.001f;
    }

    public float MusicVolume { get; set; } = 1.0f;

    private void Update()
    {
        HandleCrossFade();
        UpdateDucking();
    }

    private void HandleCrossFade()
    {
        if (fading <= 0f) return;

        fading += Time.unscaledDeltaTime;
        var fraction = Mathf.Clamp01(fading / crossFadeTime);
        var logFraction = fraction.ToLogarithmicFraction();

        if (previousMusic)
            previousMusic.volume = Mathf.Max(0, MusicVolume * (1f - logFraction));

        if (currentMusic)
            currentMusic.volume = logFraction * MusicVolume;

        if (fraction >= 1f)
        {
            fading = 0.0f;
            if (previousMusic)
            {
                previousMusic.Stop();
                Destroy(previousMusic);
                previousMusic = null;
            }
        }
    }

    // ─── Convenient Play Methods ───

    public SoundEmitter PlaySound(AudioClip clip, float volume = 1.0f, SoundCategory category = null)
    {
        if (clip == null) return null;

        var emitter = CreateSoundBuilder()
            .OverrideVolume(volume)
            .OverrideClip(clip)
            .Play(category);
        return emitter;
    }

    public SoundEmitter PlaySound3D(AudioClip clip, Vector3 pos, SoundCategory category = null)
    {
        if (clip == null) return null;

        var emitter = CreateSoundBuilder()
            .With3DPosition(pos)
            .OverrideClip(clip)
            .Play(category);
        return emitter;
    }

    // ─── Frame-Limited Playback ───

    private readonly Dictionary<AudioClip, (SoundBuilder builder, SoundCategory category)> _frameLimitedEmitters = new();

    private void FixedUpdate()
    {
        foreach (var kvp in _frameLimitedEmitters)
        {
            kvp.Value.builder.Play(kvp.Value.category);
        }
        _frameLimitedEmitters.Clear();
    }

    public void PlaySoundLimitFrame(AudioClip clip, Vector3 pos, SoundCategory category = null)
    {
        if (clip == null) return;

        var builder = CreateSoundBuilder()
            .With3DPosition(pos)
            .OverrideClip(clip);

        if (category?.mixerGroup != null)
            builder.OverrideMixerGroup(category.mixerGroup);

        _frameLimitedEmitters[clip] = (builder, category);
    }

    // ─── Mixer Volume Control ───

    [SerializeField] private AudioMixer audioMixer;

    public bool SetMixerVolume(string exposedParam, float linearVolume)
    {
        if (audioMixer == null) return false;
        float dB = linearVolume > 0.0001f
            ? Mathf.Log10(linearVolume) * 20f
            : -80f;
        return audioMixer.SetFloat(exposedParam, dB);
    }

    public float GetMixerVolume(string exposedParam)
    {
        if (audioMixer == null) return 1f;
        if (audioMixer.GetFloat(exposedParam, out float dB))
            return Mathf.Pow(10f, dB / 20f);
        return 1f;
    }

    public void TransitionToSnapshot(AudioMixerSnapshot snapshot, float duration = 0.5f)
    {
        snapshot?.TransitionTo(duration);
    }

    public void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float duration = 0.5f)
    {
        if (audioMixer == null || snapshots == null) return;
        audioMixer.TransitionToSnapshots(snapshots, weights, duration);
    }

    // ─── BGM Ducking ───

    [Header("BGM Ducking")]
    [SerializeField] private string bgmVolumeParam = "BGM_Volume";
    [SerializeField] private float normalBgmDb = 0f;
    [SerializeField] private float duckedBgmDb = -12f;
    [SerializeField] private float duckAttackSpeed = 8f;
    [SerializeField] private float duckReleaseSpeed = 2f;

    private float _duckTimer;
    private float _currentBgmDb;
    private float _targetBgmDb;
    private bool _duckingActive;

    public void DuckBGM(float duration = 0.5f)
    {
        _targetBgmDb = duckedBgmDb;
        _duckTimer = duration;
        _duckingActive = true;
    }

    public void UnduckBGM()
    {
        _targetBgmDb = normalBgmDb;
        _duckTimer = 0f;
    }

    private void UpdateDucking()
    {
        if (!_duckingActive || audioMixer == null) return;

        if (_duckTimer > 0f)
        {
            _duckTimer -= Time.unscaledDeltaTime;
            if (_duckTimer <= 0f)
                _targetBgmDb = normalBgmDb;
        }

        float speed = (_targetBgmDb < _currentBgmDb) ? duckAttackSpeed : duckReleaseSpeed;
        _currentBgmDb = Mathf.MoveTowards(_currentBgmDb, _targetBgmDb, speed * Time.unscaledDeltaTime);
        audioMixer.SetFloat(bgmVolumeParam, _currentBgmDb);

        if (Mathf.Approximately(_currentBgmDb, normalBgmDb) && _duckTimer <= 0f)
            _duckingActive = false;
    }
}