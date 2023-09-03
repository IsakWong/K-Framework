
using System;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


public enum VariantType
{
    None,
    Bool,
    Enum,
    Int,
    Float,    
    Double,
    String,
    Vector2,
    Vector3,
    Vector4,
    Color32,
    UnityObject
}

public static class VariableTypeTemplate
{    
    public static VariantType GetVariantType(this Type type)
    {
        if(type == typeof(int))
            return VariantType.Int;
        if(type == typeof(float))
            return VariantType.Float;
        if(type == typeof(bool))
            return VariantType.Bool;
        if(type == typeof(double))
            return VariantType.Double;
        if(type == typeof(string))
            return VariantType.String;
        if(type == typeof(Vector2))
            return VariantType.Vector2;
        if(type == typeof(Vector3))
            return VariantType.Vector3;
        if(type == typeof(Vector4))
            return VariantType.Vector4;
        if(type == typeof(Color32))
            return VariantType.Color32;
        if(type.IsSubclassOf(typeof(UnityEngine.Object)))
            return VariantType.UnityObject;

        throw new NotImplementedException();
    }
}



#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(Variant))]
public class SerializedVarintEditor : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty proper, GUIContent content)
    {
        return EditorGUIUtility.singleLineHeight * 2;
    }

    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);
        var valueString = property.FindPropertyRelative("mValueString");
        var objectProp = property.FindPropertyRelative("mObject");
        var typeProperty = property.FindPropertyRelative("mType");

        EditorGUIUtility.wideMode = true;
        EditorGUIUtility.labelWidth = 70;
        float height = rect.height * 0.5f;
        rect.height /= 2.0f;

        var vType = (VariantType)typeProperty.enumValueIndex;
        typeProperty.enumValueIndex = (int)(VariantType)EditorGUI.EnumPopup(rect, "Type", vType);
        rect.yMin += height;
        rect.yMax += height;
        switch (vType)
        {
            case VariantType.Bool:
            {
                var outVar = JsonUtility.FromJson<bool>(valueString.stringValue);
                outVar = EditorGUI.Toggle(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.Float:
            {
                var outVar = JsonUtility.FromJson<float>(valueString.stringValue);
                outVar = EditorGUI.FloatField(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.Int:
            {
                var outVar = JsonUtility.FromJson<int>(valueString.stringValue);
                outVar = EditorGUI.IntField(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.Vector2:
            {
                var outVar = JsonUtility.FromJson<Vector2>(valueString.stringValue);
                outVar = EditorGUI.Vector2Field(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.Vector3:
            {
                var outVar = JsonUtility.FromJson<Vector3>(valueString.stringValue);
                outVar = EditorGUI.Vector3Field(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.Vector4:
            {
                var outVar = JsonUtility.FromJson<Vector4>(valueString.stringValue);
                outVar = EditorGUI.Vector4Field(rect, "Value", outVar);
                valueString.stringValue = JsonUtility.ToJson(outVar);
                break;
            }
            case VariantType.UnityObject:
            {
                var outVar = objectProp.objectReferenceValue;
                outVar = EditorGUI.ObjectField(rect, "Value", outVar, typeof(UnityEngine.Object));
                objectProp.objectReferenceValue = outVar;
                break;
            }
        }
        EditorGUI.EndProperty();
    }
}
#endif

[Serializable]
public class Variant : ISerializationCallbackReceiver
{
    public VariantType        mType;
    public string             mValueString;
    public UnityEngine.Object mObject;

    private object mValue;

    public Variant(int value)
    {
        mValue = value;
        mType = VariantType.Int;
    }

    public Variant(float value)
    {
        mValue = value;
        mType = VariantType.Float;
    }

    public Variant(bool value)
    {
        mValue = value;
        mType = VariantType.Bool;
    }

    public Variant(double value)
    {
        mValue = value;
        mType = VariantType.Double;
    }

    public Variant(string value)
    {
        mValue = value;
        mType = VariantType.String;
    }

    public Variant(UnityEngine.Object value)
    {
        mValue = value;
        mType = VariantType.UnityObject;
    }

    public Variant(Vector2 value)
    {
        mValue = value;
        mType = VariantType.Vector2;
    }

    public Variant(Vector3 value)
    {
        mValue = value;
        mType = VariantType.Vector3;
    }

    public Variant(Vector4 value)
    {
        mValue = value;
        mType = VariantType.Vector4;
    }

    public T Get<T>() where T : class
    {
        if (typeof(T).GetVariantType() == mType)
        {
            return (T)mValue;
        }
        throw new Exception("Type mismatch");
    }
    
    void FromJson<T>()
    {
        try
        {
            mValue = JsonUtility.FromJson<T>(mValueString);

        }
        catch (Exception e)
        {
            mValue = default(T);
            EnhancedLog.LogError(e.Message);
            throw;
        }
    }

    public void OnBeforeSerialize()
    {
        switch (mType)
        {
            case VariantType.Bool:
            case VariantType.Int:
            case VariantType.Float:
            case VariantType.String:
            case VariantType.Vector2:
            case VariantType.Vector3:
            case VariantType.Vector4:
                mValueString = JsonUtility.ToJson(mValue);
                break;
            case VariantType.UnityObject:
                break;
        }
    }

    public void OnAfterDeserialize()
    {
        switch (mType)
        {
            case VariantType.Int:
                FromJson<int>();
                break;
            case VariantType.Bool:
                FromJson<bool>();
                break;
            case VariantType.Float:
                FromJson<float>();
                break;
            case VariantType.String:
                FromJson<string>();
                break;
            case VariantType.Vector2:
                FromJson<Vector2>();
                break;
            case VariantType.Vector3:
                FromJson<Vector3>();
                break;
            case VariantType.Vector4:
                FromJson<Vector4>();
                break;
            case VariantType.UnityObject:
                mValue = mObject;
                break;
        }
        return;
    }

    public void Set(bool v)
    {
        mValue = v;
        mType = VariantType.Bool;
    }

    public void Set(int v)
    {
        mValue = v;
        mType = VariantType.Int;
    }

    public void Set(float v)
    {
        mValue = v;
        mType = VariantType.Float;
    }
    public void Set(double v)
    {
        mValue = v;
        mType = VariantType.Double;
    }
    public void Set(string v)
    {
        mValue = v;
        mType = VariantType.String;
    }
    public void Set(Vector2 v)
    {
        mValue = v;
        mType = VariantType.Vector2;
    }
    public void Set(Vector3 v)
    {
        mValue = v;
        mType = VariantType.Vector3;
    }
    public void Set(Color32 v)
    {
        mValue = v;
        mType = VariantType.Color32;
    }
    public void Set(UnityEngine.Object v)
    {
        mValue = v;
        mType = VariantType.UnityObject;
    }
}

