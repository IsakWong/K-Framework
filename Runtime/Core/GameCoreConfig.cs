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
            _cached = AssetManager.Instance.LoadAsset<GameCoreConfig>("Assets/Resources/GameCoreConfig.asset");
        return _cached;
    }
}
