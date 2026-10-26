using UnityEngine;

[CreateAssetMenu(fileName = "GameCoreConfig", menuName = "JASA/核心/游戏核心配置")]
class GameCoreConfig : ScriptableObject
{
    public GameObject DefaultSoundEmitter;
    public bool IsDevelopment = true;

    private static GameCoreConfig _cached;

    public static GameCoreConfig Instance()
    {
        if (_cached == null)
        {
            // 启动期可能早于预热（例如 SoundService.Awake），此时返回 null 由调用方延迟初始化，
            // 预热完成后再取。不能用同步 API，否则三端都会在启动阶段抛错。
            AssetService.Instance.TryLoad<GameCoreConfig>(ConfigPath, out _cached);
        }
        return _cached;
    }

    private const string ConfigPath = "Assets/Config/GameCoreConfig.asset";
}
