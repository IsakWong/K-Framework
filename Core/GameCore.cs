using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.UnityConverters.Math;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuaternionConverter = Newtonsoft.Json.UnityConverters.Math.QuaternionConverter;
using Vector2Converter = Newtonsoft.Json.UnityConverters.Math.Vector2Converter;
using Vector3Converter = Newtonsoft.Json.UnityConverters.Math.Vector3Converter;

public class KGameCore
{
    private static KGameCore _core;
    public GameCoreProxy proxy;


    public GameMode CurrentGameMode;

    public static KGameCore Instance
    {
        get
        {
            if (_core == null)
            {
                _core = new KGameCore();
                _core.Init();
            }

            return _core;
        }
    }

    public static KTimerManager GlobalTimers
    {
        get { return _core.Timers; }
    }

    public static T RequireSystem<T>() where T : KModule
    {
        return _core.RequireModule<T>();
    }

    public static T SystemAt<T>() where T : KModule
    {
        return _core.Modules[typeof(T).Name] as T;
    }

    public void SwitchGameMode(GameMode value)
    {
        if (CurrentGameMode == value)
            return;
        if (CurrentGameMode)
        {
            CurrentGameMode.OnSwitchGameMode(value);
            CurrentGameMode.OnModeEnd();
            CurrentGameMode.gameObject.SetActive(false);
        }

        value.gameObject.SetActive(true);
        Instance.CurrentGameMode = value;
        if (Instance.CurrentGameMode)
            Instance.CurrentGameMode.OnModeBegin();
    }

    public KModule AddModule(KModule module)
    {
        var name = module.GetType().Name;
        module.OnInit();
        Modules[name] = module;
        return module;
    }

    public T GetModule<T>() where T : KModule
    {
        string name = typeof(T).Name;
        if (Modules.ContainsKey(name))
            return Modules[name] as T;
        return null;
    }

    public KModule GetModule(string name)
    {
        return Modules[name];
    }

    public T RequireModule<T>(string name = null) where T : KModule
    {
        if (name == null)
            name = typeof(T).Name;
        if (Modules.ContainsKey(name))
        {
            return Modules[name] as T;
        }

        var count = proxy.gameObject.transform.childCount;
        T inst = null;
        for (int i = 0; i < count; i++)
        {
            var it = proxy.gameObject.transform.GetChild(i);
            inst = it.gameObject.GetComponent<T>();
            if (inst is not null)
                break;
        }

        var modulePrefab = proxy.ModulePrefab[name];
        inst ??= GameObject.Instantiate(modulePrefab).GetComponent<T>();
        if (inst is not null)
        {
            Modules[name] = inst;
            return inst;
        }

        if (proxy.transform.parent != null)
        {
            inst.transform.SetParent(proxy.transform.parent);
        }

        return Modules[name] as T;
    }

    public T GetSystem<T>() where T : KModule
    {
        return Modules[typeof(T).Name] as T;
    }

    public void SetProxy(GameCoreProxy proxy)
    {
        this.proxy = proxy;
    }

    private KGameCore()
    {
        StaticInit();
    }

    public KTimerManager Timers = new KTimerManager();

    public void OnLogic()
    {
        Timers.OnLogic();
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    static void StaticInit()
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
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new Vector4Converter());
            settings.Converters.Add(new Color32Converter());
            settings.Converters.Add(new QuaternionConverter());
            return settings;
        };
    }

    void Init()
    {
#if !UNITY_EDITOR
        Debug.Log("Static initialized");
        StaticInit();
#endif
        Debug.Log("KGameCore initialized");
        DOTween.Init();
        Debug.Log("DOTween initialized");
        Modules = new Dictionary<string, KModule>();
        Debug.Log("Modules config initialized");
    }

    private Dictionary<string, KModule> Modules;
}