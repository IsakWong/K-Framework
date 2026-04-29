# UI 模块

K-Framework UI 管理模块。栈式 Fullscreen + 共存 Overlay + 父子级联 + 基于 UniTask 的异步 API + 可扩展淡入淡出动画。

## 架构

```
UIManager (PersistentSingleton, IUIService)
  ├─ PanelAnimation   : UIAnimation             全局显隐动画（可选，优先于 ShowFx）
  ├─ _fullscreenStack : LinkedList<UIPanel>     栈式独占，只有栈顶可见
  ├─ _overlays        : List<UIPanel>           与 Fullscreen 共存的叠加层
  ├─ _childrenOf      : Dict<UIPanel, List>     父子关系索引，父 Pop 时级联
  └─ _pendingOp       : UniTask                 串行锁，防止动画中栈被撕裂

UIAnimation (抽象基类)
  ├─ UIAnimationFade  : DOTween CanvasGroup 淡入淡出
  ├─ UIAnimationMMF   : MMF_Player 包装（自动或手动）
  └─ (自定义子类)     : 程序化动画、第三方 tween 库等

UIPanel (MonoBehaviour)
  ├─ Kind : UIPanelKind { Fullscreen, Overlay }
  ├─ ParentPanel : UIPanel                     可选父面板引用
  ├─ PanelAnimation : UIAnimation              面板级动画覆盖
  ├─ ShowFx/HideFx : MMF_Player                MMF 动画源（自动包装为 UIAnimationMMF）
  └─ async Show/HidePanelAsync                 GetEffectiveAnimation() 统一解析
```

## Panel 类型

| Kind | 语义 | 用例 |
|------|------|------|
| `Fullscreen` | 栈式独占。压栈时先 `await` 上一张的 `HidePanelAsync()`，再入栈显示新的；Pop 后自动显示新栈顶 | 主菜单、设置、背包、选择面板 |
| `Overlay` | 与当前 Fullscreen 共存，多层并列；Push 顺序决定渲染层级 | HUD、Toast、Tooltip、通知 |

`Kind` 是运行时主分类源。旧字段 `FullscreenPanel` 标为 `[HideInInspector]` 保留，`OnValidate()` 自动同步到 `Kind`，老预制体无需手动迁移。

## 父子界面

`UIPanel.ParentPanel` 指定父面板。`PushUIAsync` 时把子注册到 `_childrenOf[parent]`；父被 Pop 时，倒序级联 Pop 所有子，**无论子是 Fullscreen 还是 Overlay**。

```csharp
// 子 Panel 在 Inspector 中指定 ParentPanel = 主菜单
// 关闭主菜单时，设置面板、Codex 等子面板自动一起关闭
await UIManager.Instance.PopUIAsync<UIMainMenuPanel>();
```

也可通过 API 显式指定父：

```csharp
await UIManager.Instance.PushUIAsync<UISettingsPanel>(parent: mainMenuPanel);
```

## 异步 API

所有改变栈状态的方法返回 `UniTask` / `UniTask<T>`，内部 `await` 资源加载 + 过渡动画。同步的只读查询保持不变。

### 栈操作

```csharp
UniTask<T>       PushUIAsync<T>(UIPanel parent = null)   // 加载预制体 + 压栈
UniTask<UIPanel> PushUIAsync(UIPanel panel, UIPanel parent = null)
UniTask          PopUIAsync(UIPanel panel = null)         // null = 栈顶 Fullscreen
UniTask<T>       PopUIAsync<T>()
UniTask          PopFullscreenAsync()                     // Pop 栈顶 Fullscreen，保留 Overlay
UniTask          PopAllAsync()
UniTask          CloseUIAsync(UIPanel panel)              // = PopUIAsync
```

### 资源加载

```csharp
UniTask<T>       RequireUIAsync<T>()                      // 加载预制体，不压栈（可预热）
UniTask<UIPanel> RequireUIAsync(Type type)
```

### 销毁

```csharp
UniTask DestroyUIAsync<T>()
UniTask DestroyUIAsync(UIPanel panel)
UniTask DestroyAllUIAsync()
```

### 只读查询（同步）

```csharp
T                GetUI<T>()
bool             IsUIVisible<T>()
bool             IsUIVisible(UIPanel panel)
int              GetVisiblePanelCount()
UIPanel          GetTopmostFullscreen()
UIPanel          GetTopmostPanel()                        // Overlay 优先于 Fullscreen 栈顶
IReadOnlyCollection<UIPanel> GetFullscreenStack()
IReadOnlyList<UIPanel>       GetOverlays()
```

## Push / Pop 核心流程

**`PushInternalAsync(panel, parent)`**

