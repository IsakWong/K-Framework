#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for UnitBase with enhanced debugging and visualization
/// </summary>
[CustomEditor(typeof(UnitBase), true)]
[CanEditMultipleObjects]
public class UnitBaseEditor : OdinEditor
{
    public override void OnInspectorGUI()
    {
       DrawDefaultInspector();
    }
}
#endif
