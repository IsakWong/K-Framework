using UnityEngine;
using System.Text;

/// <summary>
/// Runtime debug overlay for displaying unit statistics
/// Attach this to a GameObject in the scene
/// </summary>
public class UnitDebugOverlay : MonoBehaviour
{
    [Header("Display Settings")]
    public bool ShowOverlay = true;
    public KeyCode ToggleKey = KeyCode.F1;
    public bool ShowDetailed = false;
    public int MaxUnitsToShow = 10;
    
    [Header("Position")]
    public Vector2 Position = new Vector2(10, 10);
    public int Width = 400;
    public int FontSize = 12;
    
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _headerStyle;
    private StringBuilder _sb = new StringBuilder();
    
    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey))
        {
            ShowOverlay = !ShowOverlay;
        }
    }

    private void OnGUI()
    {
        if (!ShowOverlay) return;
        if (UnitModule.Instance == null) return;
        
        InitStyles();
        
        // Main box
        Rect boxRect = new Rect(Position.x, Position.y, Width, Screen.height - Position.y * 2);
        GUILayout.BeginArea(boxRect);
        GUILayout.BeginVertical(_boxStyle);
        
        // Header
        GUILayout.Label("?? Unit Debug Overlay", _headerStyle);
        GUILayout.Label($"Press {ToggleKey} to toggle", _labelStyle);
        
        GUILayout.Space(10);
        
        // Statistics
        DrawStatistics();
        
        GUILayout.Space(10);
        
        // Detailed info toggle
        ShowDetailed = GUILayout.Toggle(ShowDetailed, "Show Detailed Info");
        
        if (ShowDetailed)
        {
            GUILayout.Space(5);
            DrawDetailedInfo();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void InitStyles()
    {
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15)
            };
        }
        
        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize + 4,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = FontSize,
                normal = { textColor = Color.white },
                richText = true
            };
        }
    }

    private void DrawStatistics()
    {
        var stats = UnitModule.Instance.GetStatistics();
        
        GUILayout.Label("?? <b>Statistics</b>", _labelStyle);
        GUILayout.Label(stats, _labelStyle);
        
        GUILayout.Space(5);
        
        // State breakdown
        var stateCounts = CountUnitsByState();
        GUILayout.Label("?? <b>State Breakdown:</b>", _labelStyle);
        
        foreach (var kvp in stateCounts)
        {
            if (kvp.Value > 0)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(GetStateColor(kvp.Key));
                GUILayout.Label($"  <color=#{colorHex}>¡ñ</color> {kvp.Key}: <b>{kvp.Value}</b>", _labelStyle);
            }
        }
    }

    private void DrawDetailedInfo()
    {
        GUILayout.Label("?? <b>Recent Units:</b>", _labelStyle);
        
        var units = UnitModule.Instance.UnitList;
        int count = 0;
        
        foreach (var unit in units)
        {
            if (unit == null) continue;
            if (count >= MaxUnitsToShow) break;
            
            Color stateColor = GetStateColor(unit.LifecycleState);
            string colorHex = ColorUtility.ToHtmlStringRGB(stateColor);
            
            _sb.Clear();
            _sb.Append($"  <color=#{colorHex}>¡ñ</color> ");
            _sb.Append($"<b>{unit.name}</b> ");
            _sb.Append($"({unit.GetType().Name}) - ");
            _sb.Append($"<color=#{colorHex}>{unit.LifecycleState}</color>");
            
            if (unit.EnableOnLogic)
            {
                _sb.Append(" ??");
            }
            
            GUILayout.Label(_sb.ToString(), _labelStyle);
            
            count++;
        }
        
        if (units.Count > MaxUnitsToShow)
        {
            GUILayout.Label($"  ... and {units.Count - MaxUnitsToShow} more", _labelStyle);
        }
    }

    private System.Collections.Generic.Dictionary<UnitLifecycleState, int> CountUnitsByState()
    {
        var counts = new System.Collections.Generic.Dictionary<UnitLifecycleState, int>();
        
        foreach (UnitLifecycleState state in System.Enum.GetValues(typeof(UnitLifecycleState)))
        {
            counts[state] = 0;
        }
        
        foreach (var unit in UnitModule.Instance.UnitList)
        {
            if (unit == null) continue;
            counts[unit.LifecycleState]++;
        }
        
        return counts;
    }

    private Color GetStateColor(UnitLifecycleState state)
    {
        switch (state)
        {
            case UnitLifecycleState.None: return Color.gray;
            case UnitLifecycleState.Spawning: return Color.yellow;
            case UnitLifecycleState.Alive: return Color.green;
            case UnitLifecycleState.Dying: return new Color(1f, 0.5f, 0f);
            case UnitLifecycleState.Dead: return Color.red;
            case UnitLifecycleState.Deleting: return new Color(0.8f, 0.2f, 0.2f);
            case UnitLifecycleState.Deleted: return new Color(0.5f, 0.5f, 0.5f);
            default: return Color.white;
        }
    }
}
