using System.Collections;
using Framework.Coroutine;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 可热插拔模块接口。
    ///
    /// 与 IService（全局永久服务）不同，IModule 是场景/玩法级别的
    /// 动态模块，由 KGameCore 独立管理，可随时装卸。
    /// </summary>
    public interface IModule
    {
        bool Initialized { get; }
        void Init();
        void Dispose();
        bool RequestShutdown();
        GameObject GetGameObjectProxy();
        void OnLogic(float delta);

        /// <summary>Tick 顺序，值小的先执行。默认 0。</summary>
        int Order { get; }

        /// <summary>
        /// 持久化模块标记：true 时场景切换（ShutdownModules）不销毁，跨场景生命周期。
        /// </summary>
        bool Persistent { get; }
    }

    /// <summary>
    /// 可选接口：模块实现此接口以在 ModuleMonitorPage 中提供自定义调试信息。
    /// </summary>
    public interface IModuleDebugInfo
    {
        /// <summary>列表中的摘要文字（单行）。</summary>
        string DebugSummary { get; }

        /// <summary>详情面板中的自定义 GUI。仅在 Play Mode 下调用。</summary>
        void OnDrawModuleDebugGUI();
    }

    /// <summary>
    /// 可热插拔功能模块基类（纯 C#，非 MonoBehaviour）。
    /// 通过 KGameCore.RequireModule&lt;T&gt;() 动态装卸。
    ///
    /// 设计约定：
    /// - 模块负责按 Order 顺序 Tick 游戏逻辑，不存储 Unity 序列化数据（配置进 SO/Service）
    /// - 协程使用框架纯 C# CoroutineManager，由 KGameCore.OnLogic 统一驱动
    /// - 需要场景挂点（生成物体 parent）时使用 KGameCore.Instance.ModuleRoot
    /// </summary>
    public abstract class TModule<T> : IModule where T : TModule<T>, new()
    {
        /// <summary>获取模块，不存在则自动创建并注册（RequireModule 语义）。</summary>
        public static T Instance => KGameCore.Instance.RequireModule<T>();

        /// <summary>获取模块，不存在返回 null（不创建）。</summary>
        public static T NullableInstance => KGameCore.Instance.GetModule<T>();

        #region IModule

        private bool _initialized;
        public bool Initialized => _initialized;

        /// <summary>Tick 顺序，值小的先执行。默认 0，覆写调整。</summary>
        public virtual int Order => 0;

        /// <summary>持久化模块：true 时场景切换（ShutdownModules）不销毁，跨场景生命周期。</summary>
        public virtual bool Persistent => false;

        public void Init()
        {
            if (_initialized) return;
            OnModuleInit();
            _initialized = true;
            EnhancedLog.Info("Module", $"{GetType().Name} Init");
        }

        public void Dispose()
        {
            OnModuleDispose();
            EnhancedLog.Info("Module", $"{GetType().Name} Disposed");
        }

        public virtual bool RequestShutdown() => true;

        /// <summary>返回共享场景挂点（KGameCore.ModuleRoot），模块自身无 GameObject。</summary>
        public GameObject GetGameObjectProxy() => KGameCore.Instance.ModuleRoot;

        public void OnLogic(float delta)
        {
            _coroutineHandler.TickFixedUpdate(delta);
            OnModuleLogic(delta);
        }

        #endregion

        #region Virtual overrides

        protected virtual void OnModuleInit() { }
        protected virtual void OnModuleDispose() { }
        protected virtual void OnModuleLogic(float delta) { }

        #endregion

        #region Coroutine

        public KCoroutine ExecCoroutine(IEnumerator routine)
        {
            return _coroutineHandler.StartCoroutine(routine);
        }

        private readonly CoroutineManager _coroutineHandler = new();

        #endregion
    }
}