1. 若 panel 已在任一容器 → 先从旧位置移除（不触发 Hide）
2. 若 `panel.Kind == Fullscreen`：栈顶若有其它 Fullscreen → `await oldTop.HidePanelAsync()`；然后 `_fullscreenStack.AddFirst(panel)` + `await panel.ShowPanelAsync()`
3. 若 `panel.Kind == Overlay`：`_overlays.Add(panel)` + `await panel.ShowPanelAsync()`
4. 有父引用 → 注册进 `_childrenOf[parent]`

**`PopInternalAsync(panel)`**

1. 倒序级联 `await PopInternalAsync(child)` 所有子面板
2. `await panel.HidePanelAsync()`
3. 从所属容器移除；从父的 children 列表移除
4. 若 panel 是 Fullscreen → 显示新栈顶 Fullscreen（`await ShowPanelAsync`）

## 串行锁

`PushUIAsync` / `PopUIAsync` 可能被快速连按触发（例如连按 Tab 开关背包）。`WithLock(UniTask op)` 把每次操作排队到 `_pendingOp` 之后，保证一次只有一个栈操作在途，避免动画过程中栈被撕裂。

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
| 3 | `ShowFx` / `HideFx` | MMF_Player，自动包装为 `UIAnimationMMF` | |

`ShowPanelAsync` / `HidePanelAsync` 会在 `OnPanelBeginShow` / `OnPanelBeginHide` 信号之后执行动画，`await` 动画完成后才调用 `OnShow()` / `OnHide()` 触发 `OnPanelShow` / `OnPanelHide`。

### 启用全局淡入淡出

```csharp
// 在游戏初始化时一行启用
UIManager.Instance.PanelAnimation = new UIAnimationFade
{
    Duration = 0.4f,
    Ease = Ease.OutCubic
};

// 此后所有 Push/Pop 自动淡入淡出，无需修改任何 Panel 预制体
```

### 动画信号

`UIAnimation` 基类提供四个 `KSignal<UIPanel>` 信号，可在任意位置监听：

```csharp
var anim = UIManager.Instance.PanelAnimation;

// 监听所有 Panel 的动画事件
anim.OnShowStart.Connect(panel => Debug.Log($"{panel.name} 开始显示"));
anim.OnShowEnd.Connect(panel => Debug.Log($"{panel.name} 显示完成"));
anim.OnHideStart.Connect(panel => Debug.Log($"{panel.name} 开始隐藏"));
anim.OnHideEnd.Connect(panel => Debug.Log($"{panel.name} 隐藏完成"));

// 例如配合输入屏蔽层，在动画期间拦截点击
anim.OnShowStart.Connect(_ => inputBlocker.SetActive(true));
anim.OnShowEnd.Connect(_ => inputBlocker.SetActive(false));
```

## 动画扩展

`IUIAnimation` 接口 + `UIAnimation` 抽象基类的设计允许自由替换动画实现。只需继承 `UIAnimation` 并实现 `PlayShowAsync` / `PlayHideAsync`。

### 内置：UIAnimationFade（DOTween）

```csharp
public class UIAnimationFade : UIAnimation
{
    public Ease Ease = Ease.OutCubic;

    public override async UniTask PlayShowAsync(CanvasGroup cg, CancellationToken ct)
    {
        cg.alpha = 0f;
        await cg.DOFade(1f, Duration).SetEase(Ease).ToUniTask(cancellationToken: ct);
    }

    public override async UniTask PlayHideAsync(CanvasGroup cg, CancellationToken ct)
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
    public MMF_Player ShowPlayer { get; set; }
    public MMF_Player HidePlayer { get; set; }

    public override async UniTask PlayShowAsync(CanvasGroup cg, CancellationToken ct)
    {
        if (ShowPlayer == null) return;
        ShowPlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !ShowPlayer.IsPlaying, cancellationToken: ct);
    }

    public override async UniTask PlayHideAsync(CanvasGroup cg, CancellationToken ct)
    {
        if (HidePlayer == null) return;
        HidePlayer.PlayFeedbacks();
        await UniTask.WaitUntil(() => !HidePlayer.IsPlaying, cancellationToken: ct);
    }
}
```

UIPanel 在 `GetEffectiveAnimation()` 中会自动将 `ShowFx` / `HideFx` 包装为 `UIAnimationMMF` 实例。也可手动创建并赋给 `PanelAnimation`：

### 扩展：自定义动画（示例）

