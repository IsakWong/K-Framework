# UI 模块

K-Framework UI 管理模块。四层 Canvas + Window 栈 + Popup 叠层 + Service 覆盖层 + 父子级联 + 基于 UniTask 的异步 API + 可扩展动画。

## 设计原则

- **层是静态的，容器是动态的**：`UILayer` 决定渲染层级（Canvas sortingOrder），层内策略各不相同——Window 是栈，其余是集合
- **栈/容器管理与面板生命周期分层**：业务只调 `UIService`；`UIPanel` 仅暴露字段、信号与可重写回调
- **没有同步包装**：所有改变状态的方法返回 `UniTask`；fire-and-forget 必须显式 `.Forget()`
- **唯一类型源**：`UILayer { Hud, Window, Popup, Service }`

## 架构

```
UIService (PersistentSingleton, IUIService)
  ├─ PanelAnimation : UIAnimation              全局显隐动画（可选）
  ├─ _hud      : List<UIPanel>                 HUD：常驻集合，不参与栈
  ├─ _windows  : LinkedList<UIPanel>           Window：栈，只有栈顶可见；First = 栈顶
  ├─ _popups   : List<UIPanel>                 Popup：叠在 Window 之上，多层并列
  ├─ _services : List<UIPanel>                 Service：Loading/Toast，永远最上，CloseAll 不清
  ├─ _childrenOf : Dict<UIPanel, List>         父子关系索引，父 Close 时级联
  ├─ _layerRoots : Dict<UILayer, RectTransform> 每层的嵌套 Canvas 根节点
  └─ _pendingOp : UniTask                      串行锁，防止动画中栈被撕裂

UIAnimation (抽象基类)
  ├─ UIAnimationFade  : DOTween CanvasGroup 淡入淡出
  ├─ UIAnimationMMF   : MMF_Player 包装
  └─ (自定义子类)     : 程序化动画、第三方 tween 库

UIPanel (MonoBehaviour)
  ├─ Layer : UILayer                           面板所属层
  ├─ ParentPanel : UIPanel                     可选父面板引用
  ├─ PanelAnimation : UIAnimation              面板级动画覆盖
  └─ internal Open/Close/Suspend/ResumeAsyncInternal   仅 UIService 调用
```

## 层模型

| Layer | 容器策略 | sortingOrder | 用例 |
|-------|----------|--------------|------|
| `Hud` | 常驻集合，Push/Close 不影响其它层 | 0 | 血条、Boss 条、快捷栏、触屏按钮 |
| `Window` | 严格栈式，压栈时旧栈顶 **Suspend**（保留订阅/BGM/状态），关闭后自动 Resume 下一张 | 100 | 主菜单、背包、图鉴、附魔台、失败菜单 |
| `Popup` | 多层并列，层内置顶 | 200 | 设置、暂停、Tooltip、确认框、对话横条 |
| `Service` | 永远在最上；`CloseAllAsync` 不会清掉 | 300 | Loading 遮罩、Toast |

每层是根 Canvas 下的一个嵌套 Canvas（`overrideSorting = true` + 独立 `GraphicRaycaster`），
层与层之间的层级**由 sortingOrder 固定，与 Push 顺序无关**；层内顺序才由 `SetAsLastSibling` 决定。

## Panel 生命周期

| 状态 | Visible | activeSelf | 订阅 | BGM | IsSuspended |
|------|---------|------------|------|-----|-------------|
| Open | true | true | 保留 | 播放 | false |
| Suspend（仅 Window 非栈顶） | false | false | **保留** | **保留** | true |
| Close | false | false | DisconnectAll | PopTrack | false |

- `OnOpen()` / `OnClose()` —— 完整生命周期，业务可重写。`OnClose` 默认 `subscriber.DisconnectAll() + SetActive(false) + PopTrack`
- `OnSuspend()` / `OnResume()` —— Window 被压栈/回到栈顶时调用，默认空实现
- Suspend 只做 `SetActive(false)`，**不断订阅、不动 BGM**，因此状态天然保留

## 父子界面

`UIPanel.ParentPanel` 指定父面板。`PushAsync` 时把子注册到 `_childrenOf[parent]`；父被 Close 时倒序级联 Close 所有子，**无论子在哪个层**。

```csharp
await UIService.Instance.PushAsync<UISettingsPanel>(parent: mainMenuPanel);
```

## 异步 API

所有改变栈状态的方法返回 `UniTask` / `UniTask<T>`，内部 `await` 资源加载 + 过渡动画。

### 容器操作

```csharp
UniTask<T>       PushAsync<T>(UIPanel parent = null, Action<T> configure = null)
UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null)
UniTask          PushWithCompanionsAsync(UIPanel host, params UIPanel[] companions)
UniTask          PushBatchAsync(params UIPanel[] panels)
UniTask          BringToFrontAsync(UIPanel panel)
UniTask          BringToFrontAsync<T>()
UniTask          CloseAsync(UIPanel panel = null)       // null = Window 栈顶
UniTask<T>       CloseAsync<T>()
UniTask          CloseTopWindowAsync()
UniTask          CloseAllAsync()                        // 关 Hud/Window/Popup，保留 Service
```

