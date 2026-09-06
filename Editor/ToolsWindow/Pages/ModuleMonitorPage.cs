using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// 模块监控页面 — 展示所有 IModule 实例的状态与调试信息
    /// </summary>
    public class ModuleMonitorPage : KToolPageBase
    {
        public override string PageName => "Module";
        public override string Kit => "调试";
        public override int Priority => 1;

        private Vector2 _listScroll, _detailScroll;
        private string _searchFilter = "";

        private IModule _selectedModule;
        private readonly List<IModule> _moduleList = new();

        public override void OnToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (!Application.isPlaying)
                {
                    GUILayout.Label("仅 Play Mode 可用", EditorStyles.miniLabel);
                }
                else
                {
                    int count = KGameCore.Instance?.ModuleCount ?? 0;
                    GUILayout.Label($"已注册模块: {count}", EditorStyles.miniLabel);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    RefreshModuleList();
                    RepaintWindow();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnListGUI()
        {
            if (!Application.isPlaying || KGameCore.Instance == null)
            {
                EditorGUILayout.HelpBox("Module Monitor 仅 Play Mode 可用。", MessageType.Info);
                return;
            }

            InitStyles();
            RefreshModuleList();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            int count = 0;
            foreach (var module in _moduleList)
            {
                if (!FilterModule(module)) continue;
                DrawModuleRow(module);
                count++;
            }

            if (count == 0)
                EditorGUILayout.HelpBox(string.IsNullOrEmpty(_searchFilter) ? "没有已注册的模块" : "没有匹配的模块", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        public override void OnDetailGUI()
        {
            if (!Application.isPlaying || KGameCore.Instance == null) return;

            if (_selectedModule != null)
                DrawModuleDetail();
            else
                EditorGUILayout.HelpBox("选择左侧模块查看详情", MessageType.Info);
        }

        private void DrawModuleRow(IModule module)
        {
            bool selected = _selectedModule == module;
            var style = selected ? SelectedEntryStyle : EntryStyle;

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(28));
            {
                // 状态色条
                var colorRect = GUILayoutUtility.GetRect(4, 28, GUILayout.Width(4));
                EditorGUI.DrawRect(colorRect, module.Initialized ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.3f, 0.3f));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(module.GetType().Name, EditorStyles.boldLabel);

                var summary = (module as IModuleDebugInfo)?.DebugSummary;
                var subInfo = summary ?? $"Order: {module.Order}  {(module.Persistent ? "[持久]" : "")}";
                EditorGUILayout.LabelField(subInfo, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                DrawBadge(module.Initialized, "Init");
                DrawBadge(module.Persistent, "Persist");
            }
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedModule = module;
                Event.current.Use();
                RepaintWindow();
            }
        }

        private void DrawModuleDetail()
        {
            var module = _selectedModule;
            if (module == null) return;

            var type = module.GetType();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(type.Name, HeaderStyle);
            EditorGUILayout.LabelField($"FullName: {type.FullName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Initialized: {(module.Initialized ? "✓" : "✗")}  Order: {module.Order}  Persistent: {(module.Persistent ? "✓" : "✗")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            if (module is IModuleDebugInfo debugInfo)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("自定义调试", HeaderStyle);
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                debugInfo.OnDrawModuleDebugGUI();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox($"模块 {type.Name} 未实现 IModuleDebugInfo，无自定义调试信息。", MessageType.Info);
            }
        }

        private static void DrawBadge(bool value, string label)
        {
            var color = value ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
            var r = GUILayoutUtility.GetRect(36, 16, GUILayout.Width(36));
            EditorGUI.DrawRect(r, color);
            GUI.Label(r, label, new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = value ? Color.white : Color.gray }
            });
        }

        private bool FilterModule(IModule module)
        {
            if (string.IsNullOrEmpty(_searchFilter)) return true;
            var lower = _searchFilter.ToLower();
            return module.GetType().Name.ToLower().Contains(lower);
        }

        private void RefreshModuleList()
        {
            _moduleList.Clear();
            if (KGameCore.Instance == null) return;
            foreach (var m in KGameCore.Instance.AllModules)
            {
                if (m != null)
                    _moduleList.Add(m);
            }
            _moduleList.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private void RepaintWindow()
        {
            EditorWindow.GetWindow<KFrameworkToolsWindow>()?.Repaint();
        }
    }
}
