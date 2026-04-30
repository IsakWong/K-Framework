using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 自动绑定工具：扫描 MonoBehaviour 上标记了 [AutoBind] 的字段，
/// 从子物体层级中查找并自动赋值对应组件。
/// <para>
/// 推荐用法（编辑器期绑定，运行时零反射）：
/// <code>
/// [AutoBind, SerializeField] private Transform _muzzle;
/// #if UNITY_EDITOR
/// private void OnValidate() => AutoBinder.BindInEditor(this);
/// #endif
/// </code>
/// 字段必须可序列化（[SerializeField] 或 public），否则 OnValidate 写入的引用无法持久化。
/// </para>
/// <para>
/// 兜底用法：若运行时仍出现引用为空（例如字段未序列化），可在 Awake() 中调用
/// <see cref="BindRuntimeFallback"/> 临时补齐，但代价是反射开销。
/// </para>
/// </summary>
public static class AutoBinder
{
    // 每个类型缓存其 [AutoBind] 字段列表，避免重复反射
    private static readonly Dictionary<Type, FieldInfo[]> _cache = new();

    private const BindingFlags kBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器期绑定：在 OnValidate 中调用，将解析结果写回序列化字段，运行时无反射开销。
    /// 已赋值的字段不会被覆盖。返回 true 表示有字段被修改。
    /// </summary>
    public static bool BindInEditor(MonoBehaviour target)
    {
        return BindInternal(target, persist: true);
    }
#endif

    /// <summary>
    /// 运行时兜底：在 Awake/Initialize 中调用，扫描并绑定未填的 [AutoBind] 字段。
    /// 仅在序列化字段意外缺失时使用，常规情况应依赖 BindInEditor 在编辑器中固化结果。
    /// </summary>
    public static void BindRuntimeFallback(MonoBehaviour target)
    {
        BindInternal(target, persist: false);
    }

    private static bool BindInternal(MonoBehaviour target, bool persist)
    {
        if (target == null) return false;

        var fields = GetCachedFields(target.GetType());
        if (fields.Length == 0) return false;

        var transform = target.transform;
        bool changed = false;

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var attr = field.GetCustomAttribute<AutoBindAttribute>();
            if (attr == null) continue;

            // 已有值则跳过（Inspector / 之前 OnValidate 写入的引用优先，不覆盖）
            var existing = field.GetValue(target);
            if (existing != null && (existing is not UnityEngine.Object uo || uo != null))
                continue;

            object result;

            if (attr.Self)
            {
                // 从自身获取
                result = GetComponentOnTransform(target.gameObject, field.FieldType);
            }
            else
            {
                // 推导子物体名称
                string childName = !string.IsNullOrEmpty(attr.Name)
                    ? attr.Name
                    : FieldNameToChildName(field.Name);

                result = FindAndGetComponent(transform, childName, field.FieldType);
            }

            if (result != null)
            {
                field.SetValue(target, result);
                changed = true;
            }
            else if (!attr.Optional)
            {
                string childName = attr.Self ? "(self)" : (attr.Name ?? FieldNameToChildName(field.Name));
                Debug.LogWarning(
                    $"[AutoBind] {target.GetType().Name}.{field.Name}: " +
                    $"未找到 \"{childName}\" 上的 {field.FieldType.Name} 组件",
                    target);
            }
        }

#if UNITY_EDITOR
        if (persist && changed && !Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
#endif

        return changed;
    }

    /// <summary>
    /// 将字段名转换为子物体名：去 _ 前缀，首字母大写。
    /// _muzzleFlash → MuzzleFlash, Muzzle → Muzzle
    /// </summary>
    private static string FieldNameToChildName(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return fieldName;

        // 去除前导下划线
        int start = 0;
        while (start < fieldName.Length && fieldName[start] == '_')
            start++;
        if (start >= fieldName.Length) return fieldName;

        // 首字母大写
        char first = char.ToUpperInvariant(fieldName[start]);
        if (start + 1 >= fieldName.Length)
            return first.ToString();

        return first + fieldName.Substring(start + 1);
    }

    /// <summary>递归查找子物体并获取组件</summary>
    private static object FindAndGetComponent(Transform root, string childName, Type componentType)
    {
        // 先尝试直接 Find（支持路径格式 "A/B/C"）
        var child = root.Find(childName);
        if (child != null)
            return GetComponentOnTransform(child.gameObject, componentType);

        // 递归深度搜索
        child = FindChildRecursive(root, childName);
        if (child != null)
            return GetComponentOnTransform(child.gameObject, componentType);

        return null;
    }

    /// <summary>递归搜索子物体（广度优先）</summary>
    private static Transform FindChildRecursive(Transform parent, string name)
    {
        // 广度优先：先检查所有直接子物体
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }

        // 再递归
        for (int i = 0; i < childCount; i++)
        {
            var found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    /// <summary>从 GameObject 上获取指定类型的组件/引用</summary>
    private static object GetComponentOnTransform(GameObject go, Type fieldType)
    {
        if (fieldType == typeof(GameObject))
            return go;

        if (fieldType == typeof(Transform))
            return go.transform;

        return go.GetComponent(fieldType);
    }

    /// <summary>获取并缓存类型的 [AutoBind] 字段</summary>
    private static FieldInfo[] GetCachedFields(Type type)
    {
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        var allFields = new List<FieldInfo>();
        var current = type;

        // 遍历继承链（包括父类的私有字段）
        while (current != null && current != typeof(MonoBehaviour))
        {
            var fields = current.GetFields(kBindingFlags | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsDefined(typeof(AutoBindAttribute), false))
                    allFields.Add(fields[i]);
            }
            current = current.BaseType;
        }

        var result = allFields.ToArray();
        _cache[type] = result;
        return result;
    }
}
