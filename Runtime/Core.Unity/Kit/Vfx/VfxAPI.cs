
using UnityEngine;

public static class VfxAPI
{
    private static Transform _effectRoot;

    public static Transform mRootEffect
    {
        get
        {
            if (_effectRoot == null)
            {
                var obj = new GameObject();
                obj.name = "[RootEffect]";
                _effectRoot = obj.GetComponent<Transform>();
            }

            return _effectRoot;
        }
    }

    private static Transform _uiEffectRoot;

    public static Transform UIRootEffect
    {
        get
        {
            if (_uiEffectRoot == null)
            {
                var obj = new GameObject();
                obj.name = "[RootEffect]";
                _uiEffectRoot = obj.GetComponent<Transform>();
            }

            return _uiEffectRoot;
        }
    }

    public static Vfx CreateUIVisualEffect(GameObject gameObject, Vector3 worldPosition, Vector3 direction)
    {
        var effectBase = VfxManager.Instance.Get(gameObject, worldPosition,
            direction == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(direction),
            CanvasInstance.Instance.BehaviourInstance.transform);
        effectBase.gameObject.layer = LayerMask.NameToLayer("UI");
        return effectBase;
    }

    public static Vfx CreateVisualEffectAtUnit(GameObject source, UnitBase target, Vector3 direction,
        Vector3 offset)
    {
        var effectBase = VfxManager.Instance.Get(source, offset,
            direction == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(direction),
            target.transform);
        return effectBase;
    }

    public static Vfx CreateVisualEffect(GameObject source, Vector3 position, Vector3 direction)
    {
        var effectBase = VfxManager.Instance.Get(source, position, Quaternion.LookRotation(direction));
        effectBase.transform.SetParent(mRootEffect, true);
        return effectBase;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public static Vfx CreateVisualEffectWithLifeTime(GameObject source, Vector3 position, Vector3 direction,
        float lifeTime = -1f)
    {
        var effectBase = VfxManager.Instance.Get(source, position, Quaternion.LookRotation(direction));
        effectBase.mLifeTime = lifeTime;
        effectBase.transform.SetParent(mRootEffect, true);
        return effectBase;
    }

    public static Vfx CreateVisualEffect(GameObject source, Vector3 position)
    {
        var result = VfxManager.Instance.Get(source, position, Quaternion.identity);
        result.transform.SetParent(mRootEffect);
        return result;
    }
}