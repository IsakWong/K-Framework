using System;
using System.Collections.Generic;
using DG.Tweening;
using Framework.JsonConverter;
using KFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;
using QuaternionConverter = Newtonsoft.Json.UnityConverters.Math.QuaternionConverter;
using Vector2Converter = Newtonsoft.Json.UnityConverters.Math.Vector2Converter;
using Vector3Converter = Newtonsoft.Json.UnityConverters.Math.Vector3Converter;

/// <summary>
/// 游戏核心 — 服务注册 + 模块管理 + GameMode 调度。
///
/// _services — IService（全局永久服务），OnRegisterServices() 注册，批量 Init。
/// _modules  — IModule（动态装卸模块），RequireModule/AddModule 管理。
///
/// 使用方式：
///   public class MyGameCore : KGameCore
///   {
///       protected override void OnRegisterServices()
///       {
///           RegisterService(new AssetManager());
///       }
///   }
///   KGameCore.Bootstrap&lt;MyGameCore&gt;();
///
/// 不继承则使用默认 KGameCore。
/// </summary>
public class KGameCore
{
    // ═══════════════════════════════════════════════════════════════
    //  Static
    // ═══════════════════════════════════════════════════════════════

    internal static KGameCore _core;

    public static KGameCore Instance
    {
        get
        {
            if (_core == null)
            {
                _core = new KGameCore();
                _core.Initialize();
            }
            return _core;
        }
    }

    public static T Bootstrap<T>() where T : KGameCore, new()
    {
        if (_core != null)
            throw new InvalidOperationException("KGameCore already initialized");
        _core = new T();
        _core.Initialize();
        return (T)_core;
    }

    public static KTimerManager GlobalTimers => Instance.Timers;

    public static T RequireSystem<T>() where T : MonoBehaviour, IModule
        => Instance.RequireModule<T>();

    public static T GetSystem<T>() where T : MonoBehaviour, IModule
        => Instance.GetModule<T>();

    [Obsolete("Use GetSystem<T>() instead")]
    public static T SystemAt<T>() where T : MonoBehaviour, IModule
        => Instance.GetModule<T>();

    // ═══════════════════════════════════════════════════════════════
    //  Service Registry（全局永久服务 IService）
    // ═══════════════════════════════════════════════════════════════

    private readonly Dictionary<Type, IService> _services = new();

    public int ServiceCount => _services.Count;

