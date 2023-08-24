#if UNITY_EDITOR
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace K1.Editor
{
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
                    var outVar = JsonConvert.DeserializeObject<bool>(valueString.stringValue);
                    var newVar = EditorGUI.Toggle(rect, "Value", outVar);
                    if (newVar != outVar)
                    {
                        valueString.stringValue = JsonConvert.SerializeObject(newVar);
                    }

                    break;
                }
                case VariantType.Float:
                {
                    var outVar = JsonConvert.DeserializeObject<float>(valueString.stringValue);
                    var newVar = EditorGUI.FloatField(rect, "Value", outVar);
                    if (newVar != outVar)
                    {
                        valueString.stringValue = JsonConvert.SerializeObject(newVar);
                    }

                    break;
                }
                case VariantType.Int:
                {
                    var outVar = JsonConvert.DeserializeObject<int>(valueString.stringValue);
                    var newVar = EditorGUI.IntField(rect, "Value", outVar);
                    if (newVar != outVar)
                    {
                        valueString.stringValue = JsonConvert.SerializeObject(newVar);
                    }

                    break;
                }
                case VariantType.Vector2:
                {
                    var outVar =
                        JsonConvert.DeserializeObject<Vector2>(valueString.stringValue);
                    var newVar = EditorGUI.Vector2Field(rect, "Value", outVar);
                    if (newVar != outVar)
                        valueString.stringValue = JsonConvert.SerializeObject(newVar);

                    break;
                }
                case VariantType.Vector3:
                {
                    var outVar =
                        JsonConvert.DeserializeObject<Vector3>(valueString.stringValue);
                    outVar = EditorGUI.Vector3Field(rect, "Value", outVar);
                    valueString.stringValue = JsonUtility.ToJson(outVar);
                    break;
                }
                case VariantType.Vector4:
                {
                    var outVar =
                        JsonConvert.DeserializeObject<Vector4>(valueString.stringValue);
                    var newVar = EditorGUI.Vector4Field(rect, "Value", outVar);
                    if (newVar != outVar)
                        valueString.stringValue = JsonConvert.SerializeObject(newVar);
                    break;
                }
                case VariantType.UnityObject:
                {
                    var outVar = objectProp.objectReferenceValue;
                    var newVar = EditorGUI.ObjectField(rect, "Value", outVar, typeof(Object));
                    if (newVar != outVar)
                        objectProp.objectReferenceValue = newVar;
                    break;
                }
                case VariantType.List:
                {
                }
                    break;
            }

            EditorGUI.EndProperty();
        }
    }
}

#endif