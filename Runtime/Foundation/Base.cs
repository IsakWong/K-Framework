using UnityEngine;

public interface IDataContainer
{
    bool GetData<T>(string name, out T t) where T : class;
    bool HasData<T>(string name);

    bool HasKey(string name);
    void SetData<T>(string name, T t);
}


/// <summary>
/// 纯 C# 单例基类，不依赖 MonoBehaviour。
/// 无法动态卸载，常用于 Log、AssetManager、EventBus 等。
/// 首次访问 Instance 时自动注册到 ServiceLocator。
/// 子类可 override OnServiceRegistered() 来注册接口类型。
/// </summary>
/// <typeparam name="T"></typeparam>
public class KSingleton<T> where T : KSingleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
                // 自动注册具体类型到 ServiceLocator
                ServiceLocator.Register(typeof(T), instance);
                instance.OnServiceRegistered();
            }

            return instance;
        }
    }

    /// <summary>
    /// 在单例创建并注册到 ServiceLocator 后调用。
    /// 子类 override 此方法来注册接口类型，例如：
    /// <code>ServiceLocator.Register&lt;IAssetService&gt;(this);</code>
    /// </summary>
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