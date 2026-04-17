using Framework.Foundation;
using UnityEngine;


public class VfxEmmiter : MonoBehaviour
{

}


public class VfxManager : PersistentSingleton<VfxManager>, IVfxService
{
    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IVfxService>(this);
    }

    public Vfx Get(GameObject origin)
    {
        var instance = PoolManager.Instance.Get(origin);
        var vfx = instance.GetComponent<Vfx>();
        if (vfx == null)
        {
            EnhancedLog.Warning("Vfx", $"Prefab '{origin.name}' has no Vfx component. Adding one.");
            vfx = instance.AddComponent<Vfx>();
        }
        return vfx;
    }

    public Vfx Get(Vfx vfx)
    {
        return PoolManager.Instance.Get(vfx);
    }

    public Vfx Get(GameObject origin, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var instance = PoolManager.Instance.Get(origin, position, rotation, parent);
        var vfx = instance.GetComponent<Vfx>();
        if (vfx == null)
            vfx = instance.AddComponent<Vfx>();
        return vfx;
    }

    public Vfx Get(Vfx prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return PoolManager.Instance.Get(prefab, position, rotation, parent);
    }

    public void Release(Vfx vfx)
    {
        PoolManager.Instance.Release(vfx);
    }

    /// <summary>
    /// Pre-warm the pool for a VFX prefab.
    /// </summary>
    public void Preload(GameObject prefab, int count)
    {
        PoolManager.Instance.Preload(prefab, count);
    }
}