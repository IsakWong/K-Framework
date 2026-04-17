using System;
using Framework.Coroutine;
using UnityEngine;

namespace Framework.Foundation
{
    public class PersistentSingleton<T> : MonoBehaviour where T : Component
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
                // 自动注册具体类型到 ServiceLocator
                ServiceLocator.Register(typeof(T), instance);
                OnServiceRegistered();
            }
            else
            {
                if (instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// 在单例初始化并注册到 ServiceLocator 后调用。
        /// 子类 override 此方法来注册接口类型，例如：
        /// <code>ServiceLocator.Register&lt;IUIService&gt;(this);</code>
        /// </summary>
        protected virtual void OnServiceRegistered() { }
    }
}