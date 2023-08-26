using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataContainer
{
    bool GetData<T>(string name, out T t) where T : class;
    bool HasData<T>(string name);

    bool HasKey(string name);
    void SetData<T>(string name, T t);

}
/// <summary>
/// 这是一个单例类，不同于KSystem可以装卸，是无法动态卸载的，常用于 Log，广播等
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
            }
            return instance;
        }
    }

    protected KSingleton()
    {
        // Do nothing
    }
}
/// <summary>
/// 这是一个功能模块，该模块可以动态装卸，比如Gameplay，局外等System，对局管理，场景加载
/// </summary>
/// <typeparam name="T"></typeparam>
public class KSystem : MonoBehaviour
{

    protected EventDispatcher<string> _systemDispatcher = new EventDispatcher<string>();
    public EventDispatcher<string> Dispatcher
    {
        get => _systemDispatcher;
    }

    public static GameObject GetSystemPrefabAsset<T>() where T : KSystem
    {
        var asset = AssetSystem.Instance.LoadAsset<GameObject>(String.Format("Assets/Gameplay/Framework/[{0}].prefab", typeof(T).Name));
        return asset;
    }

    private Dictionary<string, Variable> mVars = new Dictionary<string, Variable>();

    protected void Awake()
    {
        DontDestroyOnLoad(this);
        KGameCore.Instance.AddSystem(this);
    }


    protected virtual void Update()
    {
    }

    protected virtual void Shutdown()
    {
    }

    public T GetData<T>(string name)
    {
        throw new System.NotImplementedException();
    }
}

public class CoreBehaviour : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        var instace = KGameCore.Instance;
    }
}