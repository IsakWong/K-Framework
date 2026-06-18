using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// Unit 实体监控页面 — 展示所有 UnitBase 实例的生命周期状态
    /// </summary>
    public class UnitMonitorPage : KToolPageBase
    {
        public override string PageName => "Unit";
        public override string Kit => "调试";
        public override int Priority => 2;

        private Vector2 _listScroll, _detailScroll;
        private string _searchFilter = "";
        private UnitLifecycleState _stateFilter = UnitLifecycleState.Alive;
        private bool _showAll = true;

        private UnitBase _selectedUnit;
        private UnityEditor.Editor _selectedEditor;

        private readonly Dictionary<UnitLifecycleState, int> _stateCounts = new();

        public override void OnDisable()
        {
            DestroySelectedEditor();
            base.OnDisable();
        }

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
                    var stats = UnitModule.Instance?.GetStatistics();
                    if (stats != null)
                    {
                        var firstLine = stats.Split('\n').FirstOrDefault() ?? "";
                        GUILayout.Label(firstLine, EditorStyles.miniLabel);
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    RepaintWindow();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

                _showAll = GUILayout.Toggle(_showAll, "全部", EditorStyles.toolbarButton, GUILayout.Width(50));
                if (!_showAll)
                {
                    _stateFilter = (UnitLifecycleState)EditorGUILayout.EnumPopup(_stateFilter, GUILayout.Width(100));
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnListGUI()
        {
            if (!Application.isPlaying || UnitModule.Instance == null)
            {
                EditorGUILayout.HelpBox("Unit Monitor 仅 Play Mode 可用。", MessageType.Info);
                return;
            }

            InitStyles();
            CountUnitsByState();

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            var units = UnitModule.Instance.UnitList
                .Where(u => u != null)
                .Where(FilterUnit)
                .OrderBy(u => u.LifecycleState)
                .ThenBy(u => u.name);

            int count = 0;
            foreach (var unit in units)
            {
                DrawUnitRow(unit);
                count++;
            }

            if (count == 0)
                EditorGUILayout.HelpBox("没有匹配的 Unit", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        public override void OnDetailGUI()
        {
            if (!Application.isPlaying || UnitModule.Instance == null)
                return;

            DrawStatistics();
            EditorGUILayout.Space(4);

            if (_selectedUnit != null)
                DrawUnitDetail();
        }

        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("状态统计", HeaderStyle);

            EditorGUILayout.BeginHorizontal();
            foreach (UnitLifecycleState state in System.Enum.GetValues(typeof(UnitLifecycleState)))
            {
                int count = _stateCounts.GetValueOrDefault(state);
                if (count == 0) continue;

                GUI.backgroundColor = GetStateColor(state);
                if (GUILayout.Button($"{state}\n{count}", GUILayout.Height(36), GUILayout.MinWidth(55)))
                {
                    _stateFilter = state;
                    _showAll = false;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawUnitDetail()
        {
            var unit = _selectedUnit;
            if (unit == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(unit.name, HeaderStyle);
            EditorGUILayout.LabelField($"类型: {unit.GetType().Name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"状态: {unit.LifecycleState}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Logic: {(unit.EnableOnLogic ? "✓" : "✗")}  Spawned: {(unit.IsSpawned ? "✓" : "✗")}  Alive: {(unit.IsAlive ? "✓" : "✗")}");

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (unit.IsAlive && GUILayout.Button("杀死 (Die)", GUILayout.Height(24)))
                unit.Die();
            if (GUILayout.Button("选中", GUILayout.Height(24)))
            {
                Selection.activeGameObject = unit.gameObject;
                EditorGUIUtility.PingObject(unit.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            if (_selectedEditor != null)
            {
                EditorGUILayout.Space(4);
                _selectedEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawUnitRow(UnitBase unit)
        {
            bool selected = _selectedUnit == unit;
            var style = selected ? SelectedEntryStyle : EntryStyle;

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(28));
            {
                // 状态色条
                var colorRect = GUILayoutUtility.GetRect(4, 28, GUILayout.Width(4));
                EditorGUI.DrawRect(colorRect, GetStateColor(unit.LifecycleState));

                // 名称 + 类型
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(unit.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(unit.GetType().Name, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // 状态徽章
                GUI.color = GetStateColor(unit.LifecycleState);
                GUILayout.Box(unit.LifecycleState.ToString(), GUILayout.Width(70), GUILayout.Height(20));
                GUI.color = Color.white;

                DrawBadge(unit.EnableOnLogic, "L");
                DrawBadge(unit.IsSpawned, "S");
                DrawBadge(unit.IsAlive, "A");
            }
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectUnit(unit);
                Event.current.Use();
            }
        }

        private static void DrawBadge(bool value, string label)
        {
            var color = value ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.4f, 0.4f, 0.4f);
            var r = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            EditorGUI.DrawRect(r, color);
            GUI.Label(r, label, new GUIStyle { alignment = TextAnchor.MiddleCenter, fontSize = 9, normal = { textColor = Color.white } });
        }

        private bool FilterUnit(UnitBase unit)
        {
            if (!_showAll && unit.LifecycleState != _stateFilter)
                return false;

            if (!string.IsNullOrEmpty(_searchFilter))
            {
                var lower = _searchFilter.ToLower();
                if (!unit.name.ToLower().Contains(lower) &&
                    !unit.GetType().Name.ToLower().Contains(lower))
                    return false;
            }

            return true;
        }

        private void CountUnitsByState()
        {
            _stateCounts.Clear();
            if (UnitModule.Instance == null) return;

            foreach (var unit in UnitModule.Instance.UnitList)
            {
                if (unit == null) continue;
                _stateCounts.TryGetValue(unit.LifecycleState, out int c);
                _stateCounts[unit.LifecycleState] = c + 1;
            }
        }

        private void SelectUnit(UnitBase unit)
        {
            if (_selectedUnit == unit) return;
            DestroySelectedEditor();
            _selectedUnit = unit;
            if (unit != null)
                _selectedEditor = UnityEditor.Editor.CreateEditor(unit);
            RepaintWindow();
        }

        private void DestroySelectedEditor()
        {
            if (_selectedEditor != null)
            {
                Object.DestroyImmediate(_selectedEditor);
                _selectedEditor = null;
            }
            _selectedUnit = null;
        }

        private static Color GetStateColor(UnitLifecycleState state)
        {
            switch (state)
            {
                case UnitLifecycleState.None: return new Color(0.5f, 0.5f, 0.5f);
                case UnitLifecycleState.Spawning: return new Color(1f, 0.8f, 0.2f);
                case UnitLifecycleState.Alive: return new Color(0.2f, 1f, 0.2f);
                case UnitLifecycleState.Dying: return new Color(1f, 0.5f, 0.2f);
                case UnitLifecycleState.Dead: return new Color(0.8f, 0.3f, 0.3f);
                case UnitLifecycleState.Deleting: return new Color(0.6f, 0.2f, 0.2f);
                case UnitLifecycleState.Deleted: return new Color(0.3f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }

        private void RepaintWindow()
        {
            EditorWindow.GetWindow<KFrameworkToolsWindow>()?.Repaint();
        }
    }
}
