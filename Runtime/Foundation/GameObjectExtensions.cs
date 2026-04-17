using UnityEngine;

public static class GameObjectExtensions
{
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (!component)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    /// <summary>
    /// 统一的 GameObject 实例化接口，兼容编辑器和 Runtime。
    /// 编辑器非 Play 模式下使用 PrefabUtility.InstantiatePrefab 保持 Prefab 链接；
    /// Runtime 下使用 Object.Instantiate。
    /// </summary>
    /// <param name="prefab">要实例化的预制体</param>
    /// <param name="parent">父 Transform（可选）</param>
    /// <returns>实例化的 GameObject</returns>
    public static GameObject InstantiatePrefab(this GameObject prefab, Transform parent = null)
    {
        GameObject instance;
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            instance = parent != null
                ? Object.Instantiate(prefab, parent)
                : Object.Instantiate(prefab);
        }
        else
        {
            instance = parent != null
                ? (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent)
                : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        }
#else
        instance = parent != null
            ? Object.Instantiate(prefab, parent)
            : Object.Instantiate(prefab);
#endif
        return instance;
    }

    /// <summary>
    /// 统一的空 GameObject 创建接口，兼容编辑器和 Runtime。
    /// 编辑器非 Play 模式下会注册 Undo。
    /// </summary>
    /// <param name="name">GameObject 名称</param>
    /// <param name="parent">父 Transform（可选）</param>
    /// <returns>创建的 GameObject</returns>
    public static GameObject Create(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null)
        {
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        }
#endif
        return go;
    }
}