    protected void RegisterService<T>(T service) where T : IService
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        var key = typeof(T);
        if (_services.ContainsKey(key))
            _services[key].Dispose();
        _services[key] = service;
    }

    public T GetService<T>() where T : class, IService
    {
        return _services.TryGetValue(typeof(T), out var svc) ? svc as T : null;
    }

    public bool HasService<T>() where T : IService
    {
        return _services.ContainsKey(typeof(T));
    }

    /// <summary>由 KSingleton 自动调用，懒注册到 KGameCore。</summary>
    internal void TryRegisterService<T>(T service) where T : IService
    {
        if (service == null) return;
        var key = typeof(T);
        if (!_services.ContainsKey(key))
            _services[key] = service;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Module Registry（动态装卸模块 IModule）
    // ═══════════════════════════════════════════════════════════════

    private readonly Dictionary<Type, IModule> _modules = new();

    public int ModuleCount => _modules.Count;

    public T GetModule<T>() where T : class, IModule
    {
        return _modules.TryGetValue(typeof(T), out var m) ? m as T : null;
    }

    [Obsolete("Use GetModule<T>() instead.")]
    public IModule GetModule(string name)
    {
        foreach (var kv in _modules)
            if (kv.Key.Name == name) return kv.Value;
        return null;
    }

    public T RequireModule<T>(string name = null) where T : MonoBehaviour, IModule
    {
        if (GetModule<T>() is T existing) return existing;
        if (name == null) name = typeof(T).Name;

        var count = proxy.gameObject.transform.childCount;
        for (var i = 0; i < count; i++)
        {
            var inst = proxy.gameObject.transform.GetChild(i).GetComponent<T>();
            if (inst != null)
            {
                _modules[typeof(T)] = inst;
                if (!inst.Initialized) inst.Init();
                return inst;
            }
        }

        var go = new GameObject($"[{name}]");
        var newInst = go.AddComponent<T>();
        if (newInst == null) { UnityEngine.Object.Destroy(go); return null; }
        if (proxy.transform.parent != null)
            newInst.transform.SetParent(proxy.transform.parent);

        _modules[typeof(T)] = newInst;
        if (!newInst.Initialized) newInst.Init();
        return newInst;
    }

    public IModule AddModule(IModule module)
    {
        if (module == null) throw new ArgumentNullException(nameof(module));
        var key = module.GetType();
        if (_modules.ContainsKey(key))
        {
            Debug.LogWarning($"[KGameCore] Module already registered: {key.Name}, replacing.");
            _modules[key].Dispose();
        }
        _modules[key] = module;
        if (!module.Initialized) module.Init();
        return module;
    }

    internal void GetAllModules(Queue<IModule> outQueue)
    {
        foreach (var m in _modules.Values) outQueue.Enqueue(m);
    }

    internal void ClearModules() => _modules.Clear();

    // ═══════════════════════════════════════════════════════════════
    //  Legacy
    // ═══════════════════════════════════════════════════════════════

    [Obsolete("Use GetModule<T>() instead.")]
    public Dictionary<string, IModule> Modules
    {
        get
        {
            var dict = new Dictionary<string, IModule>();
            foreach (var kv in _modules) dict[kv.Key.Name] = kv.Value;
            return dict;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Initialization
    // ═══════════════════════════════════════════════════════════════

    protected KGameCore() { StaticInit(); }

    /// <summary>游戏业务覆写，在其中 RegisterService 注册全局服务。</summary>
    protected virtual void OnRegisterServices() { }

    private void Initialize()
    {
        if (proxy == null)
        {
            var go = new GameObject();
            proxy = go.AddComponent<GameCoreProxy>();
            go.name = "[GameCoreProxy]";
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

#if !UNITY_EDITOR
        StaticInit();
#endif

        Debug.Log("[KGameCore] Initializing...");
        DOTween.Init();

        OnRegisterServices();

        foreach (var kv in _services)
            if (!kv.Value.Initialized) kv.Value.Init();

        Debug.Log($"[KGameCore] Initialized ({_services.Count} services)");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Shutdown
    // ═══════════════════════════════════════════════════════════════

    public void DisposeAllServices()
    {
        foreach (var kv in _services) kv.Value.Dispose();
        _services.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  GameMode
    // ═══════════════════════════════════════════════════════════════

    public KSignal<GameMode> OnAnyModeBegin = new();
    public KSignal<GameMode> OnAnyModeStart = new();
    public KSignal<GameMode> OnAnyModeEnd = new();
    public KSignal<GameMode, GameMode> OnModeSwitch = new();

    public GameMode CurrentGameMode;

    public void SwitchGameMode(GameMode value)
    {
        if (CurrentGameMode == value) return;

        if (CurrentGameMode)
        {
            CurrentGameMode.OnSwitchGameMode(value);
            CurrentGameMode.OnModeEnd();
            OnAnyModeEnd?.Invoke(CurrentGameMode);
            CurrentGameMode.gameObject.SetActive(false);
        }

        if (value) value.gameObject.SetActive(true);

        CurrentGameMode = value;
        if (CurrentGameMode)
        {
            CurrentGameMode.OnModeAwake();
            OnAnyModeBegin?.Invoke(CurrentGameMode);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Proxy & Logic
    // ═══════════════════════════════════════════════════════════════

    public GameCoreProxy proxy;
    public void SetProxy(GameCoreProxy p) => proxy = p;
    public KTimerManager Timers = new();

    public void OnLogic()
    {
        KTime.scaleDeltaTime = Time.fixedDeltaTime;
        Timers.OnLogic(KTime.scaleDeltaTime);
        foreach (var m in _modules.Values)
            m.OnLogic(Time.fixedDeltaTime);
    }

    // ═══════════════════════════════════════════════════════════════
    //  JSON static init
    // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void StaticInit()
    {
        JsonConvert.DefaultSettings = () =>
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>(),
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector4Converter());
            settings.Converters.Add(new Color32Converter());
            settings.Converters.Add(new QuaternionConverter());
            settings.Converters.Add(new AssetReferenceConverter());
            settings.Converters.Add(new AssetReferenceTConverter());
            return settings;
        };
    }

    [Obsolete("Use KGameCore.GetSystem<T>() or GetModule<T>() instead")]
    public T GetSystemInstance<T>() where T : MonoBehaviour, IModule
        => GetModule<T>();
}
