using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AYellowpaper.SerializedCollections;


#if UNITY_EDITOR
using Framework;
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;

public interface IPrototypeConfig
{
    public void RefreshPrototypeData();
    public void RemoveUnusedPrototypeData();
    
    public SerializedDictionary<string, Variant> GetDatas();

}


public class PrototypeConfig<T> : ScriptableObject, IPrototypeConfig where T : class
{
    [HideInInspector] public SerializedDictionary<string, Variant> Datas;

    [HideInInspector] public SerializeType<T> Prototype;

    public void RefreshPrototypeData()
    {
        var fields = Prototype.As().GetFields(BindingFlags.Public | BindingFlags.Instance);
        // 指定要查找的类型
        var targetType = typeof(Variant);
        var defaultInstance = Activator.CreateInstance(Prototype);
        // 过滤出指定类型的字段
        var targetFields = fields.Where(field => typeof(IVariant).IsAssignableFrom(field.FieldType));
        // 输出结果
        foreach (var field in targetFields)
        {
            if (!Datas.ContainsKey(field.Name))
            {
                if (field != null && typeof(IVariant).IsAssignableFrom(field.FieldType))
                {
                    Datas[field.Name] = new Variant();
                    var t = VariantTypeHelper.ConvertToVariantType(field.FieldType.GenericTypeArguments[0]);
                    var defaultValue = (IVariant)field.GetValue(defaultInstance);
                    Datas[field.Name].mType = t;
                    Datas[field.Name].SetVariant(defaultValue.GetVariant());
                    //AbiPrototypeData[field.Name].Set(field.GetRawConstantValue());
                }
            }
        }
    }

    public void RemoveUnusedPrototypeData()
    {
        var fields = Prototype.As().GetFields(BindingFlags.Public | BindingFlags.Instance);
        // 指定要查找的类型
        var targetType = typeof(Variant);
        var defaultInstance = Activator.CreateInstance(Prototype);
        // 过滤出指定类型的字段
        var targetFields = fields.Where(field => typeof(IVariant).IsAssignableFrom(field.FieldType));
        HashSet<string> used = new();
        foreach (var field in targetFields)
        {
            if (field != null && typeof(IVariant).IsAssignableFrom(field.FieldType))
            {
                used.Add(field.Name);
            }
        }

        List<string> toRemove = new();

        foreach (var it in Datas)
        {
            if (!used.Contains(it.Key))
            {
                toRemove.Add(it.Key);
            }
        }

        foreach (var it in toRemove)
        {
            Datas.Remove(it);
        }
    }

    public SerializedDictionary<string, Variant> GetDatas()
    {
        return Datas;
    }

    public virtual T CreateInstance()
    {
        var type = (Type)Prototype;
        if (type == null)
        {
            Debug.Assert(false, "Prototype is null!!");
            return null;
        }

        var instance = Activator.CreateInstance(Prototype) as T;

        foreach (var it in Datas)
        {
            var field = type.GetField(it.Key);
            if (field != null && typeof(IVariant).IsAssignableFrom(field.FieldType))
            {
                var value = field.GetValue(instance);
                var var = (IVariant)value;
                if (var == null)
                {
                    Debug.Assert(false, "Field is null!!");
                    continue;
                }

                var.SetVariant(it.Value);
                field.SetValue(instance, var);
            }
        }

        return instance;
    }

    public override string ToString()
    {
        if (Prototype.As() != null)
        {
            return $"({Prototype.As().Name})";
        }

        return name;
    }
}

public class CommonConfig : ScriptableObject
{
}

#if UNITY_EDITOR

[CustomEditor(typeof(PrototypeConfig<>), true)]
public class PrototypeConfigEditor : Editor
{
    private SerializedProperty TypeProp;
    private SerializedProperty DataProp;

    private void OnEnable()
    {
        TypeProp = serializedObject.FindProperty("Prototype");
        DataProp = serializedObject.FindProperty("Datas");
    }

    public override void OnInspectorGUI()
    {
        var config = target as IPrototypeConfig;
        serializedObject.Update();

        EditorGUILayout.PropertyField(TypeProp);
        EditorGUILayout.PropertyField(DataProp);

        if (GUILayout.Button("Refresh"))
        {
            config.RefreshPrototypeData();
        }

        if (GUILayout.Button("Remove"))
        {
            config.RemoveUnusedPrototypeData();
        }

        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }
}

#endif