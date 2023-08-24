using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// 这是一个功能模块，该模块可以动态装卸，比如Gameplay，局外等System，对局管理
/// </summary>
/// <typeparam name="T"></typeparam>
public class KModule : MonoBehaviour
{
    protected EventDispatcher<string> _systemDispatcher = new EventDispatcher<string>();

    public EventDispatcher<string> Dispatcher
    {
        get => _systemDispatcher;
    }

    protected void Awake()
    {
        // DontDestroyOnLoad(this);
        KGameCore.Instance.AddModule(this);
        name = $"[{GetType().Name}]";
    }


    protected virtual void Update()
    {
    }

    public virtual void OnInit()
    {
    }

    public virtual void OnShutdown()
    {
    }

    public T GetData<T>(string name)
    {
        throw new System.NotImplementedException();
    }
}