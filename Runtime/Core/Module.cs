using System.Collections.Generic;
using System;
using System.Collections;
using Framework.Coroutine;
using UnityEngine;

public interface IModule
{
    void OnInit();
    bool RequestShutdown();
    void OnShutdown();

    GameObject GetGameObjectProxy();
    
    void OnLogic(float delta);
}

/// <summary>
/// 这是一个功能模块，该模块可以动态装卸，比如Gameplay，局外等System，对局管理
/// </summary>
/// <typeparam name="T"></typeparam>
[DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
public class TModule<T> : MonoBehaviour, IModule  where T : MonoBehaviour, IModule
{
    public static T Instance
    {
        get => KGameCore.Instance.RequireModule<T>();
    }
    public static T NullablInstance
    {
        get => KGameCore.Instance.GetModule<T>();
    }
    
    protected void Awake()
    {
        KGameCore.Instance.AddModule(this);
        EnhancedLog.Info("Module", $"{GetType().Name} Awake");
        name = $"[{GetType().Name}]";
    }

    public virtual void OnInit()
    {
        EnhancedLog.Info("Module", $"{GetType().Name} Init");
    }

    public GameObject GetGameObjectProxy()
    {
        return gameObject;
    }

    public virtual bool RequestShutdown()
    {
        return true;
    }

    public void OnLogic(float delta)
    {
        _coroutineHandler.TickFixedUpdate(delta);
        OnModuleLogic(delta);
    }
    
    /// <summary>
    /// 模块逻辑更新，子类可重写以实现每帧逻辑
    /// </summary>
    protected virtual void OnModuleLogic(float delta)
    {
    }
    
    public KCoroutine ExecCoroutine(IEnumerator routine)
    {
        return _coroutineHandler.StartCoroutine(routine);
    }
    
    private CoroutineManager _coroutineHandler = new();

    public virtual void OnShutdown()
    {
        EnhancedLog.Info("Module", $"{GetType().Name} Shutdown");
    }
}