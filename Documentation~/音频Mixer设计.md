# 音频Mixer设计

## 来自你的消息:
一个肉鸽unity游戏的音频Mixer应该如何设计，我有特效，弹幕，受伤。多个敌人同时受伤的时候如何保证不音量爆炸，以及背景音乐如何和其他音效混合


## 来自 BrainMaker 的消息:
# Unity 肉鸽游戏 Audio Mixer 设计方案

## 一、Mixer 层级架构

```
Master
├── BGM
│   ├── BattleBGM
│   └── MenuBGM
├── SFX (所有音效的父级)
│   ├── PlayerSFX
│   │   ├── Attack
│   │   ├── Skill
│   │   └── Hurt
│   ├── EnemySFX
│   │   ├── EnemyHurt      ← 关键：需要特殊处理
│   │   ├── EnemyAttack
│   │   └── EnemyDeath
│   ├── Bullet             ← 弹幕音效
│   └── VFX                ← 环境特效（爆炸、火焰等）
├── UI
│   ├── UIClick
│   └── UINotification
└── Ambience                ← 环境氛围音
```

## 二、Mixer 参数设置参考

```
Group               Volume(dB)    备注
─────────────────────────────────────────────
Master              0 dB
BGM                 -6 dB        略低于SFX，给音效留空间
SFX                 -3 dB
  PlayerSFX         0 dB         玩家反馈最重要
  EnemySFX          -3 dB        相对玩家稍低
    EnemyHurt       -6 dB        多实例叠加，基础音量要低
  Bullet            -6 dB        弹幕量大，基础音量必须低
  VFX               -3 dB
UI                  -3 dB
Ambience            -12 dB
```

## 三、核心问题：多敌人同时受伤的音量爆炸

这是肉鸽最常见的问题。解决方案是**多层防御**：

### 第 1 层：Mixer 上加 Compressor / Limiter

```
在 EnemySFX Group 上添加以下 Effect：

1. Duck Volume（侧链压缩，后面详述）
2. Compressor：
   - Threshold:  -10 dB    ← 超过此值开始压缩
   - Ratio:      4:1        ← 压缩比
   - Attack:     1 ms       ← 快速响应
   - Release:    50 ms
   
3. Limiter（在 SFX 总线上）：
   - Threshold:  -3 dB     ← 硬限制天花板
   - 防止任何情况下爆音
```

在 Unity Mixer 中操作：

```
选中 EnemyHurt Group → Add Effect → Compressor
选中 SFX Group       → Add Effect → Limiter
```

### 第 2 层：代码层面做实例数限制与冷却

```csharp
public class SFXPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class SFXConfig
    {
        public string id;
        public AudioClip[] clips;          // 多个变体随机播放
        public int maxConcurrent = 3;       // 最大同时播放数
        public float cooldown = 0.05f;      // 最小播放间隔(秒)
        public float volumeBase = 0.7f;
        public float volumeDecayPerInstance = 0.15f; // 每多一个实例衰减
        public AudioMixerGroup mixerGroup;
    }

    [SerializeField] private SFXConfig[] sfxConfigs;
    
    private Dictionary<string, SFXConfig> _configMap;
    private Dictionary<string, float> _lastPlayTime;
    private Dictionary<string, int> _activeCounts;

    private ObjectPool<AudioSource> _sourcePool;

    void Awake()
    {
        _configMap = new Dictionary<string, SFXConfig>();
        _lastPlayTime = new Dictionary<string, float>();
        _activeCounts = new Dictionary<string, int>();
        
        foreach (var cfg in sfxConfigs)
        {
            _configMap[cfg.id] = cfg;
            _lastPlayTime[cfg.id] = -999f;
            _activeCounts[cfg.id] = 0;
        }
        
        // 对象池初始化（简化）
        _sourcePool = new ObjectPool<AudioSource>(
            createFunc: () => {
                var go = new GameObject("PooledAudio");
                go.transform.SetParent(transform);
                return go.AddComponent<AudioSource>();
            },
            actionOnGet: src => src.gameObject.SetActive(true),
            actionOnRelease: src => src.gameObject.SetActive(false),
            defaultCapacity: 20,
            maxSize: 40
        );
    }

    /// <summary>
    /// 播放音效的唯一入口
    /// </summary>
    public void Play(string id, Vector3? position = null)
    {
        if (!_configMap.TryGetValue(id, out var cfg)) return;

        // ---- 冷却检查 ----
        float timeSinceLast = Time.unscaledTime - _lastPlayTime[id];
        if (timeSinceLast < cfg.cooldown) return;

        // ---- 并发数检查 ----
        if (_activeCounts[id] >= cfg.maxConcurrent) return;

        // ---- 计算衰减音量 ----
        float volume = cfg.volumeBase 
            - cfg.volumeDecayPerInstance * _activeCounts[id];
        volume = Mathf.Max(volume, 0.1f); // 保底音量

        // ---- 随机选 clip（避免重复感）----
        var clip = cfg.clips[Random.Range(0, cfg.clips.Length)];

        // ---- 从池中取 AudioSource ----
        var source = _sourcePool.Get();
        source.clip = clip;
        source.volume = volume;
        source.outputAudioMixerGroup = cfg.mixerGroup;
        source.spatialBlend = position.HasValue ? 0.7f : 0f;
        if (position.HasValue)
            source.transform.position = position.Value;
        
        // 随机 pitch 增加变化感
        source.pitch = Random.Range(0.92f, 1.08f);
        source.Play();

        _lastPlayTime[id] = Time.unscaledTime;
        _activeCounts[id]++;

        StartCoroutine(ReleaseAfterPlay(source, clip.length / source.pitch, id));
    }

    private IEnumerator ReleaseAfterPlay(AudioSource source, float duration, string id)
    {
        yield return new WaitForSecondsRealtime(duration);
        source.Stop();
        _sourcePool.Release(source);
        _activeCounts[id]--;
    }
}
```

