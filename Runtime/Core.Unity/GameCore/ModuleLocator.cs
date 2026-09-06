using System;
using System.Collections;
using System.Collections.Generic;
using KFramework;
using UnityEngine;

/// <summary>
/// [Obsolete] 场景级模块定位器。
/// 在 TModule 纯 C# 化后与 KGameCore 模块注册表（Order/持久化标记）职责冗余，
/// 且无任何调用点。统一走 KGameCore.RequireModule/GetModule，不再使用本类。
/// </summary>
[Obsolete("Use KGameCore.RequireModule<T>() / GetModule<T>() instead.")]
public static class ModuleLocator
{
    private static readonly Dictionary<Type, IService> _modules = new();

    public static int Count => _modules.Count;

    // ═══════════════════════════════════════════════════════════════
    //  注册
    // ═══════════════════════════════════════════════════════════════

    public static void Add(IService module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        var key = module.GetType();
        if (_modules.ContainsKey(key))
        {
            Debug.LogWarning($"[ModuleLocator] Module already registered: {key.Name}, replacing.");
            _modules[key].Dispose();
        }
        _modules[key] = module;
        if (!module.Initialized) module.Init();
    }

    internal static void Clear() => _modules.Clear();

    // ═══════════════════════════════════════════════════════════════
    //  获取
    // ═══════════════════════════════════════════════════════════════

    public static T Get<T>() where T : class, IService
    {
        return _modules.TryGetValue(typeof(T), out var m) ? m as T : null;
    }

    public static T Require<T>(string name = null) where T : MonoBehaviour, IService
    {
        if (Get<T>() is T existing) return existing;
        if (name == null) name = typeof(T).Name;

        var proxy = KGameCore.Instance.proxy;
        if (proxy != null)
        {
            var count = proxy.transform.childCount;
            for (var i = 0; i < count; i++)
            {
                var inst = proxy.transform.GetChild(i).GetComponent<T>();
                if (inst != null)
                {
                    _modules[typeof(T)] = inst;
                    if (!inst.Initialized) inst.Init();
                    return inst;
                }
            }
        }

        var go = new GameObject($"[{name}]");
        var newInst = go.AddComponent<T>();
        if (newInst == null) { UnityEngine.Object.Destroy(go); return null; }
        if (proxy != null && proxy.transform.parent != null)
            newInst.transform.SetParent(proxy.transform.parent);

        _modules[typeof(T)] = newInst;
        if (!newInst.Initialized) newInst.Init();
        return newInst;
    }

    // ═══════════════════════════════════════════════════════════════
    //  场景切换 — Shutdown
    // ═══════════════════════════════════════════════════════════════

    /// <summary>场景切换时销毁所有模块，返回迭代器供协程使用</summary>
    [Obsolete("模块统一走 KGameCore，本类已废弃。")]
    public static IEnumerator ShutdownAll()
    {
        EnhancedLog.Log("[ModuleLocator] Shutting down all modules...");
        var queue = new Queue<IService>(_modules.Values);

        while (queue.Count > 0)
        {
            var first = queue.Dequeue();
            first.Dispose();
            yield return new WaitForFixedUpdate();
        }

        _modules.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Logic
    // ═══════════════════════════════════════════════════════════════

    [Obsolete("模块统一走 KGameCore，本类已废弃。")]
    public static void OnLogic(float delta)
    {
        // 模块 Tick 统一由 KGameCore.OnLogic 按 Order 驱动，本类不再负责
    }
}
