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
