
using System.Collections.Generic;
using System;
using UnityEngine;

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
