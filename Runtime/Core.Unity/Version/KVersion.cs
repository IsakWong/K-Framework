using System;
using UnityEngine;

/// <summary>
/// 框架版本信息管理
/// 提供框架版本、游戏版本、构建信息的统一访问入口
/// </summary>
public static class KVersion
{
    // ─── 框架版本（手动维护，随框架发布更新） ───
    public const int FrameworkMajor = 1;
    public const int FrameworkMinor = 0;
    public const int FrameworkPatch = 0;
    public const string FrameworkVersionLabel = "";  // e.g. "beta", "rc1"

    /// <summary>框架语义化版本号，如 "1.0.0" 或 "1.0.0-beta"</summary>
    public static string FrameworkVersion =>
        string.IsNullOrEmpty(FrameworkVersionLabel)
            ? $"{FrameworkMajor}.{FrameworkMinor}.{FrameworkPatch}"
            : $"{FrameworkMajor}.{FrameworkMinor}.{FrameworkPatch}-{FrameworkVersionLabel}";

    // ─── 游戏/项目版本（从 VersionConfig ScriptableObject 读取） ───

    private static VersionConfig _config;

    /// <summary>
    /// 获取 VersionConfig 配置（自动从 Resources 加载）
    /// </summary>
    public static VersionConfig Config
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<VersionConfig>("VersionConfig");
                if (_config == null)
                {
                    Debug.LogWarning("[KVersion] VersionConfig not found in Resources. Using defaults.");
                    _config = ScriptableObject.CreateInstance<VersionConfig>();
                }
            }
            return _config;
        }
    }

    /// <summary>游戏版本号，如 "1.2.3"</summary>
    public static string GameVersion => Config.GameVersion;

    /// <summary>构建号（每次构建自增）</summary>
    public static int BuildNumber => Config.BuildNumber;

    /// <summary>构建日期（ISO 8601）</summary>
    public static string BuildDate => Config.BuildDate;

    /// <summary>Git Commit Hash（短）</summary>
    public static string GitCommitHash => Config.GitCommitHash;

    /// <summary>构建渠道（如 "dev", "staging", "release"）</summary>
    public static string Channel => Config.Channel;

    // ─── 运行时信息 ───

    /// <summary>Unity 引擎版本</summary>
    public static string UnityVersion => Application.unityVersion;

    /// <summary>当前运行平台</summary>
    public static string Platform => Application.platform.ToString();

    /// <summary>是否为 Debug/Development 构建</summary>
    public static bool IsDebugBuild => Debug.isDebugBuild;

    // ─── 格式化输出 ───

    /// <summary>
    /// 完整版本字符串，如 "MyGame v1.2.3 (Build 42) | KFramework v1.0.0"
    /// </summary>
    public static string FullVersionString =>
        $"{Application.productName} v{GameVersion} (Build {BuildNumber}) | KFramework v{FrameworkVersion}";

    /// <summary>
    /// 详细构建信息（适合 Debug 面板或日志）
    /// </summary>
    public static string DetailedBuildInfo =>
        $"Game: {Application.productName} v{GameVersion}\n" +
        $"Framework: KFramework v{FrameworkVersion}\n" +
        $"Build: #{BuildNumber} ({BuildDate})\n" +
        $"Channel: {Channel}\n" +
        $"Git: {GitCommitHash}\n" +
        $"Unity: {UnityVersion}\n" +
        $"Platform: {Platform}\n" +
        $"Debug: {IsDebugBuild}";

    // ─── 版本比较工具 ───

    /// <summary>
    /// 解析语义化版本字符串为 (major, minor, patch)
    /// </summary>
    public static bool TryParseVersion(string versionStr, out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        if (string.IsNullOrEmpty(versionStr)) return false;

        // 去掉 label 部分（如 "-beta"）
        var dashIdx = versionStr.IndexOf('-');
        var clean = dashIdx >= 0 ? versionStr.Substring(0, dashIdx) : versionStr;

        var parts = clean.Split('.');
        if (parts.Length < 3) return false;

        return int.TryParse(parts[0], out major)
            && int.TryParse(parts[1], out minor)
            && int.TryParse(parts[2], out patch);
    }

    /// <summary>
    /// 比较两个语义化版本号。返回值 &lt;0 表示 a 更旧, 0 表示相同, &gt;0 表示 a 更新
    /// </summary>
    public static int CompareVersions(string versionA, string versionB)
    {
        TryParseVersion(versionA, out var aMaj, out var aMin, out var aPat);
        TryParseVersion(versionB, out var bMaj, out var bMin, out var bPat);

        var majCmp = aMaj.CompareTo(bMaj);
        if (majCmp != 0) return majCmp;

        var minCmp = aMin.CompareTo(bMin);
        if (minCmp != 0) return minCmp;

        return aPat.CompareTo(bPat);
    }

    /// <summary>
    /// 检查当前游戏版本是否 >= 指定版本
    /// </summary>
    public static bool IsGameVersionAtLeast(string requiredVersion)
    {
        return CompareVersions(GameVersion, requiredVersion) >= 0;
    }

    // ─── 初始化日志 ───

    /// <summary>
    /// 在游戏启动时打印版本信息到控制台（建议在 KGameCore.Init 中调用）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LogVersionOnStartup()
    {
        Debug.Log($"[KVersion] {FullVersionString}");
    }
}
