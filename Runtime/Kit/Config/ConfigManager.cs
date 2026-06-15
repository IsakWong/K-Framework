using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Framework.Config
{
    public class ConfigObject : ScriptableObject
    {
    }

    public class ConfigManager : KSingleton<ConfigManager>, IConfigService
    {
        /// <summary>
        /// 全局配置文件位置
        /// </summary>
        public static string ConfigPrefix = "Assets/Config/";

        public SerializedDictionary<string, ConfigObject> AllConfigs = new();

        protected override void OnServiceRegistered()
        {
            ServiceLocator.Register<IConfigService>(this);
        }

        public T GetConfig<T>(string name="") where T : ConfigObject
        {
            if (String.IsNullOrEmpty(name))
                name = (typeof(T).Name);
            if (!AllConfigs.ContainsKey(name))
            {
                var asset = AssetManager.Instance.LoadAsset<T>($"{ConfigPrefix}{name}.asset");
                AllConfigs[name] = asset;
                return asset;
            }
            return AllConfigs[name] as T;
        }
    }
}