using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 自动绑定工具：扫描 MonoBehaviour 上标记了 [AutoBind] 的字段，
/// 从子物体层级中查找并自动赋值对应组件。
/// <para>
/// 用法：在 Awake() 或 Initialize() 中调用 AutoBinder.Bind(this);
/// </para>
/// </summary>
public static class AutoBinder
{
    // 每个类型缓存其 [AutoBind] 字段列表，避免重复反射
    private static readonly Dictionary<Type, FieldInfo[]> _cache = new();

    private const BindingFlags kBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// 扫描目标对象的所有 [AutoBind] 字段并自动绑定。
    /// </summary>
    public static void Bind(MonoBehaviour target)
    {
        if (target == null) return;

        var fields = GetCachedFields(target.GetType());
        if (fields.Length == 0) return;

        var transform = target.transform;

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var attr = field.GetCustomAttribute<AutoBindAttribute>();
            if (attr == null) continue;

            // 已有值则跳过（支持 Inspector 手动赋值优先）
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
