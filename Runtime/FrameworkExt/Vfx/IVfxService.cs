using UnityEngine;

/// <summary>
/// 特效管理服务接口
/// 提供特效对象池的获取和释放
/// </summary>
public interface IVfxService
{
    Vfx Get(GameObject origin);
    Vfx Get(Vfx vfx);
    Vfx Get(GameObject origin, Vector3 position, Quaternion rotation, Transform parent = null);
    Vfx Get(Vfx prefab, Vector3 position, Quaternion rotation, Transform parent = null);
    void Release(Vfx vfx);
    void Preload(GameObject prefab, int count);
}
