using System;
using Framework.Coroutine;
using KFramework;
using UnityEngine;

namespace Framework.Foundation
{
    /// <summary>
    /// MonoBehaviour 单例基类 — 实现 IService，与 KSingleton 统一生命周期。
    ///
    /// 生命周期：
    ///   Awake → ServiceLocator.Register → ((IService)this).Init() → OnServiceInit()
    ///
    /// 子类覆写 OnServiceInit() 做初始化（注册接口类型等）。
    /// 旧代码 OnServiceRegistered() 仍可用，标记为 Obsolete。
    /// </summary>
    public class PersistentSingleton<T> : MonoBehaviour, IService where T : Component
    {
        public bool AutoUnparentOnAwake = true;

        protected static T instance;

        public static bool HasInstance => instance != null;

        public static T TryGetInstance()
        {
            return HasInstance ? instance : null;
        }

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

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (AutoUnparentOnAwake)
            {
                transform.SetParent(null);
            }

            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
                // 注册到 ServiceLocator 并立即初始化（MonoBehaviour 不走 KGameCore 批量 Init）
                ServiceLocator.Register(typeof(T), instance);
                ((IService)this).Init();
            }
            else
            {
                if (instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}