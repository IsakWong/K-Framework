using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 服务定位器 — 框架级服务注册与发现中心
///
/// 替代散落的单例直接引用，提供统一的服务访问入口。
/// 支持接口隔离，便于单元测试和模块替换。
///
/// 与 Singleton 的关系：
///   KSingleton / PersistentSingleton 在首次创建时自动注册到 ServiceLocator。
///   各 Manager 通过 OnServiceRegistered() 额外注册自己的接口类型。
///   旧代码的 .Instance 访问方式仍然可用（向后兼容）。
/// </summary>
/// <example>
/// // 注册（由单例基类自动完成，也可手动注册 / 覆盖）
/// ServiceLocator.Register&lt;IAssetService&gt;(assetManager);
///
/// // 获取
/// var assets = ServiceLocator.Get&lt;IAssetService&gt;();
///
/// // 测试中替换实现
/// ServiceLocator.Register&lt;IAssetService&gt;(new MockAssetService());
///
/// // 安全获取（不抛异常）
/// if (ServiceLocator.TryGet&lt;IAssetService&gt;(out var svc)) { ... }
/// </example>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    // ═══════════════════════════════════════════════════════════════
    //  注册
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 注册服务实例（按泛型类型作为 Key）
    /// 如果同类型已注册，将覆盖并输出警告
    /// </summary>
    public static void Register<T>(T service) where T : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var type = typeof(T);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] 覆盖已注册服务: {type.Name}");
        }
#endif
        _services[type] = service;
    }

    /// <summary>
    /// 注册服务实例（按运行时 Type 作为 Key）
    /// 用于基类自动注册具体类型
    /// </summary>
    public static void Register(Type serviceType, object service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_services.ContainsKey(serviceType))
        {
            Debug.LogWarning($"[ServiceLocator] 覆盖已注册服务: {serviceType.Name}");
        }
#endif
        _services[serviceType] = service;
    }

    // ═══════════════════════════════════════════════════════════════
    //  获取
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取服务，未注册则抛出 InvalidOperationException
    /// </summary>
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new InvalidOperationException(
            $"[ServiceLocator] 服务未注册: {typeof(T).Name}. " +
            $"请确保服务已初始化，或手动调用 ServiceLocator.Register<{typeof(T).Name}>()");
    }

    /// <summary>
    /// 尝试获取服务（不抛异常）
    /// </summary>
    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var s))
        {
            service = (T)s;
            return true;
        }

        service = null;
        return false;
    }

    /// <summary>
    /// 获取服务，未注册返回 null
    /// </summary>
    public static T GetOrDefault<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var s) ? (T)s : null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  查询
    // ═══════════════════════════════════════════════════════════════

    /// <summary>是否已注册某服务类型</summary>
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>是否已注册某服务类型（运行时 Type）</summary>
    public static bool IsRegistered(Type type)
    {
        return _services.ContainsKey(type);
    }

    /// <summary>当前已注册的服务数量</summary>
    public static int Count => _services.Count;

    // ═══════════════════════════════════════════════════════════════
    //  注销
    // ═══════════════════════════════════════════════════════════════

    /// <summary>注销服务</summary>
    public static bool Unregister<T>() where T : class
    {
        return _services.Remove(typeof(T));
    }

    /// <summary>注销服务（运行时 Type）</summary>
    public static bool Unregister(Type type)
    {
        return _services.Remove(type);
    }

    /// <summary>
    /// 清除所有注册（通常用于测试 teardown 或场景完全重置）
    /// </summary>
    public static void Reset()
    {
        _services.Clear();
    }
}
