using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Framework.Foundation;
using UnityEngine;

[DefaultExecutionOrder(GameCoreProxy.ModuleOrder)]
public class UIManager : PersistentSingleton<UIManager>, IUIService
{
    /// <summary>
    /// UI 预制体路径前缀。业务在 GameCore.OnInit() 中设置。
    /// </summary>
    public string UIPrefix = "Assets/UI/";

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

    /// <summary>
    /// 全局 Panel 显隐动画。null 则回退到 UIPanel 自身的 OpenFx/CloseFx (MMF_Player)。
    /// 可设置为 UIAnimationFade (DOTween 淡入淡出) 或自定义 UIAnimation 子类。
    /// </summary>
    public UIAnimation PanelAnimation = new UIAnimationFade();

    public IReadOnlyCollection<UIPanel> GetFullscreenStack() => _fullscreenStack;
    public IReadOnlyList<UIPanel> GetOverlays() => _overlays;

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

    // ─────────── Canvas 初始化 ───────────

    private void InitOverlayCanvas()
    {
        if (CanvasInstance.Instance == null) return;
        _overlayCanvas = CanvasInstance.Instance.BehaviourInstance;
        if (!_overlayCanvas) return;

        _overlayCanvas.gameObject.SetActive(true);
        DontDestroyOnLoad(_overlayCanvas.gameObject);

        // 场景中直接挂在 Canvas 下的 Panel，按原有语义逐个压栈
        var panels = _overlayCanvas.transform.GetComponentsInChildren<UIPanel>();
        foreach (var p in panels)
        {
            PushAsync(p).Forget();
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

    public async UniTask<UIPanel> RequireAsync(Type type, string path = null)
    {
        // 已存在则直接返回
        foreach (var panel in _uiPanels)
        {
            if (panel != null && type.IsInstanceOfType(panel))
                return panel;
        }

        var assetPath = !string.IsNullOrEmpty(path) ? path : $"{UIPrefix}{type.Name}.prefab";
        var prefab = await AssetManager.Instance.LoadAssetAsync<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError($"[UIManager] 未找到 UI 预制体：{assetPath}");
            return null;
        }

        var instance = Instantiate(prefab);
        var ui = instance.GetComponent(type) as UIPanel;
        if (ui == null) ui = instance.AddComponent(type) as UIPanel;

        ui.transform.SetParent(OverlayCanvas.transform, false);
        ui.transform.SetSiblingIndex(ui.transform.parent.childCount);
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

            // 2. Push host（正常流程：触发 Suspend 旧栈顶等）
            await PushInternalAsync(host, null);

            // 3. Push companion 面板到 Overlay 列表，并行播放打开动画
            var openTasks = new List<UniTask>();
            foreach (var companion in companions)
            {
                if (companion == null) continue;
                _overlays.Add(companion);
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
        if (panel.Kind == UIPanelKind.Fullscreen)
        {
            // 栈顶若有其它 Fullscreen → 按 KeepAliveOnSuspend 走 Suspend 或 Close
            var oldTop = _fullscreenStack.First?.Value;
            if (oldTop != null && oldTop != panel && oldTop.Visible)
            {
                if (oldTop.KeepAliveOnSuspend)
                    await oldTop.SuspendAsyncInternal();
                else
                    await oldTop.CloseAsyncInternal();
            }

            _fullscreenStack.AddFirst(panel);
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.OpenAsyncInternal();
        }
        else // Overlay
        {
            _overlays.Add(panel);
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.OpenAsyncInternal();
        }

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
        if (panel.Kind == UIPanelKind.Fullscreen)
        {
            var oldTop = _fullscreenStack.First?.Value;
            if (oldTop == panel)
            {
                // 已经在栈顶，仅置 SiblingIndex
                panel.transform.SetAsLastSibling();
                if (!panel.Visible) await panel.ResumeAsyncInternal();
                return;
            }

            // 旧栈顶按策略 Suspend / Close
            if (oldTop != null && oldTop.Visible)
            {
                if (oldTop.KeepAliveOnSuspend)
                    await oldTop.SuspendAsyncInternal();
                else
                    await oldTop.CloseAsyncInternal();
            }

            // 自己移到栈顶
            _fullscreenStack.Remove(panel);
            _fullscreenStack.AddFirst(panel);
            panel.transform.SetAsLastSibling();

            // 如果之前是 Suspend 状态 → Resume；否则冷启动 Open
            if (panel.Visible) return;
            if (oldTop != null && oldTop != panel && oldTop.KeepAliveOnSuspend)
                await panel.ResumeAsyncInternal();
            else
                await panel.OpenAsyncInternal();
        }
        else // Overlay
        {
            // Overlay 仅置 SiblingIndex，无 Suspend/Resume 概念
            panel.transform.SetAsLastSibling();
            if (!panel.Visible) await panel.OpenAsyncInternal();
        }
    }

    // ─────────── Close ───────────

    public async UniTask CloseAsync(UIPanel panel = null)
    {
        panel ??= _fullscreenStack.First?.Value;
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

    public async UniTask CloseTopFullscreenAsync()
    {
        var top = _fullscreenStack.First?.Value;
        if (top == null) return;
        await WithLock(() => CloseInternalAsync(top));
    }

    public async UniTask CloseAllAsync()
    {
        // 快照一份避免遍历时修改
        var snapshot = new List<UIPanel>(_fullscreenStack);
        snapshot.AddRange(_overlays);

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
        _fullscreenStack.Clear();
        _overlays.Clear();
        _childrenOf.Clear();
    }

    private async UniTask CloseInternalAsync(UIPanel panel)
    {
        if (!IsInContainers(panel)) return;

        bool wasFullscreen = panel.Kind == UIPanelKind.Fullscreen;

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

        // 6. 若是 Fullscreen 且栈里还有下一张 → 显示新栈顶
        if (wasFullscreen)
        {
            var next = _fullscreenStack.First?.Value;
            if (next != null && !next.Visible)
            {
                if (next.gameObject.activeSelf)
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
        foreach (var p in _fullscreenStack) if (p is T && p.Visible) return true;
        foreach (var p in _overlays) if (p is T && p.Visible) return true;
        return false;
    }

    public bool IsOpen(UIPanel panel) => panel != null && panel.Visible && IsInContainers(panel);

    public bool IsLoaded<T>() where T : UIPanel
    {
        foreach (var p in _uiPanels) if (p is T) return true;
        return false;
    }

    public bool IsLoaded(UIPanel panel) => panel != null && _uiPanels.Contains(panel);

    public int GetVisiblePanelCount() => _fullscreenStack.Count + _overlays.Count;

    public bool HasAnyPanelVisible() => GetVisiblePanelCount() > 0;

    public UIPanel GetTopFullscreen() => _fullscreenStack.First?.Value;

    public UIPanel GetTopPanel()
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
