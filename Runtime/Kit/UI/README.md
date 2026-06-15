# UI 模块

K-Framework UI 管理模块。栈式 Fullscreen + 共存 Overlay + 父子级联 + Suspend/Resume 状态保留 + 基于 UniTask 的异步 API + 可扩展淡入淡出动画。

## 设计原则

- **栈/容器管理与面板生命周期分层**：业务只调 `UIManager`；`UIPanel` 仅暴露字段、信号与可重写回调，所有"打开/关闭/置前"操作必须经 UIManager
- **没有同步包装**：所有改变状态的方法返回 `UniTask`；fire-and-forget 必须显式 `.Forget()`，避免异常被静默吞掉
- **唯一类型源**：`UIPanelKind { Fullscreen, Overlay }`，`Kind` 字段是唯一来源（无遗留 bool 字段）

## 架构

```
UIManager (PersistentSingleton, IUIService)
  ├─ PanelAnimation   : UIAnimation             全局显隐动画（可选，优先于 OpenFx）
  ├─ _fullscreenStack : LinkedList<UIPanel>     栈式独占，只有栈顶可见
  ├─ _overlays        : List<UIPanel>           与 Fullscreen 共存的叠加层
  ├─ _childrenOf      : Dict<UIPanel, List>     父子关系索引，父 Close 时级联
  └─ _pendingOp       : UniTask                 串行锁，防止动画中栈被撕裂

UIAnimation (抽象基类)
  ├─ UIAnimationFade  : DOTween CanvasGroup 淡入淡出
  ├─ UIAnimationMMF   : MMF_Player 包装（自动或手动）
  └─ (自定义子类)     : 程序化动画、第三方 tween 库等

UIPanel (MonoBehaviour)
  ├─ Kind : UIPanelKind { Fullscreen, Overlay }
  ├─ ParentPanel : UIPanel                     可选父面板引用
  ├─ KeepAliveOnSuspend : bool                 切栈时走 Suspend（保留状态）而非 Close
  ├─ PanelAnimation : UIAnimation              面板级动画覆盖
  ├─ OpenFx/CloseFx : MMF_Player               MMF 动画源（自动包装为 UIAnimationMMF）
  └─ internal Open/Close/Suspend/ResumeAsyncInternal   仅 UIManager 调用
```

## Panel 类型

| Kind | 语义 | 用例 |
|------|------|------|
| `Fullscreen` | 栈式独占。压栈时按 `KeepAliveOnSuspend` 选择 Suspend 旧栈顶或 Close 旧栈顶；新栈顶 Resume/Open | 主菜单、设置、背包、选择面板 |
| `Overlay` | 与当前 Fullscreen 共存，多层并列；Push 顺序决定渲染层级 | HUD、Toast、Tooltip、通知 |

`Kind` 是 Panel 类型的唯一来源。无 `FullscreenPanel` 兼容字段。

## Open / Close / Suspend / Resume 生命周期

| 状态 | Visible | activeSelf | 订阅 | BGM |
|------|---------|------------|------|-----|
| Open | true | true | 保留 | 播放 |
| Suspend（仅 KeepAliveOnSuspend=true） | false | false | **保留** | **保留** |
| Close | false | false | DisconnectAll | PopTrack |

- `OnOpen()` / `OnClose()` —— 完整生命周期，业务可重写。`OnClose` 默认 `subscriber.DisconnectAll() + SetActive(false) + PopTrack`
- `OnSuspend()` / `OnResume()` —— 切栈保留状态时调用，默认空实现，业务按需重写

`KeepAliveOnSuspend` 默认 `false`，行为退化为 Close（与切栈状态丢失的旧行为一致）。需要保留状态的 Panel 显式开启即可。

## 父子界面

`UIPanel.ParentPanel` 指定父面板。`PushAsync` 时把子注册到 `_childrenOf[parent]`；父被 Close 时，倒序级联 Close 所有子，**无论子是 Fullscreen 还是 Overlay**。

