using System.Collections;

/// <summary>
/// 持久化数据管理服务接口
/// 提供 JSON 序列化的数据保存/加载/删除等能力
/// </summary>
public interface IPersistentDataService
{
    void SaveData<T>(string fileName, T data, bool prettyPrint = true);
    T LoadData<T>(string fileName) where T : class, new();
    void UpdateData<T>(string fileName, T t) where T : class, new();
    void DeleteData(string fileName);
    IEnumerator LoadPersistenSceneData(ScenePersistentData sceneData);
}
