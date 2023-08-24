using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 
/// Variant目前支持的类型
/// 
/// </summary>
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
    UnityObject,
    Quaternion,
    List
}

public static class VariantTypeHelper
{
    public static VariantType ConvertToVariantType(this Type type)
    {
        if (type == typeof(int))
            return VariantType.Int;
        if (type == typeof(float))
            return VariantType.Float;
        if (type == typeof(bool))
            return VariantType.Bool;
        if (type == typeof(double))
            return VariantType.Double;
        if (type == typeof(string))
            return VariantType.String;
        if (type == typeof(Vector2))
            return VariantType.Vector2;
        if (type == typeof(Vector3))
            return VariantType.Vector3;
        if (type == typeof(Vector4))
            return VariantType.Vector4;
        if (type == typeof(Color32))
            return VariantType.Color32;
        if (type == typeof(Quaternion))
            return VariantType.Quaternion;
        if (type.IsSubclassOf(typeof(UnityEngine.Object)))
            return VariantType.UnityObject;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return VariantType.List;
        return VariantType.None;
    }
}

/// <summary>
///
/// Variant是一个通用的支持序列化反序列化，支持常见的Unity数据的（Unity.Object，Vector3）等的数据容器。
/// 常用于数据表，相对于C#的object，它支持Unity的序列化系统 
/// 
/// </summary>
[Serializable]
public class Variant : ISerializationCallbackReceiver, IVariant
{
    [SerializeField] public VariantType mType;
    [SerializeField] public string mValueString;
    [SerializeField] public UnityEngine.Object mObject;

    protected object mValue;


    /// <summary>
    /// 获取变量类型。
    /// </summary>
    /// <returns></returns>
    public object GetRaw()
    {
        if (mType == VariantType.UnityObject)
            return mObject;
        return mValue;
    }

    public Variant(Variant v)
    {
        mType = v.mType;
        mValue = v.mValue;
        mObject = v.mObject;
    }

    public Variant()
    {
        mType = VariantType.None;
    }

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

    public void Set(object b)
    {
        mValue = b;
    }

    public object GetValue()
    {
        return mValue;
    }

    public T Get<T>(T defaultValue = default(T))
    {
        if (typeof(T).ConvertToVariantType() == mType)
        {
            return (T)mValue;
        }

        return defaultValue;
    }

    public void OnBeforeSerialize()
    {
        switch (mType)
        {
            case VariantType.Bool:
                mValue ??= default(bool);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Int:
                mValue ??= default(int);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Float:
                mValue ??= default(float);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.String:
                mValue ??= default(String);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Vector2:
                mValue ??= default(Vector2);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Vector3:
                mValue ??= default(Vector3);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Vector4:
                mValue ??= default(Vector4);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Color32:
                mValue ??= default(Color32);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.Quaternion:
                mValue ??= default(Quaternion);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.List:
                mValue ??= default(List<Variant>);
                mValueString = JsonConvert.SerializeObject(mValue);
                break;
            case VariantType.UnityObject:
                mValue = mObject;
                break;
        }
    }

    void Deserialize<T>()
    {
        try
        {
            mValue = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(mValueString);
        }
        catch (JsonReaderException)
        {
            mValue = default(T);
        }
        catch (JsonSerializationException)
        {
            mValue = default(T);
        }
    }

    public void OnAfterDeserialize()
    {
        switch (mType)
        {
            case VariantType.Int:
                Deserialize<int>();
                break;
            case VariantType.Bool:
                Deserialize<bool>();
                break;
            case VariantType.Float:
                Deserialize<float>();
                break;
            case VariantType.String:
                Deserialize<string>();
                break;
            case VariantType.Vector2:
                Deserialize<Vector2>();
                break;
            case VariantType.Vector3:
                Deserialize<Vector3>();
                break;
            case VariantType.Vector4:
                Deserialize<Vector4>();
                break;
            case VariantType.Color32:
                Deserialize<Color32>();
                break;
            case VariantType.UnityObject:
                mValue = mObject;
                break;
            case VariantType.List:
                Deserialize<List<Variant>>();
                break;
        }

        return;
    }

    public void Set(Variant v)
    {
        mValue = v.mValue;
        mType = v.mType;
        mValueString = v.mValueString;
        mObject = v.mObject;
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


    public static implicit operator GameObject(Variant t) => t.mObject as GameObject;
    public static implicit operator float(Variant t) => (float)t.mValue;
    public static implicit operator int(Variant t) => (int)t.mValue;
    public static implicit operator Vector2(Variant t) => (Vector2)t.mValue;
    public static implicit operator Vector3(Variant t) => (Vector3)t.mValue;
    public static implicit operator Quaternion(Variant t) => (Quaternion)t.mValue;

    public void SetVariant(Variant v)
    {
        mObject = v.mObject;
        mValue = v.mValue;
        mValueString = v.mValueString;
    }

    public Variant GetVariant()
    {
        return this;
    }
}

public class ListVariant : Variant
{
    [SerializeField] public List<Variant> ListValue;

    public ListVariant()
    {
        mType = VariantType.List;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
    }
}

interface IVariant
{
    void SetVariant(Variant v);
    Variant GetVariant();
}


public struct ObjectVariantRef<T> : IVariant where T : UnityEngine.Object
{
    public Variant varaint;

    public ObjectVariantRef(T t)
    {
        varaint = new Variant();
        varaint.Set(t);
    }

    public Variant GetVariant()
    {
        return varaint;
    }

    public void SetVariant(Variant v)
    {
        varaint = v;
    }

    public T As()
    {
        return (T)varaint.mObject;
    }

    public static implicit operator T(ObjectVariantRef<T> t)
    {
        if (t.varaint != null)
            return (T)t.varaint.mObject;
        return default(T);
    }
}

/// <summary>
/// Variant引用
/// </summary>
/// <typeparam name="T"></typeparam>
public class VariantRef<T> : IVariant
{
    public Variant varaint = new Variant();

    public VariantRef(T t = default(T))
    {
        varaint = new Variant();
        varaint.Set(t);
    }

    public Variant GetVariant()
    {
        return varaint;
    }

    public void SetVariant(Variant v)
    {
        varaint = v;
    }

    public T As()
    {
        return varaint.Get<T>();
    }


    public static implicit operator T(VariantRef<T> t)
    {
        if (t.varaint != null)
            return (T)t.varaint.Get<T>();
        return default(T);
    }
}

/// <summary>
/// Template variant
/// </summary>
/// <typeparam name="T"></typeparam>
public class TVariant<T> : Variant
{
    public TVariant()
    {
    }

    public TVariant(Variant v) :
        base(v)
    {
    }

    public T As()
    {
        return (T)mValue;
    }

    public static implicit operator T(TVariant<T> t)
    {
        return (T)t.mValue;
    }
}

/// <summary>
/// UnityObject Variant
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectVarint<T> : Variant where T : UnityEngine.Object
{
    public ObjectVarint()
    {
    }

    public ObjectVarint(Variant v) :
        base(v)
    {
    }

    public T As()
    {
        return (T)mObject;
    }

    public static implicit operator T(ObjectVarint<T> t)
    {
        return (T)t.mObject;
    }
}