using System;
using Framework.Coroutine;
using KFramework;
using UnityEngine;

namespace Framework.Foundation
{
    /// <summary>
    /// 跨场景 MonoBehaviour 单例基类。
    ///
    /// 规则：
    ///   - Instance getter：场景里有就用场景的，没有就自动创建一个 bare instance
    ///   - InitializeSingleton：场景版本永远优先——如已有 auto-created bare instance，销毁它，用场景的替换
    ///   - DontDestroyOnLoad：跨场景存活
    ///
    /// 适用于 CanvasInstance、CameraInstance（场景配置）、UIManager、SoundManager（自动创建）等所有跨场景单例。
    /// </summary>
    public class PersistentSingleton<T> : MonoBehaviour, IService where T : Component
    {
        protected static T instance;

        public static bool HasInstance => instance != null;

        public static T TryGetInstance()
        {
            return HasInstance ? instance : null;
        }

        /// <summary>
        /// 获取或自动创建实例。场景里有配置好的就用场景的，没有就 new bare instance。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                    {
                        var go = new GameObject($"[{typeof(T).Name}]");
                        instance = go.AddComponent<T>();
                        DontDestroyOnLoad(go);
                    }
                }

                return instance;
            }
        }

        #region IService

        bool IService.Initialized => _serviceInitialized;
        private bool _serviceInitialized;

        void IService.Init()
        {
            if (_serviceInitialized) return;
            OnServiceInit();
#pragma warning disable CS0618
            OnServiceRegistered();
#pragma warning restore CS0618
            _serviceInitialized = true;
        }

        void IService.Dispose()
        {
            OnServiceDispose();
        }

        #endregion

        protected virtual void OnServiceInit() { }
        protected virtual void OnServiceDispose() { }

        [Obsolete("Use OnServiceInit() instead.")]
        protected virtual void OnServiceRegistered() { }

        private void FixedUpdate()
        {
        }

        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        /// <summary>
        /// 场景版本永远优先。
        /// 如果 Bootstrap 期自动创建了 bare instance，场景加载后场景的配置版本会替换它。
        /// </summary>
        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            transform.SetParent(null);

            if (instance == null || instance == this)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
                ServiceLocator.Register(typeof(T), instance);
                ((IService)this).Init();
            }
            else if (instance != this)
            {
                // 已有实例 → 由子类决定接管策略。
                // 返回 true：销毁旧实例，用本场景版本替换（默认，场景版本优先）。
                // 返回 false：保留旧实例，本场景对象由基类销毁（如 CanvasInstance 合并子物体后退出）。
                if (TryReplaceExisting(instance))
                {
                    instance = this as T;
                    DontDestroyOnLoad(gameObject);
                    ServiceLocator.Register(typeof(T), instance);
                    ((IService)this).Init();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// 已有实例时的接管策略。默认场景版本优先：销毁旧实例，返回 true 由本对象接管。
        /// 子类可覆盖：例如把子物体合并进旧实例后返回 false，让旧实例继续存活。
        /// </summary>
        protected virtual bool TryReplaceExisting(T existing)
        {
            Destroy(existing.gameObject);
            return true;
        }
    }
}
