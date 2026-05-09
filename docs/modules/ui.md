# UI 系统

K-Framework UI 管理模块。栈式 Fullscreen + 共存 Overlay + 父子级联 + Suspend/Resume 状态保留 + 基于 UniTask 的异步 API + 可扩展淡入淡出动画。

## 设计原则

- **栈/容器管理与面板生命周期分层**：业务只调 `UIManager`；`UIPanel` 仅暴露字段、信号与可重写回调，所有"打开/关闭/置前"操作必须经 UIManager
- **没有同步包装**：所有改变状态的方法返回 `UniTask`；fire-and-forget 必须显式 `.Forget()`
- **唯一类型源**：`UIPanelKind { Fullscreen, Overlay }`

## 架构

```
UIManager (PersistentSingleton, IUIService)
  ├─ PanelAnimation   : UIAnimation             全局显隐动画
  ├─ _fullscreenStack : LinkedList<UIPanel>     栈式独占
  ├─ _overlays        : List<UIPanel>           叠加层
  ├─ _childrenOf      : Dict<UIPanel, List>     父子关系索引
  └─ _pendingOp       : UniTask                 串行锁

UIAnimation (抽象基类)
  ├─ UIAnimationFade  : DOTween CanvasGroup 淡入淡出
  ├─ UIAnimationMMF   : MMF_Player 包装
  └─ (自定义子类)     : 程序化动画、第三方 tween 库

UIPanel (MonoBehaviour)
  ├─ Kind : UIPanelKind { Fullscreen, Overlay }
  ├─ ParentPanel : UIPanel
  ├─ KeepAliveOnSuspend : bool
  ├─ PanelAnimation : UIAnimation
  └─ OpenFx/CloseFx : MMF_Player
```

## Panel 类型

| Kind | 语义 | 用例 |
|------|------|------|
| `Fullscreen` | 栈式独占。压栈时按 `KeepAliveOnSuspend` 选择 Suspend 或 Close 旧栈顶 | 主菜单、设置、背包 |
| `Overlay` | 与当前 Fullscreen 共存，多层并列 | HUD、Toast、Tooltip |

## Open / Close / Suspend / Resume 生命周期

| 状态 | Visible | activeSelf | 订阅 | BGM |
|------|---------|------------|------|-----|
| Open | true | true | 保留 | 播放 |
| Suspend | false | false | **保留** | **保留** |
| Close | false | false | DisconnectAll | PopTrack |

## 异步 API

### 栈/容器操作

```csharp
UniTask<T>       PushAsync<T>(UIPanel parent = null)
UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null)
UniTask          BringToFrontAsync(UIPanel panel)
UniTask          BringToFrontAsync<T>()
UniTask          CloseAsync(UIPanel panel = null)
UniTask<T>       CloseAsync<T>()
UniTask          CloseTopFullscreenAsync()
UniTask          CloseAllAsync()
```

### 资源加载

```csharp
UniTask<T>       RequireAsync<T>(string path = null)
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
bool             IsOpen<T>()
bool             IsOpen(UIPanel panel)
bool             IsLoaded<T>()
bool             IsLoaded(UIPanel panel)
int              GetVisiblePanelCount()
UIPanel          GetTopFullscreen()
UIPanel          GetTopPanel()
```

## 调用示例

```csharp
// 启用全局淡入淡出
UIManager.Instance.PanelAnimation = new UIAnimationFade { Duration = 0.3f };

// 打开背包
var backpack = await UIManager.Instance.PushAsync<UIBackpackPanel>();
backpack.Initialize(character);

// 关闭当前栈顶
await UIManager.Instance.CloseAsync();

// HUD（Overlay）
await UIManager.Instance.PushAsync<UIGameplayPanel>();

// Fire-and-forget（按钮回调）
_backButton.onClick.AddListener(() =>
    UIManager.Instance.CloseAsync(this).Forget());

// 监听动画事件
UIManager.Instance.PanelAnimation.OnOpenEnd.Connect(panel =>
{
    if (panel is UIMainMenuPanel) StartBackgroundMusic();
});
```

## 过渡动画

UIPanel 显隐时按优先级解析动画：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | `UIPanel.PanelAnimation` | 面板级覆盖 |
| 2 | `UIManager.Instance.PanelAnimation` | 全局动画 |
| 3 | `OpenFx` / `CloseFx` | MMF_Player 自动包装 |

### 自定义动画

只需继承 `UIAnimation` 并实现 `PlayOpenAsync` / `PlayCloseAsync`：

```csharp
public class MyCustomAnim : UIAnimation
{
    public override async UniTask PlayOpenAsync(CanvasGroup cg, CancellationToken ct)
    {
        // 自定义打开动画
    }

    public override async UniTask PlayCloseAsync(CanvasGroup cg, CancellationToken ct)
    {
        // 自定义关闭动画
    }
}
```

## 信号一览

| 信号 | 类型 | 时机 |
|------|------|------|
| `OnPanelBeginOpen` | `KSignal` | SetActive 之后、动画之前 |
| `OnPanelOpen` | `KSignal` | 动画完成、`OnOpen()` 之后 |
| `OnPanelBeginClose` | `KSignal` | 关闭开始时 |
| `OnPanelClose` | `KSignal` | 动画完成、`OnClose()` 之后 |
| `OnPanelBeginSuspend` | `KSignal` | 挂起开始时 |
| `OnPanelSuspend` | `KSignal` | 挂起完成时 |
| `OnPanelBeginResume` | `KSignal` | 恢复开始时 |
| `OnPanelResume` | `KSignal` | 恢复完成时 |