### 第 3 层：大量敌人受伤时合并为"群体受伤音效"

```csharp
public class MassHitSFXHandler : MonoBehaviour
{
    [SerializeField] private SFXPoolManager sfxManager;
    [SerializeField] private int massThreshold = 5; // 超过5个同帧受伤就用群体音效
    
    private int _hitCountThisFrame = 0;
    private bool _frameProcessed = false;

    /// <summary>
    /// 每个敌人受伤时调用此方法，而非直接播放音效
    /// </summary>
    public void RegisterEnemyHit()
    {
        _hitCountThisFrame++;
        
        if (!_frameProcessed)
        {
            _frameProcessed = true;
            StartCoroutine(ProcessHitsEndOfFrame());
        }
    }

    private IEnumerator ProcessHitsEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        if (_hitCountThisFrame >= massThreshold)
        {
            // 大量敌人同时受伤 → 播放一个"群体受击"音效
            sfxManager.Play("enemy_hurt_mass");
        }
        else
        {
            // 少量 → 正常逐个播放（受 SFXPoolManager 的并发限制保护）
            for (int i = 0; i < _hitCountThisFrame; i++)
            {
                sfxManager.Play("enemy_hurt_single");
            }
        }

        _hitCountThisFrame = 0;
        _frameProcessed = false;
    }
}
```

## 四、BGM 与音效的混合：侧链压缩（Duck Volume）

当战斗音效激烈时，自动压低 BGM，战斗结束后恢复。

### 方案 A：Unity Mixer 内置 Duck Volume

```
操作步骤：
1. 选中 BGM Group → Add Effect → Duck Volume
2. 在 Duck Volume 的 Inspector 中设置：
   - 右键 "Receive" 选择 SFX Group 作为 Side Chain 输入
   
参数设置：
   - Threshold:    -20 dB
   - Ratio:        2:1
   - Attack Time:  20 ms
   - Release Time: 500 ms     ← 较慢恢复，避免BGM忽大忽小
   - Knee:         10 dB      ← 柔和过渡
```

> **效果**：当 SFX 总线音量超过阈值，BGM 自动降低，音效结束后平滑恢复。

### 方案 B：代码控制（更精细）

```csharp
public class BGMDucker : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string bgmVolumeParam = "BGM_Volume"; // Expose 的参数名
    
    [SerializeField] private float normalVolume = -6f;   // dB
    [SerializeField] private float duckedVolume = -18f;   // dB
    [SerializeField] private float duckSpeed = 5f;
    [SerializeField] private float recoverSpeed = 1.5f;
    
    private float _targetVolume;
    private float _currentVolume;
    private float _duckTimer;
    
    void Start()
    {
        _targetVolume = normalVolume;
        _currentVolume = normalVolume;
    }

    /// <summary>
    /// 战斗激烈时外部调用
    /// </summary>
    public void DuckBGM(float duration = 0.5f)
    {
        _targetVolume = duckedVolume;
        _duckTimer = duration;
    }

    void Update()
    {
        // 计时恢复
        if (_duckTimer > 0)
        {
            _duckTimer -= Time.unscaledDeltaTime;
            if (_duckTimer <= 0)
                _targetVolume = normalVolume;
        }

        // 平滑过渡
        float speed = (_targetVolume < _currentVolume) ? duckSpeed : recoverSpeed;
        _currentVolume = Mathf.MoveTowards(_currentVolume, _targetVolume, 
            speed * Time.unscaledDeltaTime * 10f);
        
        mixer.SetFloat(bgmVolumeParam, _currentVolume);
    }
}
```

> 记得在 Mixer 中右键 BGM 的 Volume → **Expose to Script**，命名为 `BGM_Volume`。

## 五、弹幕音效的特殊处理

弹幕数量可能成百上千，绝对不能每颗都播放音效：

