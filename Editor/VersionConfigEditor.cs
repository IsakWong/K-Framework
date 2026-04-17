#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// VersionConfig 的自定义 Inspector
/// 提供版本管理快捷操作：自增版本号、同步 PlayerSettings、刷新 Git 信息
/// </summary>
[CustomEditor(typeof(VersionConfig))]
public class VersionConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var config = (VersionConfig)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("版本管理工具", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Major +1"))
        {
            BumpVersion(config, 0);
        }
        if (GUILayout.Button("Minor +1"))
        {
            BumpVersion(config, 1);
        }
        if (GUILayout.Button("Patch +1"))
        {
            BumpVersion(config, 2);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build +1"))
        {
            Undo.RecordObject(config, "Increment Build Number");
            config.BuildNumber++;
            MarkDirty(config);
        }
        if (GUILayout.Button("刷新 Git Hash"))
        {
            Undo.RecordObject(config, "Refresh Git Hash");
            config.GitCommitHash = GetGitCommitHash();
            MarkDirty(config);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("同步到 PlayerSettings"))
        {
            SyncToPlayerSettings(config);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            $"完整版本: {config.GameVersion} (Build {config.BuildNumber})\n" +
            $"框架版本: KFramework v{KVersion.FrameworkVersion}\n" +
            $"Git: {config.GitCommitHash}",
            MessageType.Info);
    }

    private void BumpVersion(VersionConfig config, int segment)
    {
        if (!KVersion.TryParseVersion(config.GameVersion, out var major, out var minor, out var patch))
        {
            Debug.LogError("[VersionConfig] 无法解析当前版本号，请确保格式为 X.Y.Z");
            return;
        }

        Undo.RecordObject(config, "Bump Version");

        switch (segment)
        {
            case 0:
                major++;
                minor = 0;
                patch = 0;
                break;
            case 1:
                minor++;
                patch = 0;
                break;
            case 2:
                patch++;
                break;
        }

        config.GameVersion = $"{major}.{minor}.{patch}";
        MarkDirty(config);
    }

    private void SyncToPlayerSettings(VersionConfig config)
    {
        PlayerSettings.bundleVersion = config.GameVersion;
#if UNITY_ANDROID
        PlayerSettings.Android.bundleVersionCode = config.BuildNumber;
#elif UNITY_IOS
        PlayerSettings.iOS.buildNumber = config.BuildNumber.ToString();
#endif
        Debug.Log($"[VersionConfig] PlayerSettings synced: v{config.GameVersion} Build {config.BuildNumber}");
    }

    private static void MarkDirty(VersionConfig config)
    {
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 执行 git rev-parse --short HEAD 获取当前 commit hash
    /// </summary>
    public static string GetGitCommitHash()
    {
        try
        {
            var psi = new ProcessStartInfo("git", "rev-parse --short HEAD")
            {
                WorkingDirectory = Application.dataPath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "unknown";

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);
            return string.IsNullOrEmpty(output) ? "unknown" : output;
        }
        catch (Exception)
        {
            return "unknown";
        }
    }
}

/// <summary>
/// 构建前自动处理器：自增 BuildNumber、写入构建日期和 Git Hash
/// </summary>
public class VersionBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        var config = Resources.Load<VersionConfig>("VersionConfig");
        if (config == null)
        {
            Debug.LogWarning("[VersionBuildProcessor] VersionConfig not found in Resources. Skipping auto-version.");
            return;
        }

        config.BuildNumber++;
        config.BuildDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        config.GitCommitHash = VersionConfigEditor.GetGitCommitHash();

        PlayerSettings.bundleVersion = config.GameVersion;
#if UNITY_ANDROID
        PlayerSettings.Android.bundleVersionCode = config.BuildNumber;
#elif UNITY_IOS
        PlayerSettings.iOS.buildNumber = config.BuildNumber.ToString();
#endif

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        Debug.Log($"[VersionBuildProcessor] Build #{config.BuildNumber} | {config.GameVersion} | {config.GitCommitHash} | {config.BuildDate}");
    }
}
#endif
