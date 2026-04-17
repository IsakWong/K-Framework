using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SerializeType<T> : ISerializationCallbackReceiver
{
    [SerializeField] private string qualifiedName;

    private Type storedType
    {
        get
        {
            if (_storedType != null)
            {
                return _storedType;
            }
#if UNITY_EDITOR
            if (Script)
            {
                _storedType = Script.GetClass();
            }

            return _storedType;
#endif
            return null;
        }
    }

    private Type _storedType;

#if UNITY_EDITOR
    [SerializeField] public MonoScript Script;

    // HACK: I wasn't able to find the base type from the SerializedProperty,
    // so I'm smuggling it in via an extra string stored only in-editor.
    [SerializeField] private string baseTypeName;
#endif

    public SerializeType(Type typeToStore)
    {
        _storedType = typeToStore;
        qualifiedName = typeToStore.AssemblyQualifiedName;
    }

    public string TypeName => storedType != null ? storedType.Name : "None";

    public override string ToString()
    {
        if (storedType == null)
        {
            return string.Empty;
        }

        return storedType.AssemblyQualifiedName;
    }

    public Type As()
    {
        return storedType;
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (BuildPipeline.isBuildingPlayer)
        {
        }
        else
        {
            if (_storedType == null && Script)
            {
                _storedType = Script.GetClass();
            }

            baseTypeName = typeof(T).AssemblyQualifiedName;
            if (_storedType != null)
            {
                qualifiedName = _storedType.AssemblyQualifiedName;
            }
        }
#endif
    }

    public void OnAfterDeserialize()
    {
        if (string.IsNullOrEmpty(qualifiedName) || qualifiedName == "null")
        {
            _storedType = null;
            return;
        }

        _storedType = Type.GetType(qualifiedName);
        if (_storedType != null)
        {
            qualifiedName = _storedType.AssemblyQualifiedName;
        }
    }

    public override int GetHashCode()
    {
        return storedType.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is SerializeType<T>)
        {
            var t = obj as SerializeType<T>;
            return storedType == t.storedType;
        }

        return base.Equals(obj);
    }


    public static implicit operator Type(SerializeType<T> t)
    {
        return t.storedType;
    }
}

[Serializable]
public class CommonSerializeType : SerializeType<object>
{
    public CommonSerializeType(Type typeToStore) : base(typeToStore)
    {
    }
}