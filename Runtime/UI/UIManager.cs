using System;
using System.Collections.Generic;
using Framework.Foundation;
using UnityEngine;


[DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
public class UIManager : PersistentSingleton<UIManager>, IUIService
{
    private List<UIPanel> _uiPanels = new();
    
    // Unified stack for all UI panels (fullscreen and normal UI)
    private LinkedList<UIPanel> _visibleStack = new();
    
    public LinkedList<UIPanel> VisibleStack => _visibleStack;
    
    // Helper properties to get filtered views
    public IEnumerable<UIPanel> FullscreenStack
    {
        get
        {
            foreach (var panel in _visibleStack)
            {
                if (panel.FullscreenPanel)
                    yield return panel;
            }
        }
    }
    
    public IEnumerable<UIPanel> NormalUIStack
    {
        get
        {
            foreach (var panel in _visibleStack)
            {
                if (!panel.FullscreenPanel)
                    yield return panel;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        InitOverlayCanvas();
        //Debug.Assert(GlobalConfig != null, "UIManager: GlobalConfig is null, please check your config files.");
    }

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IUIService>(this);
    }


    public void AddUI<T>(T t) where T : UIPanel
    {
        _uiPanels.Add(t as UIPanel);
    }

    public static string UIPrefix = "Assets/UI/";

    private Canvas _overlayCanvas;
    private RectTransform _hudParent;

    private void InitOverlayCanvas()
    {
        _overlayCanvas = CanvasInstance.Instance.BehaviourInstance;
        if (_overlayCanvas)
        {
            var panels = _overlayCanvas.transform.GetComponentsInChildren<UIPanel>();
            foreach (var p in panels)
            {
                PushUI(p);
            }

            _overlayCanvas.gameObject.SetActive(true);
            DontDestroyOnLoad(_overlayCanvas.gameObject);
            return;
        }
    }

    public Canvas OverlayCanvas
    {
        get
        {
            if (_overlayCanvas)
            {
                return _overlayCanvas;
            }

            InitOverlayCanvas();
            return _overlayCanvas;
        }
    }

    public UIPanel RequireUI(Type type) 
    {
        GameObject prefab = null;
        prefab = AssetManager.Instance.LoadAsset<GameObject>($"{UIPrefix}{type.Name}.prefab");
        GameObject instance;
        if (prefab != null)
        {
            instance = Instantiate(prefab);
        }
        else
        {
            Debug.LogError($"[UIManager] 未找到 UI 预制体：{UIPrefix}{type.Name}.prefab");
            return null;
        }

        var t = instance.GetComponent(type) as UIPanel;
        if (t == null)
            t = instance.AddComponent(type) as UIPanel;

        t.transform.SetParent(OverlayCanvas.transform, false);
        t.transform.SetSiblingIndex(t.transform.parent.transform.childCount);
        t.gameObject.SetActive(true);
        if (!_uiPanels.Contains(t))
            _uiPanels.Add(t as UIPanel);
        PushUI(t);
        return t;
    }

    public T RequireUI<T>(bool defaultActive = true) where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null)
        {
            return ui;
        }
        ui = RequireUI(typeof(T)) as T;
        return ui;
    }
    
    public T PushUI<T>(bool clearStack = false) where T : UIPanel
    {
        var panel = RequireUI<T>(false);
        PushUI(panel, clearStack);
        return panel;
    }
    
    public UIPanel PushUI(UIPanel panel, bool clearStack = false)
    {
        if (clearStack)
        {
            Instance.PopAll();
        }

        bool isFullscreen = panel.FullscreenPanel;
        
        // Remove from stack if exists
        if (_visibleStack.Contains(panel))
        {
            _visibleStack.Remove(panel);
        }

        // Handle fullscreen UI logic
        if (isFullscreen)
        {
            // Find and hide current fullscreen panel if exists
            UIPanel currentFullscreen = null;
            foreach (var p in _visibleStack)
            {
                if (p.FullscreenPanel && p != panel)
                {
                    currentFullscreen = p;
                    break;
                }
            }
            
            if (currentFullscreen != null)
            {
                currentFullscreen.HidePanel();
                Guid handle = Guid.NewGuid();
                handle = currentFullscreen.OnPanelHide.Connect(() =>
                {
                    currentFullscreen.OnPanelHide.Disconnect(handle);
                    if (!panel.Visible)
                    {
                        panel.ShowPanel();
                    }
                    _visibleStack.AddFirst(panel);
                });
            }
            else
            {
                if (!panel.Visible)
                {
                    panel.ShowPanel();
                }
                _visibleStack.AddFirst(panel);
            }
            
            // Hide all normal UI when showing fullscreen
            HideAllNormalUI();
        }
        else
        {
            // Normal UI can coexist
            if (!panel.Visible)
            {
                panel.ShowPanel();
            }
            _visibleStack.AddFirst(panel);
        }
        
        return panel;
    }

    public void PopAll()
    {
        // Hide all visible panels
        var panelsToHide = new List<UIPanel>(_visibleStack);
        
        foreach (var panel in panelsToHide)
        {
            if (panel != null && panel.Visible)
            {
                panel.HidePanel();
            }
        }

        _visibleStack.Clear();
    }

    public void PopUI(UIPanel panel = null, bool clearStack = false)
    {
        if (_visibleStack.Count == 0)
        {
            return;
        }

        if (panel == null)
        {
            // Pop the topmost panel
            panel = _visibleStack.First?.Value;
        }

        if (panel == null || !_visibleStack.Contains(panel))
        {
            return;
        }

        bool wasFullscreen = panel.FullscreenPanel;
        
        // Remove from stack
        _visibleStack.Remove(panel);

        // Hide the panel
        if (panel.Visible)
        {
            panel.HidePanel();
        }

        if (clearStack)
        {
            PopAll();
        }
        else
        {
            // If we closed a fullscreen panel, show the previous fullscreen or restore normal UI
            if (wasFullscreen)
            {
                // Find next fullscreen panel
                UIPanel nextFullscreen = null;
                foreach (var p in _visibleStack)
                {
                    if (p.FullscreenPanel)
                    {
                        nextFullscreen = p;
                        break;
                    }
                }
                
                if (nextFullscreen != null)
                {
                    // Show previous fullscreen panel
                    if (!nextFullscreen.Visible)
                    {
                        nextFullscreen.ShowPanel();
                    }
                    HideAllNormalUI();
                }
                else
                {
                    // No more fullscreen panels, show all normal UI
                    ShowAllNormalUI();
                }
            }
        }
    }

    public void PopFullScreenUI(bool clearStack)
    {
        if (_visibleStack.Count == 0)
        {
            return;
        }

        var panel = GetTopmostFullscreenPanel();
        PopUI(panel, clearStack);
    }
    
    public T PopFullScreenUI<T>(bool clearStack = false) where T : UIPanel
    {
        if (_visibleStack.Count == 0)
        {
            return null;
        }

        // Search for the fullscreen panel
        foreach (var p in _visibleStack)
        {
            if(p is T && p.FullscreenPanel)
            {
              PopUI(p, clearStack);
              return p as T;
            }
        }
        return null;
    }
    
    public T PopUI<T>(bool clearStack = false) where T : UIPanel
    {
        if (_visibleStack.Count == 0)
        {
            return null;
        }

        // Search for the panel
        foreach (var p in _visibleStack)
        {
            if(p is T )
            {
              PopUI(p, clearStack);
              return p as T;
            }
        }
        
        return null;
    }
    
    public UIPanel GetTopmostFullscreenPanel()
    {
        foreach (var panel in _visibleStack)
        {
            if (panel.FullscreenPanel)
            {
                return panel;
            }
        }
        return null;
    }
    
    public UIPanel GetTopmostPanel()
    {
        return _visibleStack.Count > 0 ? _visibleStack.First.Value : null;
    }

    public UIPanel GetBottomPanel()
    {
        return _visibleStack.Count > 0 ? _visibleStack.Last.Value : null;
    }

    public bool IsUIVisible<T>() where T : UIPanel
    {
        foreach (var panel in _visibleStack)
        {
            if (panel is T)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsUIVisible(UIPanel panel)
    {
        return _visibleStack.Contains(panel);
    }

    public int GetVisiblePanelCount()
    {
        return _visibleStack.Count;
    }

    public bool HasAnyPanelVisible()
    {
        return _visibleStack.Count > 0;
    }

    public void CloseUI<T>() where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null)
        {
            var panel = ui as UIPanel;
            if (_visibleStack.Contains(panel))
            {
                PopUI(panel);
            }
        }
    }

    public void CloseUI(UIPanel panel)
    {
        if (panel != null && _visibleStack.Contains(panel))
        {
            PopUI(panel);
        }
    }

    public void DestroyUI<T>() where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null)
        {
            var panel = ui as UIPanel;
            if (_visibleStack.Contains(panel))
            {
                _visibleStack.Remove(panel);
            }
            _uiPanels.Remove(panel);
            Destroy(panel.gameObject);
        }
    }

    public void DestroyUI(UIPanel panel)
    {
        if (panel != null)
        {
            if (_visibleStack.Contains(panel))
            {
                _visibleStack.Remove(panel);
            }
            _uiPanels.Remove(panel);
            Destroy(panel.gameObject);
        }
    }

    public void DestroyAllUI()
    {
        PopAll();
        var panelsToDestroy = new List<UIPanel>(_uiPanels);
        foreach (var panel in panelsToDestroy)
        {
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        _uiPanels.Clear();
    }

    public void HideAll()
    {
        foreach (var panel in _visibleStack)
        {
            if (panel != null && panel.Visible)
            {
                panel.HidePanel();
            }
        }
    }

    public void ShowAll()
    {
        foreach (var panel in _visibleStack)
        {
            if (panel != null && !panel.Visible)
            {
                panel.ShowPanel();
            }
        }
    }

    private void HideAllNormalUI()
    {
        foreach (var panel in _visibleStack)
        {
            if (panel != null && !panel.FullscreenPanel && panel.Visible)
            {
                panel.HidePanel();
            }
        }
    }

    private void ShowAllNormalUI()
    {
        foreach (var panel in _visibleStack)
        {
            if (panel != null && !panel.FullscreenPanel && !panel.Visible)
            {
                panel.ShowPanel();
            }
        }
    }

    public List<UIPanel> GetAllPanels()
    {
        return new List<UIPanel>(_uiPanels);
    }

    public List<T> GetAllPanels<T>() where T : UIPanel
    {
        var result = new List<T>();
        foreach (var panel in _uiPanels)
        {
            if (panel is T typedPanel)
            {
                result.Add(typedPanel);
            }
        }
        return result;
    }

    public void BringToFront(UIPanel panel)
    {
        if (panel == null) return;
        
        if (_visibleStack.Contains(panel))
        {
            _visibleStack.Remove(panel);
            _visibleStack.AddFirst(panel);
            panel.transform.SetAsLastSibling();
        }
    }

    public void SendToBack(UIPanel panel)
    {
        if (panel == null) return;
        
        if (_visibleStack.Contains(panel))
        {
            _visibleStack.Remove(panel);
            _visibleStack.AddLast(panel);
            panel.transform.SetAsFirstSibling();
        }
    }

    public T GetUI<T>() where T : class
    {
        // Make sure the OverlayCanvas is initialized
        var canvas = OverlayCanvas;
        foreach (var panel in _uiPanels)
        {
            var t = panel as T;
            if (t is not null)
            {
                return t;
            }
        }

        var panels = _overlayCanvas.transform.GetComponentsInChildren<T>();
        if (panels.Length > 0)
        {
            return panels[0];
        }

        return null;
    }
}
