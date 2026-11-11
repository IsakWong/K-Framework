# 架构概览

## 分层设计

K-Framework 采用 **4 层分层架构**，自下而上职责递增、依赖递减：

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Extensions Layer（可选扩展）                       │
│   ControllerBase · PlayerModule · HUDBase                          │
├─────────────────────────────────────────────────────────────────────┤
│                     Modules Layer（功能模块）                          │
│   UIService · SoundService · AssetService · ConfigService           │
│   SceneService · PoolService · VfxService · DebugService            │
│   PersistentDataService · SettingsService · CameraInstance          │
├─────────────────────────────────────────────────────────────────────┤
│                      Unity Layer（引擎适配）                           │
│   KGameCore · GameCoreProxy · SceneController · TModule<T>          │
│   PersistentSingleton · KTime · EnhancedLog · Variant · AutoBind    │
├─────────────────────────────────────────────────────────────────────┤
│                       Core Layer（引擎无关）                           │
│   KSingleton<T> · ServiceLocator · IService · ILogService           │
│   KSignal/Subscriber · EventBus · Command · KCoroutine              │
│   CSharpPool<T> · KStateMachine                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 物理目录

目录划分与上图一一对应（详见 `CLAUDE.md` 项目结构）：

- `Runtime/Core/` —— **引擎无关内核**（纯 C#：`IService` / `ServiceLocator` / `KSingleton<T>` / `KSignal` / `EventBus` / `Command` / `KCoroutine` / `CSharpPool<T>` / `KStateMachine`），不引用任何引擎代码。
- `Runtime/Unity/` —— **引擎适配层**（`Hosting/`、`Foundation/`、`Time/`、`Logging/`、`Attributes/`、`Serialization/`、`Extensions/`、`Utilities/`、`Components/`、`Version/`），所有 `MonoBehaviour` / `ScriptableObject` 宿主都在这一层或更上层。
- `Runtime/Modules/` —— **通用功能模块**（`Assets/`、`Camera/`、`Config/`、`Debug/`、`ObjectPool/`、`PersistentData/`、`Scene/`、`Settings/`、`Sound/`、`UI/`、`Vfx/`），接口、实现与组件同目录。
- `Runtime/Extensions/` —— **可选游戏扩展**（`HUD/`、`Player/`）。
- `Runtime/ThirdParty/` —— 内嵌三方（UnityHFSM、Fluid BT、JsonConverter、SerializedCollections、DOTween Modules），各自 namespace / asmdef 不变。

命名空间与目录一致：框架自有代码统一为扁平 `KFramework`，Editor 为 `KFramework.Editor`，Tests 为 `KFramework.Tests`。

## 核心设计模式

| 模式 | 实现 | 用途 |
|---|---|---|
| **Service Locator** | `ServiceLocator` 静态类 | 统一服务注册与发现，接口隔离，支持 Mock 测试 |
| **Singleton** | `KSingleton<T>` / `PersistentSingleton<T>` | Manager 生命周期管理（自动注册到 ServiceLocator） |
| **Observer** | `KSignal` / `Subscriber` / `EventBus` | 点对点信号 + 全局事件解耦通信 |
| **Command** | `Command` / `CommandQueue` | 命令模式，支持优先级和队列执行 |
| **State Machine** | `StateMachine` / `HybridStateMachine` | 层级状态机，条件转换 |
| **Object Pool** | `GameObjectPool` / `PoolService` / `CSharpPool<T>` | GameObject 对象池 + 纯 C# 对象池 |
| **Module** | `TModule<T>` | 运行时可热插拔功能模块 |

## 关键约定

- **服务访问**：推荐 `ServiceLocator.Get<ISoundService>()`（接口隔离，可 Mock）；兼容 `SoundService.Instance`（经典单例）。安全访问用 `ServiceLocator.TryGet<T>(out var svc)`
- **信号系统**：用 `KSignal`/`KSignal<T>`（点对点）和 `EventBus`（全局广播），禁止 C# 原生事件或 `UnityEvent`
- **UI 管理**：所有栈操作必须走 `UIService`，`UIPanel` 不暴露公开 Open/Close 方法。返回 `UniTask`，fire-and-forget 必须显式 `.Forget()`
- **资源加载**：`AssetService` 封装 Addressables（Runtime）+ AssetDatabase（Editor），禁止 `Resources.Load()`
- **对象池**：`PoolService` 统一管理，实现 `IPoolable` 接口接收回调。纯 C# 池用 `CSharpPool<T>`
- **协程**：`KCoroutine` 不依赖 MonoBehaviour，可在纯 C# 上下文使用
- **命名空间**：框架自有代码统一为扁平 `KFramework`；Editor 为 `KFramework.Editor`；Tests 为 `KFramework.Tests`；内嵌三方保持各自 namespace

## 架构评价

### 优点

1. **分层清晰** — Core → Unity → Modules → Extensions 四层职责分明，依赖单向
2. **Service Locator + 接口隔离** — 支持 Mock 测试和运行时服务替换
3. **信号系统完善** — KSignal（点对点）+ EventBus（全局广播）双轨通信
4. **模块可热插拔** — TModule 运行时动态装卸
5. **协程独立** — KCoroutine 不依赖 MonoBehaviour
6. **场景管理完整** — 支持 Addressables/Built-in、叠加场景、历史回退、过渡效果
7. **音频系统完整** — 对象池、并发限制/冷却/音量衰减、Mixer 控制、BGM Ducking、3D 音效
