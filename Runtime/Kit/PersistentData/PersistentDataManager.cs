using AYellowpaper.SerializedCollections;
using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectPersistentData
{
    public string Guid;
    public string AddressablePath;
    public string Data;
}

[Serializable]
public class ScenePersistentData
{
    public string SceneAddressablePath;
    public List<ObjectPersistentData> ObjectDatas = new ();
}

public class PersistentDataManager : KSingleton<PersistentDataManager>, IPersistentDataService
{
    /// <summary>
    /// 是否启用Base64加密（默认true）
    /// </summary>
    private bool enableEncryption = false;

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IPersistentDataService>(this);
    }

    /// <summary>
    /// 将字符串转换为Base64编码
    /// </summary>
    private string EncodeToBase64(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    /// <summary>
    /// 将Base64编码转换为字符串
    /// </summary>
    private string DecodeFromBase64(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    //TODO
    public IEnumerator LoadPersistenSceneData(ScenePersistentData sceneData)
    {

        IPersistent[] persistents = GameObject.FindObjectsOfType<MonoBehaviour>(true).OfType<IPersistent>().ToArray();
        Dictionary<string, IPersistent> persistentDict = new();
        foreach (IPersistent persistent in persistents)
        {
            persistentDict[persistent.GetGuid()] = persistent;
        }

        List<Tuple<GameObject, ObjectPersistentData>> newObjects = new();
        foreach (var it in sceneData.ObjectDatas)
        {
            if (persistentDict.TryGetValue(it.Guid, out IPersistent persistent))
            {
                persistent.OnLoad(it.Data);
            }
            else
            {
                var assetHandle = Addressables.LoadAssetAsync<GameObject>(it.AddressablePath);
                yield return assetHandle;
                if (assetHandle.IsDone && assetHandle.IsValid())
                {
                    var instance = GameObject.Instantiate(assetHandle.Result);
                    newObjects.Add(new Tuple<GameObject, ObjectPersistentData>(instance, it));
                }
                else
                {
                    Debug.LogError($"Failed to load asset from path: {it.AddressablePath}");
                }
            }
        }
        foreach (var it in newObjects)
        {
            it.Item1.GetComponent<IPersistent>().OnLoad(it.Item2.Data);
        }
    }

    // TODO
    public IEnumerator<ScenePersistentData> SaveScene()
    {
        ScenePersistentData sceneData = new ScenePersistentData();
        yield return sceneData;
    }

    /// <summary>
    /// 获取游戏数据文件夹路径
    /// </summary>
    public static string GetGameDataPath()
    {
#if UNITY_EDITOR
        // 编辑器模式：使用项目文件夹
        return Path.Combine(Application.dataPath, "../PersistentData");
#elif UNITY_STANDALONE_WIN
        // Win32平台：使用游戏文件夹
        return Path.Combine(Application.dataPath, "../PersistentData");
#elif UNITY_ANDROID
        // Android平台：使用持久化数据路径
        return Path.Combine(Application.persistentDataPath, "/PersistentData");
#else
        // 其他平台默认使用持久化数据路径
        return Path.Combine(Application.persistentDataPath, "/PersistentData");
#endif
    }



    /// <summary>
    /// 保存数据到JSON文件
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="fileName">文件名（无需路径）</param>
    /// <param name="data">要保存的数据对象</param>
    /// <param name="prettyPrint">是否格式化JSON（默认true）</param>
    public void SaveData<T>(string fileName, T data, bool prettyPrint = true)
    {
        try
        {
            if (!Directory.Exists(GetGameDataPath()))
            {
                Directory.CreateDirectory(GetGameDataPath());
            }

            var fullPath = Path.Combine(GetGameDataPath(), fileName);
            var jsonData = JsonUtility.ToJson(data, prettyPrint);

            // 如果启用加密，则使用Base64编码
            var dataToWrite = enableEncryption ? EncodeToBase64(jsonData) : jsonData;

            using (var stream = new FileStream(fullPath, FileMode.Create))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(dataToWrite);
            }

            Debug.Log($"数据已保存到：{fullPath}{(enableEncryption ? "（已加密）" : "")}");
        }
        catch (Exception e)
        {
            Debug.LogError($"保存数据失败：{e.Message}");
        }
    }


    private Dictionary<string, object> PersistentDatas = new();
    
    public void UpdateData<T>(string fileName,T t) where T : class, new()
    {
        var data = t;
        SaveData(fileName, data);
    }
    
    /// <summary>
    /// 从JSON文件加载数据
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="fileName">文件名（无需路径）</param>
    /// <returns>数据对象或默认值</returns>
    public T LoadData<T>(string fileName) where T : class, new()
    {
        try
        {
            if (PersistentDatas.ContainsKey(fileName))
            {
                return PersistentDatas[fileName] as T;
            }

            var fullPath = Path.Combine(GetGameDataPath(), fileName);

            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"文件不存在，返回默认数据：{fullPath}");
                PersistentDatas[fileName] = new T();
                return PersistentDatas[fileName] as T;
            }

            using (var stream = new FileStream(fullPath, FileMode.Open))
            using (var reader = new StreamReader(stream))
            {
                var fileData = reader.ReadToEnd();
                
                // 如果启用加密，则从Base64解码
                string jsonData;
                try
                {
                    jsonData = enableEncryption ? DecodeFromBase64(fileData) : fileData;
                }
                catch (FormatException)
                {
                    // 如果解码失败，可能是未加密的旧数据，尝试直接解析
                    Debug.LogWarning($"Base64解码失败，尝试读取未加密数据：{fullPath}");
                    jsonData = fileData;
                }
                
                var t = JsonUtility.FromJson<T>(jsonData);
                PersistentDatas[fileName] = t;
                return PersistentDatas[fileName] as T;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"加载数据失败：{e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 删除指定数据文件
    /// </summary>
    public void DeleteData(string fileName)
    {
        var fullPath = Path.Combine(GetGameDataPath(), fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log($"已删除文件：{fullPath}");
        }
    }
}