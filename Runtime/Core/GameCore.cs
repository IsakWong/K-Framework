using System;
using System.Collections.Generic;
using DG.Tweening;
using Framework.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;
using QuaternionConverter = Newtonsoft.Json.UnityConverters.Math.QuaternionConverter;
using Vector2Converter = Newtonsoft.Json.UnityConverters.Math.Vector2Converter;
using Vector3Converter = Newtonsoft.Json.UnityConverters.Math.Vector3Converter;

public class KGameCore
{
    private static KGameCore _core;
    public GameCoreProxy proxy;


    public KSignal<GameMode> OnAnyModeBegin = new();
    public KSignal<GameMode> OnAnyModeStart = new();
    public KSignal<GameMode> OnAnyModeEnd = new ();
    public KSignal<GameMode, GameMode> OnModeSwitch= new();

    public GameMode CurrentGameMode;

    public static KGameCore Instance
    {
        get
        {
            if (_core == null)
            {
                _core = new KGameCore();
                if (_core.proxy == null)
                {
                    GameObject go = new GameObject();
                    var proxy = go.AddComponent<GameCoreProxy>();
                    go.name = "[GameCoreProxy]";
                    GameObject.DontDestroyOnLoad(go);
                    _core.SetProxy(proxy);
                }
                _core.Init();
            }

            return _core;
        }
    }

    public static KTimerManager GlobalTimers => _core.Timers;

    /// <summary>
    /// 静态方法：获取或创建Module（如果不存在会自动创建）
    /// </summary>
    public static T RequireSystem<T>() where T : MonoBehaviour, IModule
    {
        return _core.RequireModule<T>();
    }

    /// <summary>
    /// 静态方法：获取Module（不存在返回null）
    /// </summary>
    public static T GetSystem<T>() where T : MonoBehaviour, IModule
    {
        return _core.GetModule<T>();
    }

    [Obsolete("Use GetSystem<T>() instead")]
    public static T SystemAt<T>() where T : MonoBehaviour, IModule
    {
        return _core.GetModule<T>();
    }

    public void SwitchGameMode(GameMode value)
    {
        if (CurrentGameMode == value)
        {
            return;
        }

        if (CurrentGameMode)
        {
            CurrentGameMode.OnSwitchGameMode(value);
            CurrentGameMode.OnModeEnd();
            OnAnyModeEnd?.Invoke(CurrentGameMode);
            CurrentGameMode.gameObject.SetActive(false);
        }

        if (value)
        {
            value.gameObject.SetActive(true);
        }

        Instance.CurrentGameMode = value;
        if (Instance.CurrentGameMode)
        {
            Instance.CurrentGameMode.OnModeAwake();
            OnAnyModeBegin?.Invoke(CurrentGameMode);
        }
    }

    public IModule AddModule(IModule module)
    {
        var name = module.GetType().Name;
        module.OnInit();
        Modules[name] = module;
        return module;
    }

    /// <summary>
    /// 获取Module（不存在返回null）
    /// </summary>
    public T GetModule<T>() where T : class, IModule
    {
        var name = typeof(T).Name;
        if (Modules.ContainsKey(name))
        {
            return Modules[name] as T;
        }

        return null;
    }

    /// <summary>
    /// 通过名称获取Module
    /// </summary>
    public IModule GetModule(string name)
    {
        if (Modules.TryGetValue(name, out var module))
        {
            return module;
        }
        return null;
    }

    public T RequireModule<T>(string name = null) where T : MonoBehaviour, IModule
    {
        if (name == null)
        {
            name = typeof(T).Name;
        }

        if (Modules.ContainsKey(name))
        {
            return Modules[name] as T;
        }

        var count = proxy.gameObject.transform.childCount;
        T inst = null;
        for (var i = 0; i < count; i++)
        {
            var it = proxy.gameObject.transform.GetChild(i);
            inst = it.gameObject.GetComponent<T>();
            if (inst is not null)
            {
                break;
            }
        }

        if (inst is not null)
        {
            Modules[name] = inst;
            return inst;
        }

        var GO = new GameObject($"[{name}]");
        inst = GO.AddComponent<T>();

        if (inst is null)
        {
            Debug.LogError($"[KGameCore] Failed to create module: {name}");
            UnityEngine.Object.Destroy(GO);
            return null;
        }

        if (proxy.transform.parent != null)
        {
            inst.transform.SetParent(proxy.transform.parent);
        }

        Modules[name] = inst;
        return inst;
    }

    [Obsolete("Use KGameCore.GetSystem<T>() or GetModule<T>() instead")]
    public T GetSystemInstance<T>() where T : MonoBehaviour, IModule
    {
        return GetModule<T>();
    }

    public void SetProxy(GameCoreProxy proxy)
    {
        this.proxy = proxy;
    }

    private KGameCore()
    {
        StaticInit();
    }

    public KTimerManager Timers = new();

    public void OnLogic()
    {
        KTime.scaleDeltaTime = Time.fixedDeltaTime;
        Timers.OnLogic(KTime.scaleDeltaTime);
        foreach (var module in Modules)
        {
            module.Value.OnLogic(Time.fixedDeltaTime);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void StaticInit()
    {
        //初始化一些Unity3D特有的Converter
        JsonConvert.DefaultSettings = () =>
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>(),
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            // Unity 基础类型转换器
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector4Converter());
            settings.Converters.Add(new Color32Converter());
            settings.Converters.Add(new QuaternionConverter());
            
            // Addressables 类型转换器
            settings.Converters.Add(new AssetReferenceConverter());
            settings.Converters.Add(new AssetReferenceTConverter());
            
            return settings;
        };
    }

    

    private void Init()
    {
#if !UNITY_EDITOR
        Debug.Log("Static initialized");
        StaticInit();
#endif

        Debug.Log("KGameCore initialized");
        DOTween.Init();
        Debug.Log("DOTween initialized");
        Modules = new Dictionary<string, IModule>();
        Debug.Log("Modules config initialized");
    }

    public Dictionary<string, IModule> Modules;
}