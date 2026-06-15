namespace KFramework
{
    /// <summary>
    /// 框架服务统一生命周期接口。
    ///
    /// KSingleton 全局服务实现此接口，获得一致的 Init / Dispose 流程。
    /// 与 YokiFrame Architecture 中的 IService 对应。
    /// </summary>
    public interface IService
    {
        bool Initialized { get; }
        void Init();
        void Dispose();
    }
}
