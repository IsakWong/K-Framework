using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// 工具页面基类，提供订阅管理和常用编辑器工具方法
    /// </summary>
    public abstract class KToolPageBase : IKToolPage
    {
        public abstract string PageName { get; }
        public virtual string Kit => "System";
        public virtual int Priority => 0;

        protected readonly KEditorSubscriber Subscriber = new();

        protected GUIStyle HeaderStyle;
        protected GUIStyle EntryStyle;
        protected GUIStyle SelectedEntryStyle;
        protected bool StylesInited;

        public virtual void OnEnable() { }

        public virtual void OnDisable()
        {
            Subscriber.Clear();
        }

        public virtual void OnToolbarGUI() { }

        public abstract void OnListGUI();
        public abstract void OnDetailGUI();

        protected void InitStyles()
        {
            if (StylesInited) return;
            StylesInited = true;

            HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 4, 2)
            };

            EntryStyle = new GUIStyle("CN Box")
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(0, 0, 1, 1),
            };

            SelectedEntryStyle = new GUIStyle(EntryStyle);
            SelectedEntryStyle.normal.background = MakeTex(1, 1, new Color(0.24f, 0.48f, 0.9f, 0.35f));
        }

        protected static Texture2D MakeTex(int w, int h, Color color)
        {
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        protected static string GetAssetRelPath(string path, string baseFolder)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (!string.IsNullOrEmpty(baseFolder) && path.StartsWith(baseFolder + "/"))
                path = path.Substring(baseFolder.Length + 1);
            return path.Contains('/') ? path.Substring(0, path.LastIndexOf('/')) : "";
        }

        protected static void DrawEntryItem(UnityEngine.Object asset, UnityEngine.Object selectedAsset,
            Action<UnityEngine.Object> onSelect, GUIStyle normalStyle, GUIStyle selectedStyle,
            string displayName = null, string subInfo = null, Texture icon = null)
        {
            if (asset == null) return;

            bool selected = selectedAsset == asset;
            var style = selected ? selectedStyle : normalStyle;
            string label = displayName ?? asset.name;

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(32));
            {
                if (icon != null)
                    GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));
                else
                {
                    var thumb = AssetPreview.GetMiniThumbnail(asset);
                    if (thumb != null)
                        GUILayout.Label(thumb, GUILayout.Width(24), GUILayout.Height(24));
                }

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(subInfo))
                    EditorGUILayout.LabelField(subInfo, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                onSelect?.Invoke(asset);
                if (Event.current.clickCount == 2)
                    AssetDatabase.OpenAsset(asset);
                Event.current.Use();
            }
        }

        protected static void DrawDetailHeader(UnityEngine.Object asset, string typeLabel = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(asset.name, EditorStyles.boldLabel);
            if (typeLabel != null)
                GUILayout.Label(typeLabel, EditorStyles.miniLabel, GUILayout.Width(100));
            if (GUILayout.Button("定位", GUILayout.Width(40)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("选中", GUILayout.Width(40)))
                Selection.activeObject = asset;
            if (GUILayout.Button("打开", GUILayout.Width(40)))
                AssetDatabase.OpenAsset(asset);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset), EditorStyles.miniLabel);
        }

        protected static List<T> FindAllAssetsOfType<T>(string searchFolder = null) where T : UnityEngine.Object
        {
            var result = new List<T>();
            var filter = $"t:{typeof(T).Name}";
            var folders = searchFolder != null ? new[] { searchFolder } : null;

            foreach (var guid in AssetDatabase.FindAssets(filter, folders))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    result.Add(asset);
            }

            result.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return result;
        }
    }
}