```csharp
// 子 Panel 在 Inspector 中指定 ParentPanel = 主菜单
// 关闭主菜单时，设置面板、Codex 等子面板自动一起关闭
await UIManager.Instance.CloseAsync<UIMainMenuPanel>();
```

也可通过 API 显式指定父：

```csharp
await UIManager.Instance.PushAsync<UISettingsPanel>(parent: mainMenuPanel);
```

## 异步 API

所有改变栈状态的方法返回 `UniTask` / `UniTask<T>`，内部 `await` 资源加载 + 过渡动画。同步的只读查询保持不变。

### 栈/容器操作

```csharp
UniTask<T>       PushAsync<T>(UIPanel parent = null)         // 加载预制体 + 入容器 + Open
UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null)
                                                              // 已在容器内 → 转交 BringToFrontAsync
UniTask          BringToFrontAsync(UIPanel panel)             // 已在容器：切到前台（Fullscreen 触发 Suspend/Resume）
UniTask          BringToFrontAsync<T>()
UniTask          CloseAsync(UIPanel panel = null)             // null = 栈顶 Fullscreen
UniTask<T>       CloseAsync<T>()
UniTask          CloseTopFullscreenAsync()                    // 关栈顶 Fullscreen，保留 Overlay
UniTask          CloseAllAsync()
```

### 资源加载

```csharp
UniTask<T>       RequireAsync<T>(string path = null)          // 加载预制体，不入容器（可预热）
UniTask<UIPanel> RequireAsync(Type type, string path = null)
```

### 销毁

```csharp
UniTask DestroyAsync<T>()
UniTask DestroyAsync(UIPanel panel)
UniTask DestroyAllAsync()
```

### 只读查询（同步）

```csharp
T                GetUI<T>()
bool             IsOpen<T>()                                  // 在容器内且 Visible
bool             IsOpen(UIPanel panel)
bool             IsLoaded<T>()                                // 已实例化（不要求在容器内）
bool             IsLoaded(UIPanel panel)
int              GetVisiblePanelCount()
UIPanel          GetTopFullscreen()
UIPanel          GetTopPanel()                                // Overlay 优先于 Fullscreen 栈顶
IReadOnlyCollection<UIPanel> GetFullscreenStack()
IReadOnlyList<UIPanel>       GetOverlays()
```

## Push / BringToFront / Close 核心流程

**`PushAsync(panel)`**

1. 若 panel 已在任一容器 → 转交 `BringToFrontAsync(panel)`
2. 若 `panel.Kind == Fullscreen`：栈顶若有其它 Fullscreen → 按 `oldTop.KeepAliveOnSuspend` 选 `SuspendAsyncInternal` 或 `CloseAsyncInternal`；然后 `_fullscreenStack.AddFirst(panel)` + `await panel.OpenAsyncInternal()`
3. 若 `panel.Kind == Overlay`：`_overlays.Add(panel)` + `await panel.OpenAsyncInternal()`
4. 有父引用 → 注册进 `_childrenOf[parent]`

**`BringToFrontAsync(panel)`**（panel 必须已在容器）

- Fullscreen：旧栈顶按 `KeepAliveOnSuspend` 走 Suspend/Close；自己移到栈顶；之前是 Suspend 状态则 `ResumeAsyncInternal`，否则 `OpenAsyncInternal`
- Overlay：仅 `SetAsLastSibling`，按需 Open

**`CloseAsync(panel)`**

1. 倒序级联 `await CloseInternalAsync(child)` 所有子面板
2. `await panel.CloseAsyncInternal()`（Suspend 状态的 Panel 直接关，不重播动画）
3. 从所属容器移除；从父的 children 列表移除
4. 若 panel 是 Fullscreen → 暴露的下一张栈顶按 Suspend/Cold 状态走 `ResumeAsyncInternal` 或 `OpenAsyncInternal`

## 串行锁

