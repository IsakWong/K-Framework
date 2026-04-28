# UI 模块

K-Framework UI 管理模块。栈式 Fullscreen + 共存 Overlay + 父子级联 + 基于 UniTask 的异步 API。

## 架构

```
UIManager (PersistentSingleton, IUIService)
  ├─ _fullscreenStack : LinkedList<UIPanel>   栈式独占，只有栈顶可见
  ├─ _overlays        : List<UIPanel>         与 Fullscreen 共存的叠加层
  ├─ _childrenOf      : Dict<UIPanel, List>   父子关系索引，父 Pop 时级联
  └─ _pendingOp       : UniTask                串行锁，防止动画中栈被撕裂

UIPanel (MonoBehaviour)
  ├─ Kind : UIPanelKind { Fullscreen, Overlay }
  ├─ ParentPanel : UIPanel                     可选父面板引用
  ├─ ShowFx/HideFx : MMF_Player                过渡动画（Feel）
  └─ async Show/HidePanelAsync                 UniTask.WaitUntil(!IsPlaying)
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
UniTask<T>      PushUIAsync<T>(UIPanel parent = null)   // 加载预制体 + 压栈
UniTask<UIPanel> PushUIAsync(UIPanel panel, UIPanel parent = null)
UniTask         PopUIAsync(UIPanel panel = null)         // null = 栈顶 Fullscreen
UniTask<T>      PopUIAsync<T>()
UniTask         PopFullscreenAsync()                     // Pop 栈顶 Fullscreen，保留 Overlay
UniTask         PopAllAsync()
UniTask         CloseUIAsync(UIPanel panel)              // = PopUIAsync
```

### 资源加载

```csharp
UniTask<T>      RequireUIAsync<T>()                      // 加载预制体，不压栈（可预热）
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
T      GetUI<T>()
bool   IsUIVisible<T>()
bool   IsUIVisible(UIPanel panel)
int    GetVisiblePanelCount()
UIPanel GetTopmostFullscreen()
UIPanel GetTopmostPanel()                                // Overlay 优先于 Fullscreen 栈顶
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

## UIPanel 过渡动画

`ShowPanelAsync` / `HidePanelAsync` 用 `UniTask.WaitUntil(() => !ShowFx.IsPlaying)` 等待 `MMF_Player` 播放结束，比 `OnComplete.AddListener` 干净，也彻底消除了老版本中靠 `Guid.Connect` 补丁处理的时序 bug。

```csharp
public virtual async UniTask ShowPanelAsync()
{
    Visible = true; Interactable = true;
    if (ShowAudio) SoundManager.Instance.PlaySound(ShowAudio);
    gameObject.SetActive(true);
    OnPanelBeginShow?.Invoke();
    if (ShowFx)
    {
        ShowFx.PlayFeedbacks();
        await UniTask.WaitUntil(() => !ShowFx.IsPlaying,
            cancellationToken: this.GetCancellationTokenOnDestroy());
    }
    OnShow();
}
```

`GetCancellationTokenOnDestroy()` 保证 Panel 被销毁时挂起的 await 自动取消，不会在已销毁对象上继续推进状态。

## 调用示例

```csharp
// 打开背包（Fullscreen）
var backpack = await UIManager.Instance.PushUIAsync<UIBackpackPanel>();
backpack.Initialize(character);

// 关闭当前栈顶 Fullscreen
await UIManager.Instance.PopUIAsync();

// HUD（Overlay，Kind = Overlay）
await UIManager.Instance.PushUIAsync<UIGameplayPanel>();

// Fire-and-forget（按钮 UnityEvent 回调）
_backButton.onClick.AddListener(() =>
    UIManager.Instance.PopUIAsync(this).Forget());

// 在 IEnumerator 协程中等待
yield return UIManager.Instance.PopUIAsync<UILoadingPanel>().ToCoroutine();
```

## 依赖

- **Cysharp UniTask** 2.5.10 — 通过 `Packages/manifest.json` 引入；`KFramework.asmdef` 需 `references` 中添加 `UniTask` + `UniTask.DOTween`
- **MoreMountains Feel** — `MMF_Player` 过渡动画
- **Addressables** — `AssetManager.LoadAssetAsync<T>()`

## 改造背景

本模块于 2026-04 重写，改造前存在四个问题：

1. **语义不清**：单 `LinkedList<UIPanel> _visibleStack` 混合存 Fullscreen + Normal，用 `FullscreenPanel` bool 现场过滤
2. **父子界面仅声明未实现**：`ParentPanel` 字段存在但无级联逻辑
3. **同步 API + 回调等待**：`RequireUI` 同步加载首次卡顿；`ShowFx/HideFx` 通过 `OnComplete.AddListener` 回调，压多个 Fullscreen 时出现 "上一个还没隐藏完、下一个已压栈" 的竞态
4. **调用方无法等待**：协程里无法 `yield return` UI 操作

改造后所有栈操作异步化、双容器分离、父子级联自动、串行锁防竞态。
