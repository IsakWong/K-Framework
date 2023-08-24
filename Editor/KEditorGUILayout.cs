#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace K1.Editor
{
    public class KEditorGUILayout
    {
        static public void ObjectPropertyField(object target)
        {
            var tp = target.GetType();
            var fields = tp.GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                      BindingFlags.Instance | BindingFlags.Static);

            foreach (var propInfo in fields)
            {
                var serializableAttribute =
                    propInfo.GetCustomAttribute(typeof(SerializableAttribute), false) as SerializableAttribute;
                var showEditorAttribute =
                    propInfo.GetCustomAttribute(typeof(ShowNonSerializedPropertyAttribute), false) as
                        ShowNonSerializedPropertyAttribute;

                if (serializableAttribute != null || showEditorAttribute != null)
                {
                    ObjectPropertyField(propInfo, target);
                }
            }
        }

        public static void ObjectPropertyField(FieldInfo info, object target)
        {
            if (info.FieldType == typeof(bool))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.Toggle(info.Name, (bool)v);
                if ((bool)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            if (info.FieldType == typeof(string))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.TextField(info.Name, (string)v);
                if ((string)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            if (info.FieldType == typeof(float))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.FloatField(info.Name, (float)v);
                if ((float)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            if (info.FieldType == typeof(Vector2))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.Vector2Field(info.Name, (Vector2)v);
                if ((Vector2)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            if (info.FieldType.IsSubclassOf(typeof(Enum)))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.EnumPopup(info.Name, (Enum)v);
                if ((Enum)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            if (info.FieldType == typeof(Vector3))
            {
                var v = info.GetValue(target);
                var newV = EditorGUILayout.Vector3Field(info.Name, (Vector3)v);
                if ((Vector3)v != newV)
                    info.SetValue(target, newV);
                return;
            }

            object fieldValue = info.GetValue(target);
            if (fieldValue == null)
                return;

            var tp = fieldValue.GetType();
            if (EditorGUILayout.Foldout(true, info.Name))
            {
                var fields = tp.GetFields(BindingFlags.Public |
                                          BindingFlags.Instance |
                                          BindingFlags.Static);
                foreach (var propInfo in fields)
                {
                    var serializableAttribute =
                        propInfo.GetCustomAttribute(typeof(SerializableAttribute), false) as SerializableAttribute;
                    var showEditorAttribute =
                        propInfo.GetCustomAttribute(typeof(ShowNonSerializedPropertyAttribute), false) as
                            ShowNonSerializedPropertyAttribute;
                    if (serializableAttribute != null || showEditorAttribute != null)
                    {
                        ObjectPropertyField(propInfo, fieldValue);
                    }
                }

                do
                {
                    fields = tp.GetFields(BindingFlags.NonPublic |
                                          BindingFlags.Instance |
                                          BindingFlags.Static);
                    foreach (var propInfo in fields)
                    {
                        var serializableAttribute =
                            propInfo.GetCustomAttribute(typeof(SerializableAttribute), false) as SerializableAttribute;
                        var showEditorAttribute =
                            propInfo.GetCustomAttribute(typeof(ShowNonSerializedPropertyAttribute), false) as
                                ShowNonSerializedPropertyAttribute;
                        if (serializableAttribute != null || showEditorAttribute != null)
                        {
                            ObjectPropertyField(propInfo, fieldValue);
                        }
                    }

                    tp = tp.BaseType;
                } while (tp != null);
            }
        }
    }
}
#endif