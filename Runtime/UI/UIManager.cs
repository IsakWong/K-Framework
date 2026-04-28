using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.Foundation;
using UnityEngine;

[DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
public class UIManager : PersistentSingleton<UIManager>, IUIService
{
    public static string UIPrefix = "Assets/UI/";

    private readonly List<UIPanel> _uiPanels = new();

    // Fullscreen：严格栈式，只有栈顶可见。First = 栈顶
    private readonly LinkedList<UIPanel> _fullscreenStack = new();

    // Overlay：与 Fullscreen 共存，多层可并列。Add 顺序决定渲染层级（后进在上）
    private readonly List<UIPanel> _overlays = new();

    // 父 → 子列表。运行时由 PushAsync 维护
    private readonly Dictionary<UIPanel, List<UIPanel>> _childrenOf = new();

    // 串行锁：保证一次只有一个栈操作在途，避免动画过程中栈被撕裂
    private UniTask _pendingOp = UniTask.CompletedTask;

    private Canvas _overlayCanvas;

    public IReadOnlyCollection<UIPanel> GetFullscreenStack() => _fullscreenStack;
    public IReadOnlyList<UIPanel> GetOverlays() => _overlays;

    protected override void Awake()
    {
        base.Awake();
        InitOverlayCanvas();
    }

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IUIService>(this);
    }

    public void AddUI<T>(T t) where T : UIPanel
    {
        if (!_uiPanels.Contains(t))
            _uiPanels.Add(t);
    }

    // ─────────── Canvas 初始化 ───────────

    private void InitOverlayCanvas()
    {
        _overlayCanvas = CanvasInstance.Instance.BehaviourInstance;
        if (!_overlayCanvas) return;

        _overlayCanvas.gameObject.SetActive(true);
        DontDestroyOnLoad(_overlayCanvas.gameObject);

        // 场景中直接挂在 Canvas 下的 Panel，按原有语义逐个压栈
        var panels = _overlayCanvas.transform.GetComponentsInChildren<UIPanel>();
        foreach (var p in panels)
        {
            PushUIAsync(p).Forget();
        }
    }

    public Canvas OverlayCanvas
    {
        get
        {
            if (!_overlayCanvas) InitOverlayCanvas();
            return _overlayCanvas;
        }
    }

    // ─────────── 资源加载 ───────────

    public async UniTask<UIPanel> RequireUIAsync(Type type)
    {
        // 已存在则直接返回
        foreach (var panel in _uiPanels)
        {
            if (panel != null && type.IsInstanceOfType(panel))
                return panel;
        }

        var prefab = await AssetManager.Instance.LoadAssetAsync<GameObject>(
            $"{UIPrefix}{type.Name}.prefab");
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] 未找到 UI 预制体：{UIPrefix}{type.Name}.prefab");
            return null;
        }

        var instance = Instantiate(prefab);
        var ui = instance.GetComponent(type) as UIPanel;
        if (ui == null) ui = instance.AddComponent(type) as UIPanel;

        ui.transform.SetParent(OverlayCanvas.transform, false);
        ui.transform.SetSiblingIndex(ui.transform.parent.childCount);
        ui.gameObject.SetActive(true);
        AddUI(ui);
        return ui;
    }

    public async UniTask<T> RequireUIAsync<T>() where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null) return ui;
        return (T)await RequireUIAsync(typeof(T));
    }

    // ─────────── Push ───────────

    public async UniTask<T> PushUIAsync<T>(UIPanel parent = null) where T : UIPanel
    {
        var panel = await RequireUIAsync<T>();
        if (panel == null) return null;
        await PushUIAsync(panel, parent);
        return panel;
    }

    public async UniTask<UIPanel> PushUIAsync(UIPanel panel, UIPanel parent = null)
    {
        if (panel == null) return null;
        await WithLock(PushInternalAsync(panel, parent));
        return panel;
    }

    private async UniTask PushInternalAsync(UIPanel panel, UIPanel parent)
    {
        // 已在容器里：先从旧位置移除（不触发 Hide）
        RemoveFromContainers(panel);

        if (panel.Kind == UIPanelKind.Fullscreen)
        {
            // 栈顶若有其它 Fullscreen → 先隐藏
            var oldTop = _fullscreenStack.First?.Value;
            if (oldTop != null && oldTop != panel && oldTop.Visible)
            {
                await oldTop.HidePanelAsync();
            }

            _fullscreenStack.AddFirst(panel);
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.ShowPanelAsync();
        }
        else // Overlay
        {
            _overlays.Add(panel);
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.ShowPanelAsync();
        }

        RegisterParent(panel, parent ?? panel.ParentPanel);
    }

    // ─────────── Pop ───────────

    public async UniTask PopUIAsync(UIPanel panel = null)
    {
        panel ??= _fullscreenStack.First?.Value;
        if (panel == null) return;
        await WithLock(PopInternalAsync(panel));
    }

    public async UniTask<T> PopUIAsync<T>() where T : UIPanel
    {
        var panel = FindInContainers<T>();
        if (panel == null) return null;
        await WithLock(PopInternalAsync(panel));
        return panel;
    }

    public async UniTask PopFullscreenAsync()
    {
        var top = _fullscreenStack.First?.Value;
        if (top == null) return;
        await WithLock(PopInternalAsync(top));
    }

    public async UniTask PopAllAsync()
    {
        // 快照一份避免遍历时修改
        var snapshot = new List<UIPanel>(_fullscreenStack);
        snapshot.AddRange(_overlays);

        await WithLock(PopAllInternalAsync(snapshot));
    }

    private async UniTask PopAllInternalAsync(List<UIPanel> snapshot)
    {
        foreach (var p in snapshot)
        {
            if (p == null) continue;
            if (!IsInContainers(p)) continue;
            if (p.Visible) await p.HidePanelAsync();
            RemoveFromContainers(p);
            UnregisterParent(p);
        }
        _fullscreenStack.Clear();
        _overlays.Clear();
        _childrenOf.Clear();
    }

    public UniTask CloseUIAsync(UIPanel panel) => PopUIAsync(panel);

    private async UniTask PopInternalAsync(UIPanel panel)
    {
        if (!IsInContainers(panel)) return;

        // 1. 级联 Pop 子面板（倒序，保证最后压入的最先关闭）
        if (_childrenOf.TryGetValue(panel, out var children) && children.Count > 0)
        {
            var snapshot = new List<UIPanel>(children);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                var child = snapshot[i];
                if (child != null && IsInContainers(child))
                    await PopInternalAsync(child);
            }
        }

        // 2. 隐藏自身
        bool wasFullscreen = panel.Kind == UIPanelKind.Fullscreen;
        if (panel.Visible) await panel.HidePanelAsync();

        // 3. 从容器移除
        RemoveFromContainers(panel);
        UnregisterParent(panel);

        // 4. 若是 Fullscreen 且栈里还有下一张 → 显示新栈顶
        if (wasFullscreen)
        {
            var next = _fullscreenStack.First?.Value;
            if (next != null && !next.Visible)
            {
                await next.ShowPanelAsync();
            }
        }
    }

    // ─────────── 销毁 ───────────

    public async UniTask DestroyUIAsync<T>() where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null) await DestroyUIAsync(ui);
    }

    public async UniTask DestroyUIAsync(UIPanel panel)
    {
        if (panel == null) return;
        if (IsInContainers(panel)) await PopUIAsync(panel);
        _uiPanels.Remove(panel);
        if (panel) Destroy(panel.gameObject);
    }

    public async UniTask DestroyAllUIAsync()
    {
        await PopAllAsync();
        var snapshot = new List<UIPanel>(_uiPanels);
        foreach (var p in snapshot)
        {
            if (p) Destroy(p.gameObject);
        }
        _uiPanels.Clear();
    }

    // ─────────── 查询（同步） ───────────

    public T GetUI<T>() where T : class
    {
        var canvas = OverlayCanvas;
        foreach (var panel in _uiPanels)
        {
            if (panel is T t) return t;
        }

        if (canvas != null)
        {
            var found = canvas.transform.GetComponentsInChildren<T>(true);
            if (found.Length > 0) return found[0];
        }
        return null;
    }

    public bool IsUIVisible<T>() where T : UIPanel
    {
        foreach (var p in _fullscreenStack) if (p is T) return true;
        foreach (var p in _overlays) if (p is T) return true;
        return false;
    }

    public bool IsUIVisible(UIPanel panel) => IsInContainers(panel);

    public int GetVisiblePanelCount() => _fullscreenStack.Count + _overlays.Count;

    public bool HasAnyPanelVisible() => GetVisiblePanelCount() > 0;

    public UIPanel GetTopmostFullscreen() => _fullscreenStack.First?.Value;

    public UIPanel GetTopmostPanel()
    {
        // Overlay 渲染层级 > Fullscreen 栈顶
        if (_overlays.Count > 0) return _overlays[_overlays.Count - 1];
        return _fullscreenStack.First?.Value;
    }

    public UIPanel GetBottomPanel()
    {
        if (_fullscreenStack.Count > 0) return _fullscreenStack.Last.Value;
        if (_overlays.Count > 0) return _overlays[0];
        return null;
    }

    public List<UIPanel> GetAllPanels() => new(_uiPanels);

    public List<T> GetAllPanels<T>() where T : UIPanel
    {
        var result = new List<T>();
        foreach (var panel in _uiPanels)
        {
            if (panel is T typed) result.Add(typed);
        }
        return result;
    }

    // ─────────── 工具 ───────────

    private bool IsInContainers(UIPanel panel)
    {
        return panel != null && (_fullscreenStack.Contains(panel) || _overlays.Contains(panel));
    }

    private void RemoveFromContainers(UIPanel panel)
    {
        if (panel == null) return;
        _fullscreenStack.Remove(panel);
        _overlays.Remove(panel);
    }

    private T FindInContainers<T>() where T : UIPanel
    {
        foreach (var p in _fullscreenStack) if (p is T t) return t;
        foreach (var p in _overlays) if (p is T t) return t;
        return null;
    }

    private void RegisterParent(UIPanel panel, UIPanel parent)
    {
        if (panel == null || parent == null || parent == panel) return;
        if (!_childrenOf.TryGetValue(parent, out var list))
        {
            list = new List<UIPanel>();
            _childrenOf[parent] = list;
        }
        if (!list.Contains(panel)) list.Add(panel);
    }

    private void UnregisterParent(UIPanel panel)
    {
        if (panel == null) return;
        var parent = panel.ParentPanel;
        if (parent != null && _childrenOf.TryGetValue(parent, out var list))
        {
            list.Remove(panel);
            if (list.Count == 0) _childrenOf.Remove(parent);
        }
        // 同时清掉以 panel 为 key 的条目（可能已在 Pop 流程里被消耗为空）
        _childrenOf.Remove(panel);
    }

    // 串行锁：把每次操作排队到 _pendingOp 之后
    private async UniTask WithLock(UniTask op)
    {
        var previous = _pendingOp;
        var tcs = new UniTaskCompletionSource();
        _pendingOp = tcs.Task;
        try
        {
            await previous;
            await op;
        }
        finally
        {
            tcs.TrySetResult();
        }
    }
}
