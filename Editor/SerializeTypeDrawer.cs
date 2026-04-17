#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using Type = System.Type;

[CustomPropertyDrawer(typeof(SerializeType<>), true)]
public class SerializeTypeDrawer : PropertyDrawer
{
    private Type[] _derivedTypes;
    private string[] _optionLabels;
    private int _selectedIndex;
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2; 
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        var storedProperty = property.FindPropertyRelative("qualifiedName");
        var storedScript = property.FindPropertyRelative("Script");
        var qualifiedName = storedProperty.stringValue;

        if (_optionLabels == null)
        {
            Initialize(property, storedProperty);
        }
        else if (_selectedIndex == _derivedTypes.Length)
        {
            if (qualifiedName != "null")
            {
                UpdateIndex(storedProperty);
            }
        }
        else
        {
            if (qualifiedName != _derivedTypes[_selectedIndex].AssemblyQualifiedName)
            {
                UpdateIndex(storedProperty);
            }
        }

        var propLabel = EditorGUI.BeginProperty(position, label, property);

        // First line: MonoScript field
        var scriptRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(scriptRect, storedScript, typeof(MonoScript), new GUIContent("Script"));
        if (EditorGUI.EndChangeCheck())
        {
            var script = storedScript.objectReferenceValue as MonoScript;
            var cls = script?.GetClass();
            storedProperty.stringValue = cls?.AssemblyQualifiedName ?? "null";
        }

        // Second line: Dropdown field
        var dropdownRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.BeginChangeCheck();
        _selectedIndex = EditorGUI.Popup(dropdownRect, property.name, _selectedIndex, _optionLabels);
        if (EditorGUI.EndChangeCheck())
        {
            storedProperty.stringValue = _selectedIndex < _derivedTypes.Length
                ? _derivedTypes[_selectedIndex].AssemblyQualifiedName
                : "null";

            // 尝试找到所选类型对应的 MonoScript 并赋值给 Script 属性
            if (_selectedIndex < _derivedTypes.Length)
            {
                storedScript.objectReferenceValue = FindMonoScript(_derivedTypes[_selectedIndex]);
            }
            else
            {
                storedScript.objectReferenceValue = null;
            }
        }

        EditorGUI.EndProperty();
    }

    private static Type[] FindAllDerivedTypes(Type baseType)
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        var types = baseType.Assembly.GetTypes();
        var typesEnum = types.Where(t => baseType.IsAssignableFrom(t));
        return typesEnum.ToArray<Type>();
    }

    private void Initialize(SerializedProperty property, SerializedProperty stored)
    {
        var baseTypeProperty = property.FindPropertyRelative("baseTypeName");
        var baseType = Type.GetType(baseTypeProperty.stringValue);

        _derivedTypes = FindAllDerivedTypes(baseType);

        if (_derivedTypes.Length == 0)
        {
            _optionLabels = new[] { new string($"No types derived from {baseType.Name} found.") };
            return;
        }

        _optionLabels = new string[_derivedTypes.Length + 1];
        for (var i = 0; i < _derivedTypes.Length; i++)
        {
            _optionLabels[i] = new string(_derivedTypes[i].Name);
        }

        _optionLabels[_derivedTypes.Length] = new string("null");

        UpdateIndex(stored);
    }

    private void UpdateIndex(SerializedProperty stored)
    {
        var qualifiedName = stored.stringValue;

        for (var i = 0; i < _derivedTypes.Length; i++)
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

    /// <summary>
    /// 通过 Type 查找对应的 MonoScript 资产
    /// </summary>
    private static MonoScript FindMonoScript(Type type)
    {
        if (type == null) return null;

        string[] guids = AssetDatabase.FindAssets(type.Name + " t:script");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);
            if (filename == type.Name)
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return script;
            }
        }

        return null;
    }
}

#endif