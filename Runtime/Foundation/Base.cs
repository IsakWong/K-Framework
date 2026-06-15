using KFramework;
using UnityEngine;


/// <summary>
/// 纯 C# 单例基类 — 不依赖 MonoBehaviour，实现 IService 接口。
///
/// 生命周期：
///   首次访问 Instance → new T() → 注册到 ServiceLocator + KGameCore → Init()
///
/// 子类覆写 OnServiceInit() 做初始化，OnServiceDispose() 做清理。
/// 旧代码 OnServiceRegistered() 仍可用，标记为 Obsolete。
/// </summary>
public class KSingleton<T> : IService where T : KSingleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
                ServiceLocator.Register(typeof(T), instance);
                var core = KGameCore._core;
                if (core != null)
                    core.TryRegisterService(instance);
                // 批量注册阶段不 Init，由 KGameCore 统一调用
                if (core == null || !core._batchRegistering)
                    ((IService)instance).Init();
            }
            return instance;
        }
    }

    #region IService

    bool IService.Initialized => _initialized;
    private bool _initialized;

    void IService.Init()
    {
        if (_initialized) return;
        OnServiceInit();
#pragma warning disable CS0618
        OnServiceRegistered();
#pragma warning restore CS0618
        _initialized = true;
    }

    void IService.Dispose()
    {
        OnServiceDispose();
    }

    #endregion

    protected virtual void OnServiceInit() { }
    protected virtual void OnServiceDispose() { }

    [System.Obsolete("Use OnServiceInit() instead.")]
    protected virtual void OnServiceRegistered() { }

    protected KSingleton()
    {
        Debug.Assert(instance == null, "This is a singleton class, should not be created twice!!!");
    }
}


public class ShowNonSerializedPropertyAttribute : System.Attribute
{
    public string Label;
    public string Tooltip;
    public bool Readonly;

    public ShowNonSerializedPropertyAttribute(string label)
    {
        Label = label;
    }
}
