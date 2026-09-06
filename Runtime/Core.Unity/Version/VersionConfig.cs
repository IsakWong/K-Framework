using UnityEngine;

/// <summary>
/// 版本配置 ScriptableObject
/// 放置于 Resources/VersionConfig.asset
/// 游戏版本、构建号、渠道等信息由 Editor 工具或 CI 脚本写入
/// </summary>
[CreateAssetMenu(fileName = "VersionConfig", menuName = "KFramework/Version Config")]
public class VersionConfig : ScriptableObject
{
    [Header("游戏版本")]
    [Tooltip("语义化版本号 (Major.Minor.Patch)")]
    public string GameVersion = "0.1.0";

    [Header("构建信息")]
    [Tooltip("构建号，每次构建自动递增")]
    public int BuildNumber;

    [Tooltip("构建日期 (ISO 8601)")]
    public string BuildDate = "";

    [Tooltip("Git Commit Hash (短)")]
    public string GitCommitHash = "";

    [Header("发布渠道")]
    [Tooltip("构建渠道: dev / staging / release")]
    public string Channel = "dev";
}
