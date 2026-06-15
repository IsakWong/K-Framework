// Copyright (c) 2026 NetEase Inc. All rights reserved.
// Author: wangyunfei02
// Date: 2026/01/08

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.DebugGUI
{
    /// <summary>
    /// 基于 IMGUI 的游戏内 Debug 系统
    /// 提供简化的 API 来快速构建调试界面
    /// </summary>
    public class DebugGUI : MonoBehaviour
    {
        #region Singleton
        
        private static DebugGUI _instance;
        public static DebugGUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[DebugGUI]");
                    _instance = go.AddComponent<DebugGUI>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion

        #region Settings

        [Header("显示设置")]
        public bool ShowDebugPanel = true;
        public KeyCode ToggleKey = KeyCode.F1;
        
        [Header("样式设置")]
        public int FontSize = 14;
        public Color BackgroundColor = new Color(0, 0, 0, 0.8f);
        public Color PrimaryColor = new Color(0.2f, 0.6f, 1f, 1f);
        public Color SuccessColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        public Color WarningColor = new Color(1f, 0.8f, 0.2f, 1f);
        public Color ErrorColor = new Color(1f, 0.2f, 0.2f, 1f);

        #endregion

        #region Private Fields

        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _boxStyle;
        
        private bool _stylesInitialized = false;
        private Dictionary<string, DebugWindow> _windows = new Dictionary<string, DebugWindow>();
        private List<DebugLogEntry> _logs = new List<DebugLogEntry>();
        private int _maxLogCount = 100;
        
        // 世界空间 GUI 列表
        private List<WorldGUI> _worldGUIs = new List<WorldGUI>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Application.logMessageReceived += HandleLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                ShowDebugPanel = !ShowDebugPanel;
            }
        }

        private void OnGUI()
        {
            
            if (!ShowDebugPanel) return;

            InitializeStyles();
            
            ShowLogWindow();
            
            // 绘制所有调试窗口
            foreach (var window in _windows.Values)
            {
                if (window.IsVisible)
                {
                    window.WindowRect = GUILayout.Window(
                        window.WindowId,
                        window.WindowRect,
                        DrawWindow,
                        window.Title,
                        _windowStyle
                    );
                }
            }
            
            // 绘制世界空间 GUI
            DrawWorldGUIs();
        }

        #endregion

        #region Style Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            // Window Style
            _windowStyle = new GUIStyle(GUI.skin.window);
            _windowStyle.fontSize = FontSize;
            _windowStyle.normal.background = MakeTex(2, 2, BackgroundColor);

            // Label Style
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = FontSize;
            _labelStyle.normal.textColor = Color.white;

            // Button Style
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = FontSize;
            _buttonStyle.normal.background = MakeTex(2, 2, PrimaryColor);

            // Toggle Style
            _toggleStyle = new GUIStyle(GUI.skin.toggle);
            _toggleStyle.fontSize = FontSize;

            // TextField Style
            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.fontSize = FontSize;

            // Box Style
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.fontSize = FontSize;
            _boxStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        #endregion

        #region Window Management

        private void DrawWindow(int windowId)
        {
            var window = GetWindowById(windowId);
            if (window == null) return;

            // 内容区域由 DrawContent 自行管理滚动，不在此处包裹 ScrollView
            window.DrawContent?.Invoke();

            GUI.DragWindow(new Rect(0, 0, window.WindowRect.width, 20));
        }

        private DebugWindow GetWindowById(int id)
        {
            foreach (var window in _windows.Values)
            {
                if (window.WindowId == id)
                    return window;
            }
            return null;
        }

        #endregion

        #region Static API - Window Management

        /// <summary>
        /// 创建或获取调试窗口
        /// </summary>
        public static DebugWindow Window(string title, float x = 10, float y = 10, float width = 300, float height = 400)
        {
            if (Instance._windows.TryGetValue(title, out var window))
            {
                return window;
            }

            window = new DebugWindow
            {
                Title = title,
                WindowId = Instance._windows.Count,
                WindowRect = new Rect(x, y, width, height),
                IsVisible = true
            };

            Instance._windows[title] = window;
            return window;
        }

        /// <summary>
        /// 显示窗口
        /// </summary>
        public static void ShowWindow(string title)
        {
            if (Instance._windows.TryGetValue(title, out var window))
            {
                window.IsVisible = true;
            }
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public static void HideWindow(string title)
        {
            if (Instance._windows.TryGetValue(title, out var window))
            {
                window.IsVisible = false;
            }
        }

        /// <summary>
        /// 移除窗口
        /// </summary>
        public static void RemoveWindow(string title)
        {
            Instance._windows.Remove(title);
        }

        #endregion

        #region Static API - UI Elements

        /// <summary>
        /// 绘制标签
        /// </summary>
        public static void Label(string text, Color? color = null)
        {
            var oldColor = GUI.color;
            if (color.HasValue)
                GUI.color = color.Value;
            
            GUILayout.Label(text, Instance._labelStyle);
            
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制按钮
        /// </summary>
        public static bool Button(string text, float? width = null, float? height = null)
        {
            if (width.HasValue && height.HasValue)
                return GUILayout.Button(text, Instance._buttonStyle, GUILayout.Width(width.Value), GUILayout.Height(height.Value));
            else if (width.HasValue)
                return GUILayout.Button(text, Instance._buttonStyle, GUILayout.Width(width.Value));
            else if (height.HasValue)
                return GUILayout.Button(text, Instance._buttonStyle, GUILayout.Height(height.Value));
            else
                return GUILayout.Button(text, Instance._buttonStyle);
        }

        /// <summary>
        /// 绘制切换按钮
        /// </summary>
        public static bool Toggle(string label, bool value)
        {
            return GUILayout.Toggle(value, label, Instance._toggleStyle);
        }

        /// <summary>
        /// 绘制文本框
        /// </summary>
        public static string TextField(string text, float? width = null)
        {
            if (width.HasValue)
                return GUILayout.TextField(text, Instance._textFieldStyle, GUILayout.Width(width.Value));
            else
                return GUILayout.TextField(text, Instance._textFieldStyle);
        }

        /// <summary>
        /// 绘制滑块
        /// </summary>
        public static float Slider(float value, float min, float max, string label = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                GUILayout.BeginHorizontal();
                Label(label);
                value = GUILayout.HorizontalSlider(value, min, max);
                Label(value.ToString("F2"));
                GUILayout.EndHorizontal();
            }
            else
            {
                value = GUILayout.HorizontalSlider(value, min, max);
            }
            return value;
        }

        /// <summary>
        /// 绘制水平分隔线
        /// </summary>
        public static void Separator()
        {
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        }

        /// <summary>
        /// 开始水平布局
        /// </summary>
        public static void BeginHorizontal()
        {
            GUILayout.BeginHorizontal();
        }

        /// <summary>
        /// 结束水平布局
        /// </summary>
        public static void EndHorizontal()
        {
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 开始垂直布局
        /// </summary>
        public static void BeginVertical()
        {
            GUILayout.BeginVertical(Instance._boxStyle);
        }

        /// <summary>
        /// 结束垂直布局
        /// </summary>
        public static void EndVertical()
        {
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 空白间距
        /// </summary>
        public static void Space(float pixels = 10)
        {
            GUILayout.Space(pixels);
        }

        #endregion

        #region Static API - World Space GUI

        /// <summary>
        /// 添加一个跟随 Transform 的世界空间 GUI 元素
        /// </summary>
        /// <param name="transform">要跟随的 Transform</param>
        /// <param name="drawCallback">在该位置绘制 GUI 的回调</param>
        /// <param name="offset">相对于 Transform 的偏移量（世界空间）</param>
        /// <returns>GUI ID，用于后续移除</returns>
        public static int AddWorldGUI(Transform transform, Action drawCallback, Vector3 offset = default)
        {
            var worldGUI = new WorldGUI
            {
                Transform = transform,
                DrawCallback = drawCallback,
                Offset = offset,
                Id = Instance._worldGUIs.Count
            };
            
            Instance._worldGUIs.Add(worldGUI);
            return worldGUI.Id;
        }

        /// <summary>
        /// 添加一个固定世界坐标的 GUI 元素
        /// </summary>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="drawCallback">在该位置绘制 GUI 的回调</param>
        /// <returns>GUI ID，用于后续移除</returns>
        public static int AddWorldGUI(Vector3 worldPosition, Action drawCallback)
        {
            var worldGUI = new WorldGUI
            {
                WorldPosition = worldPosition,
                DrawCallback = drawCallback,
                Id = Instance._worldGUIs.Count,
                UseFixedPosition = true
            };
            
            Instance._worldGUIs.Add(worldGUI);
            return worldGUI.Id;
        }

        /// <summary>
        /// 添加一个跟随 Transform 的世界空间 GUI 元素（带区域大小配置）
        /// </summary>
        /// <param name="transform">要跟随的 Transform</param>
        /// <param name="drawCallback">在该位置绘制 GUI 的回调</param>
        /// <param name="offset">相对于 Transform 的偏移量（世界空间）</param>
        /// <param name="areaSize">绘制区域大小</param>
        /// <param name="pivot">锚点 (0,0)=左上角 (0.5,0.5)=中心 (1,1)=右下角</param>
        /// <returns>GUI ID，用于后续移除</returns>
        public static int AddWorldGUI(Transform transform, Action drawCallback, Vector3 offset, Vector2 areaSize, Vector2 pivot)
        {
            var worldGUI = new WorldGUI
            {
                Transform = transform,
                DrawCallback = drawCallback,
                Offset = offset,
                AreaSize = areaSize,
                Pivot = pivot,
                Id = Instance._worldGUIs.Count
            };
            
            Instance._worldGUIs.Add(worldGUI);
            return worldGUI.Id;
        }

        /// <summary>
        /// 添加一个固定世界坐标的 GUI 元素（带区域大小配置）
        /// </summary>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="drawCallback">在该位置绘制 GUI 的回调</param>
        /// <param name="areaSize">绘制区域大小</param>
        /// <param name="pivot">锚点 (0,0)=左上角 (0.5,0.5)=中心 (1,1)=右下角</param>
        /// <returns>GUI ID，用于后续移除</returns>
        public static int AddWorldGUI(Vector3 worldPosition, Action drawCallback, Vector2 areaSize, Vector2 pivot)
        {
            var worldGUI = new WorldGUI
            {
                WorldPosition = worldPosition,
                DrawCallback = drawCallback,
                AreaSize = areaSize,
                Pivot = pivot,
                Id = Instance._worldGUIs.Count,
                UseFixedPosition = true
            };
            
            Instance._worldGUIs.Add(worldGUI);
            return worldGUI.Id;
        }

        /// <summary>
        /// 移除世界空间 GUI
        /// </summary>
        /// <param name="guiId">GUI ID</param>
        public static void RemoveWorldGUI(int guiId)
        {
            if (guiId >= 0 && guiId < Instance._worldGUIs.Count)
            {
                Instance._worldGUIs[guiId] = null;
            }
        }

        /// <summary>
        /// 清除所有世界空间 GUI
        /// </summary>
        public static void ClearWorldGUIs()
        {
            Instance._worldGUIs.Clear();
        }

        /// <summary>
        /// 绘制所有世界空间 GUI
        /// </summary>
        private void DrawWorldGUIs()
        {
            if (Camera.main == null) return;

            var camera = Camera.main;
            
            for (int i = _worldGUIs.Count - 1; i >= 0; i--)
            {
                var worldGUI = _worldGUIs[i];
                
                // 清理无效 GUI
                if (worldGUI == null || (!worldGUI.UseFixedPosition && worldGUI.Transform == null))
                {
                    _worldGUIs.RemoveAt(i);
                    continue;
                }

                // 获取世界坐标
                Vector3 worldPos = worldGUI.UseFixedPosition 
                    ? worldGUI.WorldPosition 
                    : worldGUI.Transform.position + worldGUI.Offset;

                // 转换为屏幕坐标
                Vector3 screenPos = camera.WorldToScreenPoint(worldPos);

                // 检查是否在摄像机前方
                if (screenPos.z < 0)
                    continue;

                // Unity GUI 的 Y 坐标是从上到下的，需要翻转
                screenPos.y = Screen.height - screenPos.y;
                
                // 根据锚点调整位置
                float offsetX = worldGUI.AreaSize.x * worldGUI.Pivot.x;
                float offsetY = worldGUI.AreaSize.y * worldGUI.Pivot.y;
                
                // 创建绘制区域
                Rect areaRect = new Rect(
                    screenPos.x - offsetX, 
                    screenPos.y - offsetY, 
                    worldGUI.AreaSize.x, 
                    worldGUI.AreaSize.y
                );
                
                GUILayout.BeginArea(areaRect);
                
                // 执行用户回调
                try
                {
                    worldGUI.DrawCallback?.Invoke();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"Error in WorldGUI callback: {e.Message}");
                }
                
                GUILayout.EndArea();
            }
        }

        #endregion

        #region Static API - Log System

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            var entry = new DebugLogEntry
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type,
                Time = DateTime.Now
            };

            _logs.Add(entry);

            if (_logs.Count > _maxLogCount)
            {
                _logs.RemoveAt(0);
            }
        }

        /// <summary>
        /// 显示日志窗口
        /// </summary>
        public static void ShowLogWindow()
        {
            var logWindow = Window("Console Logs", 10, 10, 500, 400);
            logWindow.DrawContent = () =>
            {
                if (Button("Clear Logs"))
                {
                    Instance._logs.Clear();
                }

                Separator();

                // 虚拟化滚动 - 只渲染可见的日志条目
                int totalLogs = Instance._logs.Count;
                if (totalLogs == 0) return;

                // 估算每个日志条目的高度（包括文本和间距）
                float itemHeight = Instance.FontSize + 6; // 文本高度 + 间距
                float visibleHeight = logWindow.WindowRect.height - 80; // 窗口高度 - 按钮和边距
                int visibleItemCount = Mathf.CeilToInt(visibleHeight / itemHeight) + 2; // +2 作为缓冲

                // 计算当前可见的起始和结束索引
                int startIndex = Mathf.FloorToInt(logWindow.ScrollPosition.y / itemHeight);
                startIndex = Mathf.Max(0, startIndex);
                int endIndex = Mathf.Min(totalLogs, startIndex + visibleItemCount);

                // 创建虚拟滚动区域
                logWindow.ScrollPosition = GUILayout.BeginScrollView(
                    logWindow.ScrollPosition,
                    GUILayout.Height(visibleHeight)
                );

                // 顶部填充空间
                if (startIndex > 0)
                {
                    GUILayout.Space(startIndex * itemHeight);
                }

                // 只渲染可见的日志条目
                for (int i = startIndex; i < endIndex; i++)
                {
                    var log = Instance._logs[i];
                    Color logColor = Color.white;
                    switch (log.Type)
                    {
                        case LogType.Error:
                        case LogType.Exception:
                            logColor = Instance.ErrorColor;
                            break;
                        case LogType.Warning:
                            logColor = Instance.WarningColor;
                            break;
                    }

                    Label($"[{log.Time:HH:mm:ss}] {log.Message}", logColor);
                }

                // 底部填充空间
                if (endIndex < totalLogs)
                {
                    GUILayout.Space((totalLogs - endIndex) * itemHeight);
                }

                GUILayout.EndScrollView();
            };
        }

        #endregion

        #region Helper Classes

        public class DebugWindow
        {
            public string Title;
            public int WindowId;
            public Rect WindowRect;
            public bool IsVisible;
            public Action DrawContent;
            public Vector2 ScrollPosition;
        }

        private class DebugLogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
            public DateTime Time;
        }

        private class WorldGUI
        {
            public int Id;
            public Transform Transform;
            public Vector3 Offset;
            public Vector3 WorldPosition;
            public Action DrawCallback;
            public bool UseFixedPosition;
            public Vector2 AreaSize = new Vector2(300, 100); // 可配置的绘制区域大小
            public Vector2 Pivot = new Vector2(0, 0); // 锚点，(0,0)表示左上角，(0.5,0.5)表示中心
        }

        #endregion
    }
}