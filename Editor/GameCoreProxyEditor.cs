using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GameCoreProxy 的自定义编辑器
/// 扫描所有 IModule 的具体实现类型，显示开关列表
/// 勾选后自动在 GameCoreProxy 下创建对应的模块 GameObject
/// </summary>
[CustomEditor(typeof(GameCoreProxy))]
public class GameCoreProxyEditor : Editor
{
    private List<Type> _moduleTypes;
    private bool _showModules = true;

    private void OnEnable()
    {
        _moduleTypes = FindAllModuleTypes();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        _showModules = EditorGUILayout.Foldout(_showModules, "模块管理", true, EditorStyles.foldoutHeader);

        if (!_showModules) return;

        var proxy = (GameCoreProxy)target;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (_moduleTypes == null || _moduleTypes.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 IModule 实现类型", MessageType.Info);
        }
        else
        {
            foreach (var moduleType in _moduleTypes)
            {
                DrawModuleToggle(proxy, moduleType);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("刷新模块列表"))
        {
            _moduleTypes = FindAllModuleTypes();
        }
    }

    /// <summary>
    /// 绘制单个模块的 Bool 开关
    /// </summary>
    private void DrawModuleToggle(GameCoreProxy proxy, Type moduleType)
    {
        // 在 GameCoreProxy 的子节点中查找该类型的组件
        var existingComponent = FindModuleInChildren(proxy, moduleType);
        bool exists = existingComponent != null;

        EditorGUILayout.BeginHorizontal();

        bool newValue = EditorGUILayout.ToggleLeft(moduleType.Name, exists);

        EditorGUILayout.EndHorizontal();

        if (newValue != exists)
        {
            if (newValue)
            {
                // 创建模块 GameObject 并挂载组件
                CreateModule(proxy, moduleType);
            }
            else
            {
                // 销毁模块 GameObject
                DestroyModule(existingComponent);
            }
        }
    }

    /// <summary>
    /// 在 GameCoreProxy 的子节点中查找指定类型的模块
    /// </summary>
    private Component FindModuleInChildren(GameCoreProxy proxy, Type moduleType)
    {
        // 先检查子节点
        for (int i = 0; i < proxy.transform.childCount; i++)
        {
            var child = proxy.transform.GetChild(i);
            var comp = child.GetComponent(moduleType);
            if (comp != null)
                return comp;
        }

        // 也检查场景中是否有独立存在的该模块
        var allInScene = FindObjectsByType(moduleType, FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allInScene != null && allInScene.Length > 0)
            return allInScene[0] as Component;

        return null;
    }

    /// <summary>
    /// 创建模块 GameObject 并挂载到 GameCoreProxy 下
    /// </summary>
    private void CreateModule(GameCoreProxy proxy, Type moduleType)
    {
        Undo.RecordObject(proxy, $"创建模块 {moduleType.Name}");

        var go = new GameObject($"[{moduleType.Name}]");
        Undo.RegisterCreatedObjectUndo(go, $"创建模块 {moduleType.Name}");

        go.transform.SetParent(proxy.transform);
        go.transform.localPosition = Vector3.zero;

        Undo.AddComponent(go, moduleType);

        EditorUtility.SetDirty(proxy);
    }

    /// <summary>
    /// 销毁模块组件所在的 GameObject
    /// </summary>
    private void DestroyModule(Component component)
    {
        if (component == null) return;

        Undo.DestroyObjectImmediate(component.gameObject);
    }

    /// <summary>
    /// 通过反射查找所有继承自 TModule 的具体类型
    /// </summary>
    private static List<Type> FindAllModuleTypes()
    {
        var moduleInterface = typeof(IModule);
        var monoType = typeof(MonoBehaviour);

        var types = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 跳过 Unity/System 内部程序集以提高性能
            var assemblyName = assembly.GetName().Name;
            if (assemblyName.StartsWith("Unity") ||
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("Microsoft") ||
                assemblyName.StartsWith("Mono") ||
                assemblyName.StartsWith("mscorlib") ||
                assemblyName.StartsWith("netstandard"))
                continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // 必须是具体类（非抽象、非泛型定义）
                    if (type.IsAbstract || type.IsGenericTypeDefinition)
                        continue;

                    // 必须实现 IModule 并且是 MonoBehaviour
                    if (!moduleInterface.IsAssignableFrom(type))
                        continue;

                    if (!monoType.IsAssignableFrom(type))
                        continue;

                    // 排除 TModule<T> 自身（泛型基类）
                    if (type.Name.StartsWith("TModule"))
                        continue;

                    types.Add(type);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 忽略无法加载的程序集
            }
        }

        types.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return types;
    }

#if UNITY_2021_2_OR_NEWER
    private UnityEngine.Object[] FindObjectsByType(Type type, FindObjectsInactive includeInactive, FindObjectsSortMode sortMode)
    {
        // Unity 2023+ 使用 FindObjectsByType
        return UnityEngine.Object.FindObjectsByType(type, includeInactive, sortMode);
    }
#endif
}

