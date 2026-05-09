# 音频系统

基于 SoundCategory / SoundData 双层架构的统一音频管理。包含对象池、并发限制、冷却时间、音量衰减、AudioMixer 控制、BGM Ducking、Snapshot 过渡、交叉淡入淡出和 3D 音效。

## 双层架构

| 层级 | 类型 | 职责 |
|------|------|------|
| **SoundCategory** | ScriptableObject | 音频分类：Mixer 路由、并发上限、冷却、衰减、随机音高 |
| **SoundData** | ScriptableObject | AudioSource 参数模板：优先级、空间混合、距离衰减曲线 |

`SoundCategory` 管"怎么播"，`SoundData` 管"参数是什么"。

## API

```csharp
// 播放音效
SoundManager.Instance.PlaySound(clip, category);

// 播放 3D 音效
SoundManager.Instance.PlaySound3D(clip, position);

// 播放音乐
SoundManager.Instance.PlayMusic(clip);

// 停止当前音乐
SoundManager.Instance.PopTrack();

// Mixer 控制
SoundManager.Instance.SetMixerVolume("MusicVolume", 0.8f);
float vol = SoundManager.Instance.GetMixerVolume("MusicVolume");

// Snapshot 过渡
SoundManager.Instance.TransitionToSnapshot(snapshot);

// BGM Ducking（侧链压缩）
SoundManager.Instance.DuckBGM(duration);
SoundManager.Instance.UnduckBGM();

// 音量
SoundManager.Instance.MusicVolume = 0.8f;

// 查询是否可播放（考虑并发/冷却限制）
bool canPlay = SoundManager.Instance.CanPlaySound(soundData);

// 获取有效音量（含逐实例衰减）
float effectiveVol = SoundManager.Instance.GetEffectiveVolume(data, 1f);
```

## SoundCategory 参数

| 参数 | 说明 |
|------|------|
| Mixer Group | AudioMixer 输出路由 |
| Max Concurrent | 最大同时播放数（超出时停止最旧的） |
| Cooldown | 同一 Clip 最小播放间隔 |
| Volume Attenuation | 逐实例音量衰减（多实例堆叠时自动降低每个实例音量） |
| Pitch Randomization | 随机音高范围 |

## SoundEmitter

`SoundEmitter` 是 AudioSource 的池化包装，实现 `IPoolable` 接口，由 `PoolManager` 统一管理。

- `OnSpawned()` — 从池中取出时初始化
- `OnDespawned()` — 回池时清理
- `OnFinished` 信号 — 播放完成时回调

## 交叉淡入淡出

```csharp
// 自动淡入淡出切换音乐
// 由 SoundCategory 的 CrossFade 参数控制
```

## 帧去重

同一帧内多次调用 `PlaySound` 播放同一 clip 会自动去重，只播一次。