`PushAsync` / `CloseAsync` / `BringToFrontAsync` 可能被快速连按触发（例如连按 Tab 开关背包）。`WithLock(UniTask op)` 把每次操作排队到 `_pendingOp` 之后，保证一次只有一个栈操作在途，避免动画过程中栈被撕裂。

```csharp
private async UniTask WithLock(UniTask op)
{
    var previous = _pendingOp;
    var tcs = new UniTaskCompletionSource();
    _pendingOp = tcs.Task;
    try { await previous; await op; }
    finally { tcs.TrySetResult(); }
}
```

## 过渡动画

UIPanel 显隐时通过 `GetEffectiveAnimation()` 按以下优先级解析动画：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | `UIPanel.PanelAnimation` | 面板级覆盖，Inspector 可设 |
| 2 | `UIManager.Instance.PanelAnimation` | 全局动画，所有 Panel 共享 |
| 3 | `OpenFx` / `CloseFx` | MMF_Player，自动包装为 `UIAnimationMMF` |

`OpenAsyncInternal` / `CloseAsyncInternal` 会在 `OnPanelBeginOpen` / `OnPanelBeginClose` 信号之后执行动画，`await` 动画完成后才调用 `OnOpen()` / `OnClose()` 触发 `OnPanelOpen` / `OnPanelClose`。Suspend/Resume 复用同一套动画（CloseAnim 用于 Suspend，OpenAnim 用于 Resume）。

### 启用全局淡入淡出

```csharp
UIManager.Instance.PanelAnimation = new UIAnimationFade
{
    Duration = 0.4f,
    Ease = Ease.OutCubic
};
```

### 动画信号

`UIAnimation` 基类提供四个 `KSignal<UIPanel>` 信号：

```csharp
var anim = UIManager.Instance.PanelAnimation;
anim.OnOpenStart.Connect(panel => Debug.Log($"{panel.name} 开始打开"));
anim.OnOpenEnd.Connect(panel => Debug.Log($"{panel.name} 打开完成"));
anim.OnCloseStart.Connect(panel => Debug.Log($"{panel.name} 开始关闭"));
anim.OnCloseEnd.Connect(panel => Debug.Log($"{panel.name} 关闭完成"));
```

## 动画扩展

`IUIAnimation` 接口 + `UIAnimation` 抽象基类的设计允许自由替换动画实现。只需继承 `UIAnimation` 并实现 `PlayOpenAsync` / `PlayCloseAsync`。

### 内置：UIAnimationFade（DOTween）

```csharp
public class UIAnimationFade : UIAnimation
{
    public Ease Ease = Ease.OutCubic;

    public override async UniTask PlayOpenAsync(CanvasGroup cg, CancellationToken ct)
    {
        cg.alpha = 0f;
        await cg.DOFade(1f, Duration).SetEase(Ease).ToUniTask(cancellationToken: ct);
    }

    public override async UniTask PlayCloseAsync(CanvasGroup cg, CancellationToken ct)
    {
        cg.alpha = 1f;
        await cg.DOFade(0f, Duration).SetEase(Ease).ToUniTask(cancellationToken: ct);
    }
}
```

### 内置：UIAnimationMMF（MMF_Player）

```csharp
public class UIAnimationMMF : UIAnimation
{
    public MMF_Player OpenPlayer { get; set; }
    public MMF_Player ClosePlayer { get; set; }

    public override async UniTask PlayOpenAsync(CanvasGroup cg, CancellationToken ct)
    {
        if (OpenPlayer == null) return;
        OpenPlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !OpenPlayer.IsPlaying, cancellationToken: ct);
    }

    public override async UniTask PlayCloseAsync(CanvasGroup cg, CancellationToken ct)
    {
        if (ClosePlayer == null) return;
        ClosePlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !ClosePlayer.IsPlaying, cancellationToken: ct);
    }
}
```

UIPanel 在 `GetEffectiveAnimation()` 中会自动将 `OpenFx` / `CloseFx` 包装为 `UIAnimationMMF` 实例。

## 信号一览

