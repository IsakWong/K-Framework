using KFramework;
using UnityEngine;

public interface IDataContainer
{
    bool GetData<T>(string name, out T t) where T : class;
    bool HasData<T>(string name);

    bool HasKey(string name);
    void SetData<T>(string name, T t);
}


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
                // 也注册到 KGameCore
                if (KGameCore._core != null)
                    KGameCore.Instance.TryRegisterService(instance);
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
