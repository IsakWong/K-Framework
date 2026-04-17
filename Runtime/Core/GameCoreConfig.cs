using UnityEngine;

[CreateAssetMenu(fileName = "GameCoreConfig", menuName = "JASA/核心/游戏核心配置")]
class GameCoreConfig : ScriptableObject
{
    public GameObject DefaultSoundEmitter;
    public bool IsDevelopment = true;

    public static GameCoreConfig Instance()
    {
        return Resources.Load<GameCoreConfig>("GameCoreConfig");
    }
}