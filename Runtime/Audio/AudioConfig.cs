using Framework.Config;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "KFramework/AudioConfig")]
public class AudioConfig : ConfigObject
{
    [Header("Prefab")]
    public GameObject AudioPrefab;

    [Header("Sound Category Presets")]
    [Tooltip("默认 SFX 分类（2D）")]
    public SoundCategory DefaultCategory;
    [Tooltip("默认 3D SFX 分类")]
    public SoundCategory Default3DCategory;
    [Tooltip("角色施法音效分类")]
    public SoundCategory CharacterCastCategory;

    [Header("Mixer Snapshots")]
    public AudioMixerSnapshot UISnapShot;
    public AudioMixerSnapshot GameplaySnapshot;

    [Header("Mixer Exposed Parameter Names")]
    [Tooltip("Master 总线暴露参数名")]
    public string MasterVolumeParam = "Master_Volume";
    [Tooltip("BGM 总线暴露参数名")]
    public string BGMVolumeParam = "BGM_Volume";
    [Tooltip("SFX 总线暴露参数名")]
    public string SFXVolumeParam = "SFX_Volume";
    [Tooltip("UI 总线暴露参数名")]
    public string UIVolumeParam = "UI_Volume";
    [Tooltip("环境音总线暴露参数名")]
    public string AmbienceVolumeParam = "Ambience_Volume";

    private static AudioConfig _instance;

    public static AudioConfig Instance
    {
        get
        {
            if (_instance)
                return _instance;

            _instance = ConfigManager.Instance.GetConfig<AudioConfig>();
            return _instance;
        }
    }
}