| 信号 | 类型 | 来源 | 时机 |
|------|------|------|------|
| `OnPanelBeginOpen` | `KSignal` | UIPanel | SetActive 之后、动画之前 |
| `OnPanelOpen` | `KSignal` | UIPanel | 动画完成、`OnOpen()` 之后 |
| `OnPanelBeginClose` | `KSignal` | UIPanel | 关闭开始时 |
| `OnPanelClose` | `KSignal` | UIPanel | 动画完成、`OnClose()` 之后 |
| `OnPanelBeginSuspend` | `KSignal` | UIPanel | 挂起开始时 |
| `OnPanelSuspend` | `KSignal` | UIPanel | 挂起完成时 |
| `OnPanelBeginResume` | `KSignal` | UIPanel | 恢复开始时 |
| `OnPanelResume` | `KSignal` | UIPanel | 恢复完成时 |
| `OnOpenStart` / `OnOpenEnd` | `KSignal<UIPanel>` | UIAnimation | 全局打开动画起止 |
| `OnCloseStart` / `OnCloseEnd` | `KSignal<UIPanel>` | UIAnimation | 全局关闭动画起止 |

## 调用示例

```csharp
// 启用全局淡入淡出（Init 阶段一次调用）
UIManager.Instance.PanelAnimation = new UIAnimationFade { Duration = 0.3f };

// 打开背包（Fullscreen，自动淡入）
var backpack = await UIManager.Instance.PushAsync<UIBackpackPanel>();
backpack.Initialize(character);

// 关闭当前栈顶 Fullscreen（自动淡出，下一张淡入）
await UIManager.Instance.CloseAsync();

// HUD（Overlay，Kind = Overlay）
await UIManager.Instance.PushAsync<UIGameplayPanel>();

// Fire-and-forget（按钮 UnityEvent 回调）
_backButton.onClick.AddListener(() =>
    UIManager.Instance.CloseAsync(this).Forget());

// 在 IEnumerator 协程中等待
yield return UIManager.Instance.CloseAsync<UILoadingPanel>().ToCoroutine();

// 监听动画事件
UIManager.Instance.PanelAnimation.OnOpenEnd.Connect(panel =>
{
    if (panel is UIMainMenuPanel) StartBackgroundMusic();
});

// 切栈保留状态：Inspector 设 KeepAliveOnSuspend = true
// 之后 Push 其它 Fullscreen 时，此 Panel 走 Suspend（保留订阅、BGM、状态），
// CloseAsync 它时再走完整 Close
```

## 依赖

- **Cysharp UniTask** 2.5.10 — 通过 `Packages/manifest.json` 引入；`KFramework.asmdef` 需 `references` 中添加 `UniTask` + `UniTask.DOTween`
- **DOTween** — `UIAnimationFade` 的淡入淡出
- **MoreMountains Feel** — `MMF_Player` 过渡动画
- **Addressables** — `AssetManager.LoadAssetAsync<T>()`
- **Sirenix Odin Inspector** — `[LabelText]` 等编辑器标签

## 改造背景

- **2026-04 v1**：拆分 Fullscreen/Overlay 双容器、引入父子级联、UniTask 化、串行锁
- **2026-04 v2**：引入 `IUIAnimation` 可扩展动画系统与 `UIAnimationFade`
- **2026-04 v3**（本次）：
  - 删除 `FullscreenPanel` 兼容字段，`Kind` 成为唯一类型源
  - 接口分层：`UIManager` 是唯一对外操作入口；`UIPanel` 仅暴露字段/信号/protected 回调
  - 重命名 Show/Hide → Open/Close（API 与字段全面对齐）
  - 删除所有同步包装（`Open()/Close()/Pop()/ShowPanel()/HidePanel()`）；fire-and-forget 必须显式 `.Forget()`
  - 新增 `BringToFrontAsync` —— 已在容器时切到前台
  - 新增 `Suspend/Resume` 生命周期 + `KeepAliveOnSuspend` 字段 —— 切栈可保留状态
  - 重命名 `IsUIVisible → IsOpen`；新增 `IsLoaded`
