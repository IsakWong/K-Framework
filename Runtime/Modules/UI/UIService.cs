using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace KFramework
{
[DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
public class UIService : PersistentSingleton<UIService>, IUIService
{
    /// <summary>
    /// 面板预制体加载器（类型 → 预制体）。业务在 GameCore.OnInit() 中注入。
    /// 面板按加载阶段分目录存放时，路径与类型不再一一对应，由这张表解析。
    /// </summary>
    public static Func<Type, UniTask<GameObject>> PanelLoader;

    /// <summary>
    /// 为 true 时面板只从已加载的缓存读取，未命中直接报错。
    /// 适合"所有 UI 都随语义批次预加载"的项目；默认关闭以保持通用服务的异步行为。
    /// </summary>
    public static bool RequirePreloadedPanels = false;

    private readonly List<UIPanel> _uiPanels = new();

    // HUD：常驻集合，不参与栈，Push/Close 不影响其它面板
    private readonly List<UIPanel> _hud = new();

    // Window：严格栈式，只有栈顶可见；First = 栈顶
    private readonly LinkedList<UIPanel> _windows = new();

    // Popup：叠在 Window 之上，多层并列。Add 顺序决定层内层级（后进在上）
    private readonly List<UIPanel> _popups = new();

    // Service：Loading / Toast 等全局覆盖层。永远在最上，CloseAll 不清
    private readonly List<UIPanel> _services = new();

    // 父 → 子列表。运行时由 PushAsync 维护
    private readonly Dictionary<UIPanel, List<UIPanel>> _childrenOf = new();

    // 层根节点（每层一个嵌套 Canvas，固定 sortingOrder）
    private readonly Dictionary<UILayer, RectTransform> _layerRoots = new();

    // 串行锁：保证一次只有一个栈操作在途，避免动画过程中栈被撕裂
    private UniTask _pendingOp = UniTask.CompletedTask;

    private Canvas _overlayCanvas;

    /// <summary>
    /// 全局 Panel 显隐动画。null 则不播放动画，由各 Panel 自行在 Inspector 中设置。
    /// 可设置为 UIAnimationFade (DOTween 淡入+上浮)、UIAnimationSlide (侧滑) 或自定义 UIAnimation 子类。
    /// </summary>
    public UIAnimation PanelAnimation = null;

    public IReadOnlyCollection<UIPanel> GetWindowStack() => _windows;
    public IReadOnlyList<UIPanel> GetLayerPanels(UILayer layer) => ContainerOf(layer);

    protected override void Awake()
    {
        base.Awake();
        InitOverlayCanvas();
    }

    protected override void OnServiceInit()
    {
        ServiceLocator.Register<IUIService>(this);
    }

    /// <summary>
    /// 由 UIPanel.Awake 自动调用，把 Panel 注册到 _uiPanels 列表。
    /// 业务不应直接调用。
    /// </summary>
    internal void AddUI<T>(T t) where T : UIPanel
    {
        if (t != null && !_uiPanels.Contains(t))
            _uiPanels.Add(t);
    }

    // ─────────── Canvas / 层初始化 ───────────

    private void InitOverlayCanvas()
    {
        if (CanvasInstance.Instance == null) return;
        _overlayCanvas = CanvasInstance.Instance.BehaviourInstance;
        if (!_overlayCanvas) return;

        _overlayCanvas.gameObject.SetActive(true);
        DontDestroyOnLoad(_overlayCanvas.gameObject);

        EnsureLayerRoots();

        // 场景中直接挂在 Canvas 下的 Panel，按各自 Layer 入容器
        var panels = _overlayCanvas.transform.GetComponentsInChildren<UIPanel>();
        foreach (var p in panels)
        {
            PushAsync(p).Forget();
        }
    }

    /// <summary>
    /// 为每个 UILayer 建立独立的嵌套 Canvas。层间层级由 sortingOrder 固定，
    /// 与 Push 顺序无关；层内层级才由 SetAsLastSibling 决定。
    /// </summary>
    private void EnsureLayerRoots()
    {
        if (_layerRoots.Count > 0 || _overlayCanvas == null) return;

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            var go = new GameObject($"[UI] {layer}", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_overlayCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrderOf(layer);
            go.AddComponent<GraphicRaycaster>();

            _layerRoots[layer] = rect;
        }
    }

    private static int SortingOrderOf(UILayer layer) => layer switch
    {
        UILayer.Hud => 0,
        UILayer.Window => 100,
        UILayer.Popup => 200,
        UILayer.Service => 300,
        _ => 0,
    };

    private RectTransform GetLayerRoot(UILayer layer)
    {
        if (_layerRoots.Count == 0) EnsureLayerRoots();
        if (_layerRoots.TryGetValue(layer, out var root)) return root;
        return _overlayCanvas != null ? _overlayCanvas.transform as RectTransform : null;
    }

    private void EnsureLayerParent(UIPanel panel)
    {
        var root = GetLayerRoot(panel.Layer);
        if (root != null && panel.transform.parent != root)
            panel.transform.SetParent(root, false);
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

    public async UniTask<UIPanel> RequireAsync(Type type, string path = null)
    {
        // 已存在则直接返回
        foreach (var panel in _uiPanels)
        {
            if (panel != null && type.IsInstanceOfType(panel))
                return panel;
        }

        GameObject prefab;
        if (!string.IsNullOrEmpty(path))
        {
            if (RequirePreloadedPanels)
            {
                // 严格模式：面板必须随阶段清单（AssetManifest）预热。
                // 未命中说明清单漏了，直接报错而不是悄悄异步补加载。
                if (!AssetService.Instance.TryLoad<GameObject>(path, out prefab))
                {
                    Debug.LogError($"[UIService] 面板未预热，请把 {path} 加入对应阶段的 AssetManifest.Paths。");
                    return null;
                }
            }
            else
            {
                prefab = await AssetService.Instance.LoadAsync<GameObject>(path);
            }
        }
        else if (PanelLoader != null)
        {
            prefab = await PanelLoader(type);
        }
        else
        {
            Debug.LogError($"[UIService] 未注入 PanelLoader，无法解析面板：{type.Name}");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError($"[UIService] 未找到 UI 预制体：{type.Name}");
            return null;
        }

        var instance = Instantiate(prefab);
        var ui = instance.GetComponent(type) as UIPanel;
        if (ui == null) ui = instance.AddComponent(type) as UIPanel;

        EnsureLayerParent(ui);
        ui.transform.SetAsLastSibling();
        ui.gameObject.SetActive(false);
        AddUI(ui);
        return ui;
    }

    public async UniTask<T> RequireAsync<T>(string path = null) where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null) return ui;
        return (T)await RequireAsync(typeof(T), path);
    }

    // ─────────── Push Batch ───────────

    /// <summary>
    /// 批量 Push 多个面板，所有面板的打开动画并行播放。
    /// 按数组顺序设置 SiblingIndex（后面的在上层）。
    /// </summary>
    public async UniTask PushBatchAsync(params UIPanel[] panels)
    {
        if (panels == null || panels.Length == 0) return;
        await WithLock(() => PushBatchInternalAsync(panels));
    }

    private async UniTask PushBatchInternalAsync(UIPanel[] panels)
    {
        // Phase 1: 处理 Window 栈变化
        UIPanel oldTop = null;
        foreach (var panel in panels)
        {
            if (panel == null || IsInContainers(panel)) continue;
            if (panel.Layer == UILayer.Window)
            {
                oldTop = _windows.First?.Value;
                if (oldTop != null && oldTop != panel && oldTop.Visible)
                {
                    await oldTop.SuspendAsyncInternal();
                }
                break;
            }
        }

        // Phase 2: 加入容器 + 设置 SiblingIndex，收集需要 Open 的面板
        var toOpen = new List<UIPanel>();
        foreach (var panel in panels)
        {
            if (panel == null || IsInContainers(panel)) continue;
            AddToContainer(panel);
            EnsureLayerParent(panel);
            panel.transform.SetAsLastSibling();
            toOpen.Add(panel);
        }

        // Phase 3: 并行播放所有打开动画
        var openTasks = new List<UniTask>();
        foreach (var panel in toOpen)
        {
            if (!panel.Visible)
                openTasks.Add(panel.OpenAsyncInternal());
        }
        await UniTask.WhenAll(openTasks);

        // Phase 4: 注册 parent
        foreach (var panel in panels)
        {
            if (panel == null) continue;
            RegisterParent(panel, panel.ParentPanel);
        }
    }

    // ─────────── Push ───────────

    /// <summary>
    /// 同时 Push host 面板与 companion 面板。
    /// Companion 面板的开关动画与 host 并行播放；host 关闭时所有 companion 自动并行关闭。
    /// Companion 面板标记 <see cref="UIPanel.IsCompanion"/>，不响应独立唤醒逻辑。
    /// </summary>
    public async UniTask PushWithCompanionsAsync(UIPanel host, params UIPanel[] companions)
    {
        if (host == null) return;

        await WithLock(async () =>
        {
            // 1. 标记 companion 面板为 Companion 模式
            foreach (var companion in companions)
            {
                if (companion == null) continue;
                companion.IsCompanion = true;

                // 如果已在容器中且 Visible（如玩家先 Tab 打开了背包），先瞬间关闭
                if (IsInContainers(companion) && companion.Visible)
                {
                    var savedAnim = companion.PanelAnimation;
                    companion.PanelAnimation = new UIAnimationNone();
                    await CloseInternalAsync(companion);
                    companion.PanelAnimation = savedAnim;
                }
            }

            // 2. Push host（正常流程：触发 Window 挂起等）
            await PushInternalAsync(host, null);

            // 3. Push companion 面板到各自层，并行播放打开动画
            var openTasks = new List<UniTask>();
            foreach (var companion in companions)
            {
                if (companion == null) continue;
                AddToContainer(companion);
                EnsureLayerParent(companion);
                companion.transform.SetAsLastSibling();
                if (!companion.Visible)
                    openTasks.Add(companion.OpenAsyncInternal());
            }

            if (openTasks.Count > 0)
                await UniTask.WhenAll(openTasks);

            // 4. 注册父→子级联：host 关闭时自动带 companion
            if (_childrenOf.TryGetValue(host, out var existing))
                existing.AddRange(companions.Where(c => c != null));
            else
                _childrenOf[host] = new List<UIPanel>(companions.Where(c => c != null));
        });
    }

    public async UniTask<T> PushAsync<T>(UIPanel parent = null, Action<T> configure = null) where T : UIPanel
    {
        var panel = await RequireAsync<T>();
        if (panel == null) return null;

        // Push 阶段初始化：在动画播放前绑定数据，避免面板空着播完动画
        if (configure != null)
        {
            panel.gameObject.SetActive(false);
            configure(panel);
        }

        await PushAsync(panel, parent);
        return panel;
    }

    public async UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null)
    {
        if (panel == null) return null;

        // 已在容器内 → 切到前台，不重复 Open
        if (IsInContainers(panel))
        {
            await BringToFrontAsync(panel);
            return panel;
        }

        await WithLock(() => PushInternalAsync(panel, parent));
        return panel;
    }

    private async UniTask PushInternalAsync(UIPanel panel, UIPanel parent)
    {
        // Window 是栈：压栈前把旧栈顶挂起（保留订阅 / BGM / 状态）
        if (panel.Layer == UILayer.Window)
        {
            var oldTop = _windows.First?.Value;
            if (oldTop != null && oldTop != panel && oldTop.Visible)
                await oldTop.SuspendAsyncInternal();
        }

        AddToContainer(panel);
        EnsureLayerParent(panel);
        panel.transform.SetAsLastSibling();
        if (!panel.Visible) await panel.OpenAsyncInternal();

        RegisterParent(panel, parent ?? panel.ParentPanel);
    }

    // ─────────── BringToFront ───────────

    public async UniTask BringToFrontAsync(UIPanel panel)
    {
        if (panel == null || !IsInContainers(panel)) return;
        await WithLock(() => BringToFrontInternalAsync(panel));
    }

    public async UniTask BringToFrontAsync<T>() where T : UIPanel
    {
        var panel = FindInContainers<T>();
        if (panel == null) return;
        await BringToFrontAsync(panel);
    }

    private async UniTask BringToFrontInternalAsync(UIPanel panel)
    {
        if (panel.Layer == UILayer.Window)
        {
            var oldTop = _windows.First?.Value;
            if (oldTop == panel)
            {
                // 已经在栈顶，仅置 SiblingIndex
                panel.transform.SetAsLastSibling();
                if (!panel.Visible) await panel.ResumeAsyncInternal();
                return;
            }

            // 旧栈顶挂起
            if (oldTop != null && oldTop.Visible)
                await oldTop.SuspendAsyncInternal();

            // 自己移到栈顶
            _windows.Remove(panel);
            _windows.AddFirst(panel);
            EnsureLayerParent(panel);
            panel.transform.SetAsLastSibling();

            // 之前是挂起状态 → Resume；否则冷启动 Open
            if (!panel.Visible)
            {
                if (panel.IsSuspended) await panel.ResumeAsyncInternal();
                else await panel.OpenAsyncInternal();
            }
        }
        else // Hud / Popup / Service：仅置层内置顶
        {
            EnsureLayerParent(panel);
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.OpenAsyncInternal();
        }
    }

    // ─────────── Close ───────────

    public async UniTask CloseAsync(UIPanel panel = null)
    {
        panel ??= _windows.First?.Value;
        if (panel == null) return;
        await WithLock(() => CloseInternalAsync(panel));
    }

    public async UniTask<T> CloseAsync<T>() where T : UIPanel
    {
        var panel = FindInContainers<T>();
        if (panel == null) return null;
        await WithLock(() => CloseInternalAsync(panel));
        return panel;
    }

    public async UniTask CloseTopWindowAsync()
    {
        var top = _windows.First?.Value;
        if (top == null) return;
        await WithLock(() => CloseInternalAsync(top));
    }

    /// <summary>
    /// 关闭 Hud / Window / Popup。Service 层不受影响（Loading 等全局覆盖层不会被误关）。
    /// </summary>
    public async UniTask CloseAllAsync()
    {
        // 快照一份避免遍历时修改；Popup 先于 Window，避免 Window 恢复时闪一下
        var snapshot = new List<UIPanel>(_popups);
        snapshot.AddRange(_windows);
        snapshot.AddRange(_hud);

        await WithLock(() => CloseAllInternalAsync(snapshot));
    }

    private async UniTask CloseAllInternalAsync(List<UIPanel> snapshot)
    {
        foreach (var p in snapshot)
        {
            if (p == null) continue;
            if (!IsInContainers(p)) continue;
            if (p.Visible) await p.CloseAsyncInternal();
            RemoveFromContainers(p);
            UnregisterParent(p);
        }
        _hud.Clear();
        _windows.Clear();
        _popups.Clear();
        _childrenOf.Clear();
    }

    private async UniTask CloseInternalAsync(UIPanel panel)
    {
        if (!IsInContainers(panel)) return;

        bool wasWindow = panel.Layer == UILayer.Window;

        // 1. 收集 companion 子面板（稍后与 panel 并行关闭）
        //    普通子面板按原有逻辑倒序串行关闭
        List<UIPanel> companions = null;
        if (_childrenOf.TryGetValue(panel, out var children) && children.Count > 0)
        {
            var snapshot = new List<UIPanel>(children);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                var child = snapshot[i];
                if (child == null || !IsInContainers(child)) continue;

                if (child.IsCompanion)
                {
                    companions ??= new List<UIPanel>();
                    companions.Add(child);
                }
                else
                {
                    await CloseInternalAsync(child);
                }
            }
        }

        // 2. 所有面板执行 BeginCloseSequence（OnBeforeClose + 停交互 + 发信号）
        var closeTargets = new List<UIPanel> { panel };
        if (companions != null)
            closeTargets.AddRange(companions);

        foreach (var target in closeTargets)
        {
            if (target.Visible)
                target.BeginCloseSequence();
            else
                target.gameObject.SetActive(false);
        }

        // 3. 并行播放所有关闭动画
        var animTasks = new List<UniTask>();
        foreach (var target in closeTargets)
        {
            if (target.Visible)
                animTasks.Add(target.PlayCloseAnimationAsync());
        }

        if (animTasks.Count > 0)
            await UniTask.WhenAll(animTasks);

        // 4. 完成关闭（OnClose: 断订阅、SetActive(false)）
        foreach (var target in closeTargets)
            target.FinishClose();

        // 5. 从容器移除
        RemoveFromContainers(panel);
        UnregisterParent(panel);

        if (companions != null)
        {
            foreach (var companion in companions)
            {
                RemoveFromContainers(companion);
                UnregisterParent(companion);
            }
        }

        // 6. 若是 Window 且栈里还有下一张 → 恢复新栈顶
        if (wasWindow)
        {
            var next = _windows.First?.Value;
            if (next != null && !next.Visible)
            {
                EnsureLayerParent(next);
                if (next.IsSuspended)
                    await next.ResumeAsyncInternal();
                else
                    await next.OpenAsyncInternal();
            }
        }
    }

    // ─────────── 销毁 ───────────

    public async UniTask DestroyAsync<T>() where T : UIPanel
    {
        var ui = GetUI<T>();
        if (ui != null) await DestroyAsync(ui);
    }

    public async UniTask DestroyAsync(UIPanel panel)
    {
        if (panel == null) return;
        if (IsInContainers(panel)) await CloseAsync(panel);
        _uiPanels.Remove(panel);
        if (panel) Destroy(panel.gameObject);
    }

    public async UniTask DestroyAllAsync()
    {
        await CloseAllAsync();
        var snapshot = new List<UIPanel>(_uiPanels);
        foreach (var p in snapshot)
        {
            if (p) Destroy(p.gameObject);
        }
        _uiPanels.Clear();
        _services.Clear();
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

    public bool IsOpen<T>() where T : UIPanel
    {
        foreach (var p in _services) if (p is T && p.Visible) return true;
        foreach (var p in _popups) if (p is T && p.Visible) return true;
        foreach (var p in _windows) if (p is T && p.Visible) return true;
        foreach (var p in _hud) if (p is T && p.Visible) return true;
        return false;
    }

    public bool IsOpen(UIPanel panel) => panel != null && panel.Visible && IsInContainers(panel);

    public bool IsLoaded<T>() where T : UIPanel
    {
        foreach (var p in _uiPanels) if (p is T) return true;
        return false;
    }

    public bool IsLoaded(UIPanel panel) => panel != null && _uiPanels.Contains(panel);

    public int GetVisiblePanelCount() =>
        _hud.Count + _windows.Count + _popups.Count + _services.Count;

    public bool HasAnyPanelVisible() => GetVisiblePanelCount() > 0;

    public UIPanel GetTopWindow() => _windows.First?.Value;

    /// <summary>按层优先级取最顶层可见面板：Service &gt; Popup &gt; Window &gt; Hud。</summary>
    public UIPanel GetTopPanel()
    {
        var top = TopVisible(_services);
        if (top != null) return top;

        top = TopVisible(_popups);
        if (top != null) return top;

        var window = _windows.First?.Value;
        if (window != null && window.Visible) return window;

        return TopVisible(_hud);
    }

    public UIPanel GetBottomPanel()
    {
        if (_windows.Count > 0) return _windows.Last.Value;
        if (_hud.Count > 0) return _hud[0];
        if (_popups.Count > 0) return _popups[0];
        if (_services.Count > 0) return _services[0];
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

    private List<UIPanel> ContainerOf(UILayer layer) => layer switch
    {
        UILayer.Hud => _hud,
        UILayer.Window => new List<UIPanel>(_windows),
        UILayer.Popup => _popups,
        UILayer.Service => _services,
        _ => _popups,
    };

    private void AddToContainer(UIPanel panel)
    {
        switch (panel.Layer)
        {
            case UILayer.Hud: _hud.Add(panel); break;
            case UILayer.Window: _windows.AddFirst(panel); break;
            case UILayer.Popup: _popups.Add(panel); break;
            case UILayer.Service: _services.Add(panel); break;
        }
    }

    private static UIPanel TopVisible(List<UIPanel> container)
    {
        for (int i = container.Count - 1; i >= 0; i--)
        {
            var p = container[i];
            if (p != null && p.Visible) return p;
        }
        return null;
    }

    private bool IsInContainers(UIPanel panel)
    {
        return panel != null
            && (_hud.Contains(panel)
                || _windows.Contains(panel)
                || _popups.Contains(panel)
                || _services.Contains(panel));
    }

    private void RemoveFromContainers(UIPanel panel)
    {
        if (panel == null) return;
        _hud.Remove(panel);
        _windows.Remove(panel);
        _popups.Remove(panel);
        _services.Remove(panel);
    }

    private T FindInContainers<T>() where T : UIPanel
    {
        foreach (var p in _services) if (p is T t) return t;
        foreach (var p in _popups) if (p is T t) return t;
        foreach (var p in _windows) if (p is T t) return t;
        foreach (var p in _hud) if (p is T t) return t;
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
        // 同时清掉以 panel 为 key 的条目（可能已在 Close 流程里被消耗为空）
        _childrenOf.Remove(panel);
    }

    // 串行锁：把每次操作排队到 _pendingOp 之后。
    // 使用 Func<UniTask> 工厂而非直接传 UniTask，避免 async 方法的同步部分
    // 在 await previous 之前就执行，导致动画重叠和状态撕裂。
    private async UniTask WithLock(Func<UniTask> opFactory)
    {
        var previous = _pendingOp;
        var tcs = new UniTaskCompletionSource();
        _pendingOp = tcs.Task;
        try
        {
            await previous;
            await opFactory();
        }
        finally
        {
            tcs.TrySetResult();
        }
    }
}
}
