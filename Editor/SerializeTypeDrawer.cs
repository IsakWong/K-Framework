#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using K1.Gameplay;
using Type = System.Type;

[CustomPropertyDrawer(typeof(SerializeType<>), true)]
public class SerializeTypeDrawer : PropertyDrawer
{
    System.Type[] _derivedTypes;
    GUIContent[] _optionLabels;
    int _selectedIndex;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var storedProperty = property.FindPropertyRelative("qualifiedName");
        var storedScript = property.FindPropertyRelative("Script");
        string qualifiedName = storedProperty.stringValue;

        if (_optionLabels == null)
        {
            Initialize(property, storedProperty);
        }
        else if (_selectedIndex == _derivedTypes.Length)
        {
            if (qualifiedName != "null") UpdateIndex(storedProperty);
        }
        else
        {
            if (qualifiedName != _derivedTypes[_selectedIndex].AssemblyQualifiedName) UpdateIndex(storedProperty);
        }

        var propLabel = EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.ObjectField(storedScript, typeof(MonoScript));
        if (EditorGUI.EndChangeCheck())
        {
            MonoScript script = storedScript.objectReferenceValue as MonoScript;
            Type cls = script.GetClass();
            storedProperty.stringValue = cls.AssemblyQualifiedName;
        }

        EditorGUI.BeginChangeCheck();
        _selectedIndex = EditorGUI.Popup(position, propLabel, _selectedIndex, _optionLabels);

        if (EditorGUI.EndChangeCheck())
        {
            storedProperty.stringValue = _selectedIndex < _derivedTypes.Length
                ? _derivedTypes[_selectedIndex].AssemblyQualifiedName
                : "null";
        }

        EditorGUI.EndProperty();
    }

    static Type[] FindAllDerivedTypes(Type baseType)
    {
        var t = typeof(AIYasuoBossController);
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var types = baseType.Assembly.GetTypes();
        var typesEnum = types.Where(t => t != baseType && baseType.IsAssignableFrom(t));
        return typesEnum.ToArray<Type>();
    }

    void Initialize(SerializedProperty property, SerializedProperty stored)
    {
        var baseTypeProperty = property.FindPropertyRelative("baseTypeName");
        var baseType = Type.GetType(baseTypeProperty.stringValue);

        _derivedTypes = FindAllDerivedTypes(baseType);

        if (_derivedTypes.Length == 0)
        {
            _optionLabels = new[] { new GUIContent($"No types derived from {baseType.Name} found.") };
            return;
        }

        _optionLabels = new GUIContent[_derivedTypes.Length + 1];
        for (int i = 0; i < _derivedTypes.Length; i++)
        {
            _optionLabels[i] = new GUIContent(_derivedTypes[i].Name);
        }

        _optionLabels[_derivedTypes.Length] = new GUIContent("null");

        UpdateIndex(stored);
    }

    void UpdateIndex(SerializedProperty stored)
    {
        string qualifiedName = stored.stringValue;

        for (int i = 0; i < _derivedTypes.Length; i++)
        {
            if (_derivedTypes[i].AssemblyQualifiedName == qualifiedName)
            {
                _selectedIndex = i;
                return;
            }
        }

        _selectedIndex = _derivedTypes.Length;
        stored.stringValue = "null";
    }
}

#endif