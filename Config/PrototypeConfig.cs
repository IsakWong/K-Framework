using System;
using System.Linq;
using System.Reflection;
using AYellowpaper.SerializedCollections;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;

public interface IPrototypeConfig
{
    public void RefreshPrototypeData();
}

public class PrototypeConfig<T> : ScriptableObject, IPrototypeConfig where T : class
{
    [HideInInspector] public SerializedDictionary<string, Variant> Datas;

    [HideInInspector] public SerializeType<T> Prototype;

    public void RefreshPrototypeData()
    {
        FieldInfo[] fields = Prototype.As().GetFields(BindingFlags.Public | BindingFlags.Instance);
        // 指定要查找的类型
        Type targetType = typeof(Variant);
        object defaultInstance = Activator.CreateInstance(Prototype);
        // 过滤出指定类型的字段
        var targetFields = fields.Where(field => (typeof(IVariant).IsAssignableFrom(field.FieldType)));
        // 输出结果
        foreach (var field in targetFields)
        {
            if (!Datas.ContainsKey(field.Name))
            {
                if (field != null && typeof(IVariant).IsAssignableFrom(field.FieldType))
                {
                    Datas[field.Name] = new Variant();
                    var t = VariantTypeHelper.ConvertToVariantType(field.FieldType.GenericTypeArguments[0]);
                    IVariant defaultValue = (IVariant)field.GetValue(defaultInstance);
                    Datas[field.Name].mType = t;
                    Datas[field.Name].SetVariant(defaultValue.GetVariant());
                    //AbiPrototypeData[field.Name].Set(field.GetRawConstantValue());
                }
            }
        }
    }

    public virtual T CreateInstance()
    {
        Type type = (Type)Prototype;
        if (type == null)
        {
            Debug.Assert(false, "Prototype is null!!");
            return null;
        }

        var instance = Activator.CreateInstance(Prototype) as T;
        foreach (var it in Datas)
        {
            FieldInfo field = type.GetField(it.Key);
            if (field != null && typeof(IVariant).IsAssignableFrom(field.FieldType))
            {
                object value = field.GetValue(instance);
                IVariant var = (IVariant)(value);
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
        if(Prototype.As() != null)
            return Prototype.As().Name;
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
    SerializedProperty TypeProp;
    SerializedProperty DataProp;

    void OnEnable()
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

        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }
}

#endif