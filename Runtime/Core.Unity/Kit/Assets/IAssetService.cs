using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 资源管理服务接口
/// 提供同步/异步资源加载、实例化、释放等能力
/// </summary>
public interface IAssetService
{
    // ─── 路径加载 ───

    T LoadAsset<T>(string path) where T : Object;
    Task<T> LoadAssetAsync<T>(string path) where T : Object;

    // ─── AssetReference 加载 ───

    T LoadAssetReference<T>(AssetReference assetReference) where T : Object;
    T LoadAsset<T>(AssetReferenceT<T> assetReference) where T : Object;
    Task<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object;
    Task<T> LoadAssetAsync<T>(AssetReferenceT<T> assetReference) where T : Object;

    // ─── 实例化 ───

    GameObject InstantiateAssetReference(AssetReference assetReference, Transform parent = null);
    Task<GameObject> InstantiateAssetReferenceAsync(AssetReference assetReference, Transform parent = null);

    // ─── 释放 ───

    void ReleaseAssetReference(AssetReference assetReference);
    void ReleaseAsset(string path);
    void ReleaseAll();
}
