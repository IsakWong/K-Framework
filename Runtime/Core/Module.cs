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
    }

    /// <summary>
    /// 可热插拔功能模块基类。
    /// 通过 KGameCore.RequireModule&lt;T&gt;() 动态装卸。
    /// </summary>
    [DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
    public class TModule<T> : MonoBehaviour, IModule where T : MonoBehaviour, IModule
    {
        public static T Instance => KGameCore.Instance.RequireModule<T>();
        public static T NullableInstance => KGameCore.Instance.GetModule<T>();

        #region IModule

        private bool _initialized;
        public bool Initialized => _initialized;

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
        public GameObject GetGameObjectProxy() => gameObject;

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

        #region Unity Lifecycle

        protected void Awake()
        {
            KGameCore.Instance.AddModule(this);
            EnhancedLog.Info("Module", $"{GetType().Name} Awake");
            name = $"[{GetType().Name}]";
        }

        #endregion
    }
}
