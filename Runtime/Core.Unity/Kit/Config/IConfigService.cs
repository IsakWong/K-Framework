using Framework.Config;

namespace Framework.Config
{
    /// <summary>
    /// 配置管理服务接口
    /// 提供 ScriptableObject 配置的加载与缓存
    /// </summary>
    public interface IConfigService
    {
        T GetConfig<T>(string name = "") where T : ConfigObject;
    }
}
