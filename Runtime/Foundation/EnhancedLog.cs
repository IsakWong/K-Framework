using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 日志级别（从低到高）
/// 运行时只有 >= 当前阈值的日志才会输出
/// </summary>
public enum LogLevel
{
    /// <summary>最详细的跟踪信息，仅开发阶段使用</summary>
    Verbose = 0,
    /// <summary>调试信息</summary>
    Debug = 1,
    /// <summary>一般运行信息</summary>
    Info = 2,
    /// <summary>潜在问题警告</summary>
    Warning = 3,
    /// <summary>可恢复的错误</summary>
    Error = 4,
    /// <summary>不可恢复的致命错误</summary>
    Fatal = 5,
    /// <summary>关闭所有日志</summary>
    Off = 6,
}

/// <summary>
/// 日志文件配置
/// </summary>
public class LogFileConfig
{
    /// <summary>日志文件存储目录（默认项目根目录）</summary>
    public string LogDirectory;
    /// <summary>日志文件名前缀</summary>
    public string FilePrefix = "game";
    /// <summary>单个日志文件最大字节数（默认 5MB）</summary>
    public long MaxFileSize = 5 * 1024 * 1024;
    /// <summary>最多保留多少个历史日志文件（默认 5）</summary>
    public int MaxFileCount = 5;
    /// <summary>是否启用文件日志</summary>
    public bool Enabled = true;
    /// <summary>文件日志最低级别（默认 Info，Verbose/Debug 不写文件避免性能问题）</summary>
    public LogLevel MinFileLevel = LogLevel.Info;
}

/// <summary>
/// 分级日志系统 — 支持模块 Tag、运行时级别过滤、本地文件日志（带轮转）
///
/// 特性：
///   • 6 个日志级别：Verbose / Debug / Info / Warning / Error / Fatal
///   • 模块 Tag 过滤：可按 Tag 单独设置级别
///   • 本地文件日志：大小轮转、历史保留
///   • 平台适配：Unity Console / 微信小游戏 / WebGL
///   • 向后兼容：原 EnhancedLog.Log() / LogError() 仍可用
///
/// <example>
/// // 基本用法
/// EnhancedLog.Info("SceneManager", "场景加载完成: MainMenu");
/// EnhancedLog.Warning("Sound", "AudioClip 为空，跳过播放");
/// EnhancedLog.Error("Network", $"HTTP 请求失败: {statusCode}");
///
/// // 设置全局级别（Release 推荐 Warning）
/// EnhancedLog.SetGlobalLevel(LogLevel.Warning);
///
/// // 单独放开某个模块的日志
/// EnhancedLog.SetTagLevel("SceneManager", LogLevel.Debug);
///
/// // 向后兼容
/// EnhancedLog.Log("旧代码仍然工作");
/// EnhancedLog.LogError("旧的错误日志也工作");
/// </example>
/// </summary>
public class EnhancedLog : KSingleton<EnhancedLog>, ILogService
{
    // ═══════════════════════════════════════════════════════════════
    //  配置
    // ═══════════════════════════════════════════════════════════════

    private LogLevel _globalLevel;
    private readonly Dictionary<string, LogLevel> _tagLevels = new();
    private readonly StringBuilder _sb = new();

    // ═══════════════════════════════════════════════════════════════
    //  文件日志
    // ═══════════════════════════════════════════════════════════════

    private LogFileConfig _fileConfig;
    private StreamWriter _logFile;
    private string _currentLogFilePath;
    private long _currentFileSize;

    // 级别标签（固定宽度，方便对齐）
    private static readonly string[] LevelTags =
    {
        "VRB", // Verbose
        "DBG", // Debug
        "INF", // Info
        "WRN", // Warning
        "ERR", // Error
        "FTL", // Fatal
        "OFF", // Off (不使用)
    };

    // Unity Console 颜色（仅 Editor 和 Development Build 生效）
    private static readonly string[] LevelColors =
    {
        "#888888", // Verbose — 灰
        "#4FC3F7", // Debug — 浅蓝
        "#FFFFFF", // Info — 白
        "#FFD54F", // Warning — 黄
        "#EF5350", // Error — 红
        "#D500F9", // Fatal — 紫
        "",
    };

    // ═══════════════════════════════════════════════════════════════
    //  构造 & 初始化
    // ═══════════════════════════════════════════════════════════════

