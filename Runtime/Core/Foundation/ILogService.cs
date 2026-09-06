/// <summary>
/// 日志服务接口 — 通过 ServiceLocator 访问
///
/// <example>
/// var log = ServiceLocator.Get&lt;ILogService&gt;();
/// log.Info("Network", "连接成功");
/// log.SetGlobalLevel(LogLevel.Warning); // 只输出 Warning 及以上
/// </example>
/// </summary>
public interface ILogService
{
    void Verbose(string tag, string message);
    void Debug(string tag, string message);
    void Info(string tag, string message);
    void Warning(string tag, string message);
    void Error(string tag, string message);
    void Fatal(string tag, string message);

    /// <summary>设置全局最低日志级别</summary>
    void SetGlobalLevel(LogLevel level);

    /// <summary>当前全局日志级别</summary>
    LogLevel GlobalLevel { get; }

    /// <summary>为指定 Tag 设置单独级别（优先于全局）</summary>
    void SetTagLevel(string tag, LogLevel level);

    /// <summary>移除指定 Tag 的自定义级别</summary>
    void ClearTagLevel(string tag);
}