```csharp
public class UIAnimationScale : UIAnimation
{
    public Vector3 ShowScale = Vector3.one;
    public Vector3 HideScale = Vector3.zero;

    public override async UniTask PlayShowAsync(CanvasGroup cg, CancellationToken ct)
    {
        cg.transform.localScale = Vector3.zero;
        await cg.transform.DOScale(ShowScale, Duration).SetEase(Ease.OutBack).ToUniTask(cancellationToken: ct);
    }

    public override async UniTask PlayHideAsync(CanvasGroup cg, CancellationToken ct)
    {
        await cg.transform.DOScale(HideScale, Duration).SetEase(Ease.InBack).ToUniTask(cancellationToken: ct);
    }
}
```

### 设计要点

- **CanvasGroup 由 UIPanel 自动创建**：`ShowPanelAsync` 检测到全局动画时自动 `AddComponent<CanvasGroup>()`，开发者无需在预制体上手动添加
- **对 Panel 透明**：Panel 子类不需要感知动画实现，继续重写 `OnShow()` / `OnHide()` 即可
- **动画完成后才触发 OnPanelShow**：BGM、业务逻辑确保在视觉可见之后才执行
- **向后兼容**：未设置 `PanelAnimation` 时行为与旧版完全一致

## 信号一览

| 信号 | 类型 | 来源 | 时机 |
|------|------|------|------|
| `OnPanelBeginShow` | `KSignal` | UIPanel | SetActive 之后、动画之前 |
| `OnPanelShow` | `KSignal` | UIPanel | 动画完成、BGM 播放之后 |
| `OnPanelBeginHide` | `KSignal` | UIPanel | 隐藏开始时 |
| `OnPanelHide` | `KSignal` | UIPanel | 动画完成、SetActive(false) 之后 |
| `OnShowStart` | `KSignal<UIPanel>` | UIAnimation | 全局显示动画开始时 |
| `OnShowEnd` | `KSignal<UIPanel>` | UIAnimation | 全局显示动画完成时 |
| `OnHideStart` | `KSignal<UIPanel>` | UIAnimation | 全局隐藏动画开始时 |
| `OnHideEnd` | `KSignal<UIPanel>` | UIAnimation | 全局隐藏动画完成时 |

## 调用示例

```csharp
// 启用全局淡入淡出（Init 阶段一次调用）
UIManager.Instance.PanelAnimation = new UIAnimationFade { Duration = 0.3f };

// 打开背包（Fullscreen，自动淡入）
var backpack = await UIManager.Instance.PushUIAsync<UIBackpackPanel>();
backpack.Initialize(character);

// 关闭当前栈顶 Fullscreen（自动淡出，下一张淡入）
await UIManager.Instance.PopUIAsync();

// HUD（Overlay，Kind = Overlay）
await UIManager.Instance.PushUIAsync<UIGameplayPanel>();

// Fire-and-forget（按钮 UnityEvent 回调）
_backButton.onClick.AddListener(() =>
    UIManager.Instance.PopUIAsync(this).Forget());

// 在 IEnumerator 协程中等待
yield return UIManager.Instance.PopUIAsync<UILoadingPanel>().ToCoroutine();

// 监听动画事件
UIManager.Instance.PanelAnimation.OnShowEnd.Connect(panel =>
{
    if (panel is UIMainMenuPanel) StartBackgroundMusic();
});
```

## 依赖

- **Cysharp UniTask** 2.5.10 — 通过 `Packages/manifest.json` 引入；`KFramework.asmdef` 需 `references` 中添加 `UniTask` + `UniTask.DOTween`
- **DOTween** — `UIAnimationFade` 的淡入淡出；`UIAnimationFade` 依赖 `UniTask.DOTween` 的 `ToUniTask()` 扩展
- **MoreMountains Feel** — `MMF_Player` 过渡动画（后备方案，全局动画启用后可移除依赖）
- **Addressables** — `AssetManager.LoadAssetAsync<T>()`
- **Sirenix Odin Inspector** — `[LabelText]` 等编辑器标签

## 改造背景

本模块于 2026-04 重写，改造前存在四个问题：

1. **语义不清**：单 `LinkedList<UIPanel> _visibleStack` 混合存 Fullscreen + Normal，用 `FullscreenPanel` bool 现场过滤
2. **父子界面仅声明未实现**：`ParentPanel` 字段存在但无级联逻辑
3. **同步 API + 回调等待**：`RequireUI` 同步加载首次卡顿；`ShowFx/HideFx` 通过 `OnComplete.AddListener` 回调，压多个 Fullscreen 时出现 "上一个还没隐藏完、下一个已压栈" 的竞态
4. **调用方无法等待**：协程里无法 `yield return` UI 操作

2026-04 v2 更新：引入 `IUIAnimation` / `UIAnimation` 可扩展动画系统与 `UIAnimationFade` DOTween 实现，Panel 显隐过渡从分散的 `ShowFx/HideFx` 统一为可替换的全局动画管道。
