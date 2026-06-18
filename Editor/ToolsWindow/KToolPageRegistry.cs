using System;
using System.Collections.Generic;
using UnityEditor;

namespace KFramework.Editor
{
    /// <summary>
    /// 工具页面注册表，自动发现所有实现 IKToolPage 的类型。
    /// 页面元数据（Name/Kit/Priority）直接通过接口属性获取。
    /// </summary>
    public static class KToolPageRegistry
    {
        private static Dictionary<Type, IKToolPage> _instances;
        private static List<PageEntry> _entries;

        public static IReadOnlyList<PageEntry> Pages
        {
            get
            {
                EnsureInitialized();
                return _entries;
            }
        }

        public static IKToolPage GetPage(Type type)
        {
            EnsureInitialized();
            _instances.TryGetValue(type, out var page);
            return page;
        }

        private static void EnsureInitialized()
        {
            if (_instances != null) return;

            _instances = new Dictionary<Type, IKToolPage>();
            _entries = new List<PageEntry>();

            var types = TypeCache.GetTypesDerivedFrom<IKToolPage>();
            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition) continue;

                try
                {
                    var instance = (IKToolPage)Activator.CreateInstance(type);
                    _instances[type] = instance;
                    _entries.Add(new PageEntry
                    {
                        Type = type,
                        Name = instance.PageName ?? type.Name,
                        Kit = instance.Kit ?? "System",
                        Priority = instance.Priority,
                        Instance = instance
                    });
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[KToolPageRegistry] 无法创建页面 {type.Name}: {e.Message}");
                }
            }

            _entries.Sort((a, b) =>
            {
                int kitCompare = string.Compare(a.Kit, b.Kit, StringComparison.Ordinal);
                if (kitCompare != 0) return kitCompare;
                return a.Priority.CompareTo(b.Priority);
            });
        }

        public static void Refresh()
        {
            foreach (var kv in _instances)
                kv.Value?.OnDisable();
            _instances = null;
            _entries = null;
        }

        public sealed class PageEntry
        {
            public Type Type;
            public string Name;
            public string Kit;
            public int Priority;
            public IKToolPage Instance;
        }
    }
}
