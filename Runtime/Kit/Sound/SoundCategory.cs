using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音效分类预设（ScriptableObject 资产）
/// 代表一类音效（如"敌人受伤"、"弹幕"、"UI点击"），提供：
///   - Mixer 路由（指定 AudioMixerGroup）
///   - 并发控制（最大实例数、冷却时间、逐实例音量衰减）
///   - 音效变化（随机 Pitch）
///   - AudioSource 默认参数模板
///
/// 作为 ScriptableObject 资产，天然共享引用，
/// 同一 SoundCategory 实例的所有播放请求共享并发计数。
/// </summary>
[CreateAssetMenu(fileName = "NewSoundCategory", menuName = "KFramework/Sound Category")]
public class SoundCategory : ScriptableObject
{
    [Header("Mixer Routing")]
    [Tooltip("此类音效输出到的 AudioMixerGroup")]
    public AudioMixerGroup mixerGroup;

    [Header("Concurrency Control")]
    [Tooltip("最大同时播放数量（0 = 不限制）")]
    public int maxConcurrent = 0;

    [Tooltip("同类音效最小播放间隔（秒），防止连续触发叠加")]
    public float cooldown = 0f;

    [Tooltip("每多一个并发实例的音量衰减（线性），如 0.15 表示第 2 个实例 -0.15")]
    public float volumeDecayPerInstance = 0f;

    [Tooltip("标记为高频音效（受 SoundManager.maxSoundInstances 全局上限限制）")]
    public bool frequentSound;

    [Header("Variation")]
    [Tooltip("随机 Pitch 偏移范围 ±（0 = 不随机），如 0.08 表示 pitch ∈ [0.92, 1.08]")]
    public float randomPitchRange = 0f;

    [Header("AudioSource Defaults")]
    [Tooltip("此分类音效的默认 AudioSource 参数模板")]
    public SoundData defaults = new();
}
