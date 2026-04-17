namespace Framework.Settings
{
    /// <summary>
    /// 设置管理服务接口
    /// 提供游戏设置的读取和应用
    /// </summary>
    public interface ISettingsService
    {
        Settings CurrentSettings { get; }
        void LoadSettings();
    }
}
