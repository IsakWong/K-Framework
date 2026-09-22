using Framework.Foundation;
using UnityEngine;


public class VfxEmmiter : MonoBehaviour
{

}


public class VfxService : PersistentSingleton<VfxService>, IVfxService
{
    protected override void OnServiceInit()
    {
        ServiceLocator.Register<IVfxService>(this);
    }

    public Vfx Get(GameObject origin)
    {
        var instance = PoolService.Instance.Get(origin);
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
        return PoolService.Instance.Get(vfx);
    }

    public Vfx Get(GameObject origin, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var instance = PoolService.Instance.Get(origin, position, rotation, parent);
        var vfx = instance.GetComponent<Vfx>();
        if (vfx == null)
            vfx = instance.AddComponent<Vfx>();
        return vfx;
    }

    public Vfx Get(Vfx prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return PoolService.Instance.Get(prefab, position, rotation, parent);
    }

    public void Release(Vfx vfx)
    {
        PoolService.Instance.Release(vfx);
    }

    /// <summary>
    /// Pre-warm the pool for a VFX prefab.
    /// </summary>
    public void Preload(GameObject prefab, int count)
    {
        PoolService.Instance.Preload(prefab, count);
    }
}