    public EnhancedLog()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _globalLevel = LogLevel.Verbose;
#else
        _globalLevel = LogLevel.Info;
#endif
        _fileConfig = new LogFileConfig
        {
            LogDirectory = Path.Combine(Application.persistentDataPath, "Logs"),
        };
        InitFileLogger();
    }

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<ILogService>(this);
    }

    /// <summary>
    /// 使用自定义配置重新初始化文件日志
    /// </summary>
    public static void ConfigureFileLog(LogFileConfig config)
    {
        if (config == null) return;
        var inst = Instance;
        inst.CloseFileLogger();
        inst._fileConfig = config;
        if (config.Enabled)
            inst.InitFileLogger();
    }

    private void InitFileLogger()
    {
        if (!_fileConfig.Enabled) return;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL 不支持文件写入
        return;
#else
        try
        {
            if (!Directory.Exists(_fileConfig.LogDirectory))
                Directory.CreateDirectory(_fileConfig.LogDirectory);

            _currentLogFilePath = Path.Combine(
                _fileConfig.LogDirectory,
                $"{_fileConfig.FilePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            var fs = File.Open(_currentLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _logFile = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
            _currentFileSize = fs.Length;

            // 写入启动分隔线
            var header = $"\n===== KFramework Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====\n";
            _logFile.Write(header);
            _currentFileSize += Encoding.UTF8.GetByteCount(header);

            // 清理旧文件
            CleanupOldLogFiles();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[EnhancedLog] 文件日志初始化失败: {e.Message}");
        }
#endif
    }

    private void CloseFileLogger()
    {
        if (_logFile != null)
        {
            try
            {
                _logFile.Flush();
                _logFile.Close();
                _logFile.Dispose();
            }
            catch { /* ignore */ }
            _logFile = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  文件轮转
    // ═══════════════════════════════════════════════════════════════

    private void RotateIfNeeded()
    {
        if (_logFile == null || _currentFileSize < _fileConfig.MaxFileSize) return;

        CloseFileLogger();
        InitFileLogger();
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var dir = new DirectoryInfo(_fileConfig.LogDirectory);
            var files = dir.GetFiles($"{_fileConfig.FilePrefix}_*.log");
            if (files.Length <= _fileConfig.MaxFileCount) return;

            // 按创建时间排序，删除最旧的
            Array.Sort(files, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));
            var toDelete = files.Length - _fileConfig.MaxFileCount;
            for (int i = 0; i < toDelete; i++)
            {
                // 不删除当前正在使用的文件
                if (files[i].FullName == _currentLogFilePath) continue;
                files[i].Delete();
            }
        }
        catch { /* ignore */ }
    }

    // ═══════════════════════════════════════════════════════════════
    //  级别控制
    // ═══════════════════════════════════════════════════════════════

    /// <summary>设置全局最低日志级别</summary>
    public static void SetGlobalLevel(LogLevel level)
    {
        Instance._globalLevel = level;
    }

    /// <summary>获取当前全局日志级别</summary>
    public static LogLevel GetGlobalLevel() => Instance._globalLevel;

    /// <summary>为指定 Tag 单独设置级别（优先于全局级别）</summary>
    public static void SetTagLevel(string tag, LogLevel level)
    {
        Instance._tagLevels[tag] = level;
    }

    /// <summary>移除指定 Tag 的自定义级别（回退到全局级别）</summary>
    public static void ClearTagLevel(string tag)
    {
        Instance._tagLevels.Remove(tag);
    }

    /// <summary>清除所有 Tag 级别设置</summary>
    public static void ClearAllTagLevels()
    {
        Instance._tagLevels.Clear();
    }

    private bool ShouldLog(LogLevel level, string tag)
    {
        // Tag 级别优先于全局级别
        if (tag != null && _tagLevels.TryGetValue(tag, out var tagLevel))
            return level >= tagLevel;

        return level >= _globalLevel;
    }

    // ═══════════════════════════════════════════════════════════════
    //  核心输出
    // ═══════════════════════════════════════════════════════════════

    private void WriteLog(LogLevel level, string tag, string message)
    {
        if (!ShouldLog(level, tag)) return;

        var dt = DateTime.Now;
        var levelTag = LevelTags[(int)level];

        // 文件格式：[HH:mm:ss.fff][INF][Tag] message
        _sb.Clear();
        _sb.Append('[').Append(dt.ToString("HH:mm:ss.fff")).Append(']');
        _sb.Append('[').Append(levelTag).Append(']');
        if (tag != null)
            _sb.Append('[').Append(tag).Append(']');
        _sb.Append(' ').Append(message);
        var plainText = _sb.ToString();

        // 写入文件
        WriteToFile(level, plainText);

        // 写入 Unity Console（带颜色）
        WriteToPlatform(level, tag, message, dt);
    }

    private void WriteToPlatform(LogLevel level, string tag, string message, DateTime dt)
    {
        var levelTag = LevelTags[(int)level];

#if WEIXINMINIGAME
        var text = $"[{dt:HH:mm:ss.fff}][{levelTag}]{(tag != null ? $"[{tag}]" : "")} {message}";
        switch (level)
        {
            case LogLevel.Error:
            case LogLevel.Fatal:
            case LogLevel.Warning:
                WX.LogManagerWarn(text);
                break;
            default:
                WX.LogManagerInfo(text);
                break;
        }
#else
        // Unity Console 带颜色标记
        var color = LevelColors[(int)level];
        _sb.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _sb.Append("<color=").Append(color).Append('>');
#endif
        _sb.Append('[').Append(dt.ToString("HH:mm:ss.fff")).Append(']');
        _sb.Append('[').Append(levelTag).Append(']');
        if (tag != null)
            _sb.Append('[').Append(tag).Append(']');
        _sb.Append(' ').Append(message);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _sb.Append("</color>");
#endif
        var formatted = _sb.ToString();

        switch (level)
        {
            case LogLevel.Fatal:
            case LogLevel.Error:
                UnityEngine.Debug.LogError(formatted);
                break;
            case LogLevel.Warning:
                UnityEngine.Debug.LogWarning(formatted);
                break;
            default:
                UnityEngine.Debug.Log(formatted);
                break;
        }
#endif
    }

    private void WriteToFile(LogLevel level, string plainText)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return;
#else
        if (_logFile == null) return;
        if (level < _fileConfig.MinFileLevel) return;

        try
        {
            _logFile.WriteLine(plainText);
            _currentFileSize += Encoding.UTF8.GetByteCount(plainText) + 2; // +2 for \r\n
            RotateIfNeeded();
        }
        catch { /* ignore */ }
#endif
    }

    // ═══════════════════════════════════════════════════════════════
    //  分级 API（推荐使用）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>最详细的跟踪日志，仅开发期使用</summary>
    public static void Verbose(string tag, string message) => Instance.WriteLog(LogLevel.Verbose, tag, message);

    /// <summary>调试信息</summary>
    public static void Debug(string tag, string message) => Instance.WriteLog(LogLevel.Debug, tag, message);

    /// <summary>一般运行信息</summary>
    public static void Info(string tag, string message) => Instance.WriteLog(LogLevel.Info, tag, message);

    /// <summary>潜在问题警告</summary>
    public static void Warning(string tag, string message) => Instance.WriteLog(LogLevel.Warning, tag, message);

    /// <summary>可恢复的错误</summary>
    public static void Error(string tag, string message) => Instance.WriteLog(LogLevel.Error, tag, message);

    /// <summary>不可恢复的致命错误</summary>
    public static void Fatal(string tag, string message) => Instance.WriteLog(LogLevel.Fatal, tag, message);

    // ═══════════════════════════════════════════════════════════════
    //  向后兼容 API（旧代码无需修改）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>[兼容] 等价于 Info(null, message)</summary>
    public static void Log(string message) => Instance.WriteLog(LogLevel.Info, null, message);

    /// <summary>[兼容] 带颜色的 Info 日志（颜色仅在 Console 生效）</summary>
    public static void Log(string message, Color color)
    {
        var inst = Instance;
        if (!inst.ShouldLog(LogLevel.Info, null)) return;

        var dt = DateTime.Now;
        var hex = ColorToHex(color);

        // 文件写入（无颜色）
        inst._sb.Clear();
        inst._sb.AppendFormat("[{0}][INF] {1}", dt.ToString("HH:mm:ss.fff"), message);
        inst.WriteToFile(LogLevel.Info, inst._sb.ToString());

        // Console 写入（带颜色）
        var consoleText = $"<color=#{hex}>[{dt:HH:mm:ss.fff}][INF] {message}</color>";
#if WEIXINMINIGAME
        WX.LogManagerInfo(consoleText);
#else
        UnityEngine.Debug.Log(consoleText);
#endif
    }

    /// <summary>[兼容] 等价于 Error(null, message)</summary>
    public static void LogError(string message) => Instance.WriteLog(LogLevel.Error, null, message);

    /// <summary>[兼容] 等价于 LogError</summary>
    public static void LogErrorFormat(string message) => Instance.WriteLog(LogLevel.Error, null, message);

    // ═══════════════════════════════════════════════════════════════
    //  颜色工具（保留原有功能）
    // ═══════════════════════════════════════════════════════════════

    public static string ColorToHex(Color color)
    {
        var r = Mathf.RoundToInt(color.r * 255.0f);
        var g = Mathf.RoundToInt(color.g * 255.0f);
        var b = Mathf.RoundToInt(color.b * 255.0f);
        var a = Mathf.RoundToInt(color.a * 255.0f);
        return $"{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    public static Color HexToColor(string hex)
    {
        var br = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var bg = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var bb = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var cc = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color(br / 255f, bg / 255f, bb / 255f, cc / 255f);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ILogService 实例方法
    // ═══════════════════════════════════════════════════════════════

    void ILogService.Verbose(string tag, string message) => WriteLog(LogLevel.Verbose, tag, message);
    void ILogService.Debug(string tag, string message) => WriteLog(LogLevel.Debug, tag, message);
    void ILogService.Info(string tag, string message) => WriteLog(LogLevel.Info, tag, message);
    void ILogService.Warning(string tag, string message) => WriteLog(LogLevel.Warning, tag, message);
    void ILogService.Error(string tag, string message) => WriteLog(LogLevel.Error, tag, message);
    void ILogService.Fatal(string tag, string message) => WriteLog(LogLevel.Fatal, tag, message);

    void ILogService.SetGlobalLevel(LogLevel level) => _globalLevel = level;
    LogLevel ILogService.GlobalLevel => _globalLevel;
    void ILogService.SetTagLevel(string tag, LogLevel level) => _tagLevels[tag] = level;
    void ILogService.ClearTagLevel(string tag) => _tagLevels.Remove(tag);
}