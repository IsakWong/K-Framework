#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for UnityUnit with enhanced debugging and visualization.
/// 内核 UnitBase 已非 UnityEngine.Object，Inspector 面板落在表现宿主 UnityUnit 上。
/// </summary>
[CustomEditor(typeof(UnityUnit), true)]
[CanEditMultipleObjects]
public class UnitBaseEditor : OdinEditor
{
    public override void OnInspectorGUI()
    {
       DrawDefaultInspector();
    }
}
#endif