```csharp
public class BulletSFXStrategy : MonoBehaviour
{
    [Header("发射音效")]
    [SerializeField] private float fireMinInterval = 0.08f;  // 连射间隔限制
    
    [Header("命中音效")]
    [SerializeField] private int maxHitSoundsPerFrame = 2;
    
    [Header("飞行音效")]
    [SerializeField] private AudioSource bulletLoopSource;    // 循环音效，用音量表示密度
    [SerializeField] private float loopVolumePerBullet = 0.02f;
    [SerializeField] private float loopVolumeMax = 0.6f;
    
    private float _lastFireTime;
    private int _hitSoundsThisFrame;
    private int _activeBulletCount;

    /// <summary>
    /// 弹幕发射 → 有冷却的播放
    /// </summary>
    public bool TryPlayFireSound()
    {
        if (Time.time - _lastFireTime < fireMinInterval) return false;
        _lastFireTime = Time.time;
        return true; // 调用方拿到 true 再播放
    }

    /// <summary>
    /// 弹幕命中 → 帧内限制
    /// </summary>
    public bool TryPlayHitSound()
    {
        if (_hitSoundsThisFrame >= maxHitSoundsPerFrame) return false;
        _hitSoundsThisFrame++;
        return true;
    }

    /// <summary>
    /// 用循环音效 + 动态音量表示弹幕密度
    /// 比逐颗播放飞行音效高效得多
    /// </summary>
    public void UpdateBulletDensity(int activeBulletCount)
    {
        _activeBulletCount = activeBulletCount;
        float targetVol = Mathf.Min(activeBulletCount * loopVolumePerBullet, loopVolumeMax);
        bulletLoopSource.volume = Mathf.Lerp(bulletLoopSource.volume, targetVol, 
            Time.deltaTime * 8f);
    }

    void LateUpdate()
    {
        _hitSoundsThisFrame = 0; // 每帧重置
    }
}
```

## 六、完整 Mixer 效果链路图

```
┌──────────────────────────────────────────────────────┐
│                      MASTER                          │
│  Effect: Limiter (Threshold: -1dB) ← 最终安全网     │
│                                                      │
│  ┌─────────────┐     ┌────────────────────────────┐  │
│  │     BGM      │     │           SFX              │  │
│  │ -6dB base    │     │  -3dB base                 │  │
│  │              │     │  Effect: Compressor         │  │
│  │ Duck Volume ←──────│  (Threshold:-10, Ratio:3:1)│  │
│  │ (SideChain   │     │                            │  │
│  │  from SFX)   │     │  ┌────────┐ ┌───────────┐ │  │
│  │              │     │  │PlayerSFX│ │ EnemySFX  │ │  │
│  │  ┌────────┐  │     │  │ 0dB    │ │ -3dB      │ │  │
│  │  │Battle  │  │     │  │        │ │ Compress  │ │  │
│  │  │BGM     │  │     │  │        │ │ -6dB Hurt │ │  │
│  │  └────────┘  │     │  └────────┘ └───────────┘ │  │
│  │  ┌────────┐  │     │  ┌────────┐ ┌───────────┐ │  │
│  │  │Menu    │  │     │  │Bullet  │ │   VFX     │ │  │
│  │  │BGM     │  │     │  │ -6dB   │ │  -3dB     │ │  │
│  │  └────────┘  │     │  └────────┘ └───────────┘ │  │
│  └─────────────┘     └────────────────────────────┘  │
│                                                      │
│  ┌─────────┐  ┌──────────┐                           │
│  │   UI    │  │ Ambience │                           │
│  │  -3dB   │  │  -12dB   │                           │
│  └─────────┘  └──────────┘                           │
└──────────────────────────────────────────────────────┘
```

## 七、设计原则总结

| 问题 | 解决方案 | 层级 |
|------|----------|------|
| 多敌人同时受伤爆音 | ① 并发数限制 ② 逐实例音量衰减 ③ Compressor ④ 大量时合并为群体音效 | 代码 + Mixer |
| 弹幕音效过多 | ① 发射冷却 ② 命中帧限制 ③ 用循环音效+动态音量代替逐颗播放 | 代码 |
| BGM 被音效淹没 | Duck Volume 侧链压缩，战斗激烈时自动压低 BGM | Mixer |
| BGM 盖住音效 | BGM 基础音量设低于 SFX，且做 EQ 避开音效频段 | Mixer |
| 最终安全保障 | Master 总线加 Limiter (-1dB)，任何情况不爆音 | Mixer |
| 音效重复感强 | 每个音效准备 3-5 个变体 + 随机 Pitch (±8%) | 素材 + 代码 |

> **核心理念**：**防御纵深** — 不要依赖单一手段。代码层做数量控制（治本），Mixer 层做压缩限制（兜底），两者配合才能在肉鸽这种"屏幕全是特效"的场景下保持良好的音频体验。