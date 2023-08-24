using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Framework.Config
{
    public class ConfigObject : ScriptableObject
    {
    }

    public class ConfigModule : KModule
    {
        //public SerializedDictionary<SerializeType<ConfigObject>, ConfigObject> Configs;
        public SerializedDictionary<string, ConfigObject> AllConfigs;

        public T GetConfig<T>() where T : ConfigObject
        {
            return AllConfigs[typeof(T).Name] as T;
        }

        public override void OnInit()
        {
            base.OnInit();
        }
    }
}