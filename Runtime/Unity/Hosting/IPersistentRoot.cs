using System.Collections.Generic;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 跨场景根对象：模块按需注册自己的子根，框架统一管理 DontDestroyOnLoad。
    /// 不需要挂点的模块完全不接触这个接口。
    /// </summary>
    public interface IPersistentRoot
    {
        /// <summary>服务自身的根对象。</summary>
        GameObject Root { get; }

        /// <summary>取或创建同名子根（幂等）。</summary>
        GameObject GetOrCreate(string name);

        /// <summary>销毁并移除同名子根；只应由注册它的人调用。</summary>
        void Release(string name);
    }

    /// <summary>
    /// <see cref="IPersistentRoot"/> 的默认实现：服务自身的 GameObject 即根，
    /// 由 <see cref="PersistentSingleton{T}"/> 保证跨场景存活。
    /// </summary>
    public class PersistentRootService : PersistentSingleton<PersistentRootService>, IPersistentRoot
    {
        private readonly Dictionary<string, GameObject> _roots = new();

        public GameObject Root => gameObject;

        protected override void OnServiceInit()
        {
            base.OnServiceInit();
            ServiceLocator.Register<IPersistentRoot>(this);
        }

        /// <summary>场景里意外配置的实例不替换已有根，避免把已注册的子根一起销毁。</summary>
        protected override bool TryReplaceExisting(PersistentRootService existing) => false;

        public GameObject GetOrCreate(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Root";

            if (_roots.TryGetValue(name, out var existing) && existing != null)
                return existing;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            _roots[name] = go;
            return go;
        }

        public void Release(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (!_roots.TryGetValue(name, out var go))
                return;

            _roots.Remove(name);
            if (go != null)
                Destroy(go);
        }
    }
}
