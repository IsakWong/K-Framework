#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor window for monitoring all UnitBase instances in the scene
/// </summary>
public class UnitMonitorWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private string _searchFilter = "";
    private UnitLifecycleState _stateFilter = UnitLifecycleState.Alive;
    private bool _showAll = true;
    private bool _showStatistics = true;
    private bool _autoRefresh = true;
    private double _lastRefreshTime;
    private float _refreshInterval = 0.1f;
    
    private GUIStyle _headerStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _miniButtonStyle;
    private GUIStyle _stateBoxStyle;
    
    private Dictionary<UnitLifecycleState, int> _stateCounts = new Dictionary<UnitLifecycleState, int>();

    [MenuItem("Tools/Unit Monitor Window", false, 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<UnitMonitorWindow>("Unit Monitor");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        _lastRefreshTime = EditorApplication.timeSinceStartup;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    private void InitStyles()
    {
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(5, 5, 10, 5)
            };
        }
        
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }
        
        if (_miniButtonStyle == null)
        {
            _miniButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 10
            };
        }
        
        if (_stateBoxStyle == null)
        {
            _stateBoxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }
    }

    private void OnGUI()
    {
        InitStyles();
        
        EditorGUILayout.BeginVertical();
        
        DrawToolbar();
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Unit Monitor is only available in Play Mode", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        
        if (UnitModule.Instance == null)
        {
            EditorGUILayout.HelpBox("UnitModule not initialized", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }
        
        DrawStatistics();
        DrawFilters();
        DrawUnitList();
        
        EditorGUILayout.EndVertical();
        
        // Auto refresh
        if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > _refreshInterval)
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        GUILayout.Label("?? Unit Monitor", EditorStyles.boldLabel);
        
        GUILayout.FlexibleSpace();
        
        _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(90));
        
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            Repaint();
        }
        
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            _searchFilter = "";
            _showAll = true;
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatistics()
    {
        if (!_showStatistics) return;
        
        EditorGUILayout.BeginVertical(_boxStyle);
        
        _showStatistics = EditorGUILayout.Foldout(_showStatistics, "?? Statistics", true, EditorStyles.foldoutHeader);
        
        if (_showStatistics && UnitModule.Instance != null)
        {
            var stats = UnitModule.Instance.GetStatistics();
            EditorGUILayout.LabelField(stats, EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(5);
            
            // Count units by state
            CountUnitsByState();
            
            EditorGUILayout.BeginHorizontal();
            foreach (UnitLifecycleState state in System.Enum.GetValues(typeof(UnitLifecycleState)))
            {
                int count = _stateCounts.ContainsKey(state) ? _stateCounts[state] : 0;
                if (count > 0)
                {
                    Color stateColor = GetStateColor(state);
                    GUI.backgroundColor = stateColor;
                    if (GUILayout.Button($"{state}\n{count}", GUILayout.Height(40), GUILayout.MinWidth(60)))
                    {
                        _stateFilter = state;
                        _showAll = false;
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void CountUnitsByState()
    {
        _stateCounts.Clear();
        
        if (UnitModule.Instance == null) return;
        
        foreach (var unit in UnitModule.Instance.UnitList)
        {
            if (unit == null) continue;
            
            if (!_stateCounts.ContainsKey(unit.LifecycleState))
            {
                _stateCounts[unit.LifecycleState] = 0;
            }
            _stateCounts[unit.LifecycleState]++;
        }
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginVertical(_boxStyle);
        
        EditorGUILayout.LabelField("?? Filters", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("State Filter:", GUILayout.Width(80));
        _showAll = GUILayout.Toggle(_showAll, "Show All", GUILayout.Width(80));
        if (!_showAll)
        {
            _stateFilter = (UnitLifecycleState)EditorGUILayout.EnumPopup(_stateFilter);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawUnitList()
    {
        if (UnitModule.Instance == null) return;
        
        EditorGUILayout.BeginVertical(_boxStyle);
        EditorGUILayout.LabelField("?? Units", EditorStyles.boldLabel);
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        var units = UnitModule.Instance.UnitList
            .Where(u => u != null)
            .Where(u => FilterUnit(u))
            .OrderBy(u => u.LifecycleState)
            .ThenBy(u => u.name);
        
        int displayedCount = 0;
        
        foreach (var unit in units)
        {
            DrawUnitRow(unit);
            displayedCount++;
        }
        
        if (displayedCount == 0)
        {
            EditorGUILayout.HelpBox("No units match the current filter", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private bool FilterUnit(UnitBase unit)
    {
        if (unit == null) return false;
        
        // State filter
        if (!_showAll && unit.LifecycleState != _stateFilter)
        {
            return false;
        }
        
        // Search filter
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            string lowerFilter = _searchFilter.ToLower();
            if (!unit.name.ToLower().Contains(lowerFilter) && 
                !unit.GetType().Name.ToLower().Contains(lowerFilter))
            {
                return false;
            }
        }
        
        return true;
    }

    private void DrawUnitRow(UnitBase unit)
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        
        // State indicator
        Color stateColor = GetStateColor(unit.LifecycleState);
        GUI.backgroundColor = stateColor;
        GUILayout.Box("", _stateBoxStyle, GUILayout.Width(5), GUILayout.Height(30));
        GUI.backgroundColor = Color.white;
        
        // Unit icon and name
        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();
        
        GUILayout.Label(GetUnitIcon(unit), GUILayout.Width(20));
        
        if (GUILayout.Button(unit.name, EditorStyles.label, GUILayout.Width(150)))
        {
            Selection.activeGameObject = unit.gameObject;
            SceneView.FrameLastActiveSceneView();
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Type name
        GUILayout.Label(unit.GetType().Name, EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        
        // State badge
        GUI.color = stateColor;
        GUILayout.Box(unit.LifecycleState.ToString(), _stateBoxStyle, GUILayout.Width(80), GUILayout.Height(30));
        GUI.color = Color.white;
        
        // Status indicators
        DrawStatusBadge("L", unit.EnableOnLogic, "Logic Enabled");
        DrawStatusBadge("S", unit.IsSpawned, "Spawned");
        DrawStatusBadge("A", unit.IsAlive, "Alive");
        
        // Actions
        GUI.enabled = unit.IsAlive;
        if (GUILayout.Button("Die", _miniButtonStyle, GUILayout.Width(40)))
        {
            unit.Die();
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("Select", _miniButtonStyle, GUILayout.Width(50)))
        {
            Selection.activeGameObject = unit.gameObject;
            EditorGUIUtility.PingObject(unit.gameObject);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusBadge(string label, bool value, string tooltip)
    {
        Color badgeColor = value ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
        GUI.backgroundColor = badgeColor;
        GUILayout.Box(new GUIContent(label, tooltip), _stateBoxStyle, GUILayout.Width(20), GUILayout.Height(20));
        GUI.backgroundColor = Color.white;
    }

    private string GetUnitIcon(UnitBase unit)
    {
        if (unit.name.Contains("Player")) return "??";
        if (unit.name.Contains("Enemy")) return "??";
        if (unit.name.Contains("Projectile")) return "??";
        return "??";
    }

    private Color GetStateColor(UnitLifecycleState state)
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
}
#endif