### 资源加载

```csharp
UniTask<T>       RequireAsync<T>(string path = null)    // 加载预制体，不入容器（可预热）
UniTask<UIPanel> RequireAsync(Type type, string path = null)
```

### 只读查询（同步）

```csharp
T                GetUI<T>()
bool             IsOpen<T>()                            // 在任一层容器内且 Visible
bool             IsLoaded<T>()                          // 已实例化（不要求在容器内）
int              GetVisiblePanelCount()
UIPanel          GetTopWindow()                         // Window 栈顶
UIPanel          GetTopPanel()                          // Service > Popup > Window > Hud
IReadOnlyCollection<UIPanel> GetWindowStack()
IReadOnlyList<UIPanel>       GetLayerPanels(UILayer layer)
```

## 核心流程

**`PushAsync(panel)`**

1. 若 panel 已在任一层 → 转交 `BringToFrontAsync(panel)`
2. `Window`：旧栈顶 `SuspendAsyncInternal()` → 入栈 → `OpenAsyncInternal()`
3. `Hud` / `Popup` / `Service`：直接入对应集合 → `OpenAsyncInternal()`，不影响其它面板
4. 有父引用 → 注册进 `_childrenOf[parent]`

**`CloseAsync(panel)`**

1. 倒序级联关闭子面板（companion 并行）
2. 播放关闭动画 → `OnClose`
3. 从所属层移除；从父的 children 列表移除
4. 若 panel 是 Window → 栈顶下一张 `ResumeAsyncInternal()`

**`CloseAllAsync()`**：按 Popup → Window → Hud 顺序关闭，**Service 层不动**——转场遮罩不会因为清 UI 被误关。

## 串行锁

`PushAsync` / `CloseAsync` / `BringToFrontAsync` 可能被快速连按触发。`WithLock(UniTask op)` 把每次操作排队到 `_pendingOp` 之后，保证一次只有一个栈操作在途，避免动画过程中栈被撕裂。

## 过渡动画

`GetEffectiveAnimation()` 优先级：`UIPanel.PanelAnimation` → `UIService.PanelAnimation` → 无。

`OpenAsyncInternal` / `CloseAsyncInternal` 在 `OnPanelBeginOpen` / `OnPanelBeginClose` 之后执行动画，`await` 完成后才触发 `OnPanelOpen` / `OnPanelClose`。Suspend/Resume 复用同一套动画。

### 启用全局淡入淡出

```csharp
UIService.Instance.PanelAnimation = new UIAnimationFade
{
    Duration = 0.4f,
    Ease = Ease.OutCubic
};
```

## 信号一览

| 信号 | 类型 | 时机 |
|------|------|------|
| `OnPanelBeginOpen` | `KSignal` | SetActive 之后、动画之前 |
| `OnPanelOpen` | `KSignal` | 动画完成、`OnOpen()` 之后 |
| `OnPanelBeginClose` | `KSignal` | 关闭开始时 |
| `OnPanelClose` | `KSignal` | 动画完成、`OnClose()` 之后 |
| `OnPanelBeginSuspend` / `OnPanelSuspend` | `KSignal` | Window 被压栈（开始/完成） |
| `OnPanelBeginResume` / `OnPanelResume` | `KSignal` | Window 回到栈顶（开始/完成） |
| `OnOpenStart` / `OnOpenEnd` | `KSignal<UIPanel>` | 全局打开动画起止 |
| `OnCloseStart` / `OnCloseEnd` | `KSignal<UIPanel>` | 全局关闭动画起止 |

## 调用示例

```csharp
// 打开背包（Window，自动挂起栈顶）
var backpack = await UIService.Instance.PushAsync<UIBackpackPanel>();
backpack.Initialize(character);

// 关闭当前 Window 栈顶（下一张自动 Resume）
await UIService.Instance.CloseAsync();

// HUD（常驻，不参与栈）
await UIService.Instance.PushAsync<UIGameplayPanel>();

// 设置叠在暂停面板之上（Popup）
await UIService.Instance.PushAsync<UISettingsPanel>(parent: pausePanel);

// Loading（Service，永远不会被其它面板顶掉）
await UIService.Instance.PushAsync(loadingPanel);

// Fire-and-forget（按钮回调）
_backButton.onClick.AddListener(() =>
    UIService.Instance.CloseAsync(this).Forget());
```

## 依赖

- **Cysharp UniTask** — 异步 API
- **DOTween / MoreMountains Feel** — 过渡动画
- **Addressables** — `AssetService.LoadAsync<T>()`
- **Sirenix Odin Inspector** — `[LabelText]` 编辑器标签
