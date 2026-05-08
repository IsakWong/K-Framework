# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

KFramework 是一个 Unity UPM 包（`com.isakwong.kframework`），面向 2D/3D 游戏的轻量级模块化框架。Unity 2021.3+，MIT 许可证。

## 架构分层

```
Foundation → Core → Module → FrameworkExt（游戏扩展层）
```

- **Foundation**：`KSingleton<T>`、`PersistentSingleton<T>`、`ServiceLocator`、`EnhancedLog`、`KTimer`
- **Core**：`KGameCore`（全局单例，Module 容器）、`GameMode`（场景生命周期）、`TModule<T>`（可热插拔模块）、`KSignal`/`Subscriber`、`KCoroutine`、FSM、BehaviorTree、`Command`、`KActionBase`
- **Module**：`UIManager`、`SoundManager`、`AssetManager`、`ConfigManager`、`SceneManager`、`EventBus`、`PersistentDataManager`、`SettingsManager`、`DebugManager`
- **FrameworkExt**：`UnitBase`/`UnitModule`、`ControllerBase`/`PlayerModule`、`VfxManager`、`HUDBase`、`CameraInstance`

## 关键约定

- **服务访问**：推荐 `ServiceLocator.Get<ISoundService>()`（接口隔离，可 Mock）；兼容 `SoundManager.Instance`（经典单例）。安全访问用 `ServiceLocator.TryGet<T>(out var svc)`
- **信号系统**：用 `KSignal`/`KSignal<T>`（点对点）和 `EventBus`（全局广播），禁止 C# 原生事件或 `UnityEvent`
- **UI 管理**：所有栈操作必须走 `UIManager`（`PushAsync<T>()`、`CloseAsync()`、`BringToFrontAsync<T>()`），`UIPanel` 不暴露公开 Open/Close 方法。返回 `UniTask`，fire-and-forget 必须显式 `.Forget()`
- **资源加载**：`AssetManager` 封装 Addressables（Runtime）+ AssetDatabase（Editor），禁止 `Resources.Load()`
- **对象池**：`PoolManager` 统一管理，实现 `IPoolable` 接口接收回调。纯 C# 池用 `CSharpPool<T>`
- **协程**：`KCoroutine` 不依赖 MonoBehaviour，可在纯 C# 上下文使用
- **命名空间**：代码在 `Framework.*` 下，部分遗留类在全局命名空间，新增代码统一用 `KFramework.*`

## 核心系统速查

### KGameCore + GameMode + TModule

```csharp
KGameCore.RequireSystem<T>()  // 获取或自动创建模块
KGameCore.SystemAt<T>()       // 获取，不存在返回 null

// TModule<T>：Awake() 自动注册到 KGameCore
// GameMode：Awake→Init→Start→End 场景生命周期
```

### Unit 生命周期

```
None → Spawning → Alive → Dying → Dead → Deleting → Deleted
```

- `unit.Spawn()` / `unit.Die()` / `unit.Delete()` 请求，`UnitModule` 批量处理
- 覆写 `OnSpawn()`、`OnDie()` 添加逻辑；`OnLogic()` 每 FixedUpdate 调用
- `UnitComponent` 挂载在 `__Components__` 子 Transform，`unit.GetUnitComponent<T>()` 获取
- 操作前检查 `unit.IsAlive`，禁止直接 `Destroy()`

### UI 系统

- `Fullscreen`：栈式独占，切栈按 `KeepAliveOnSuspend` 选 Suspend（保留状态/订阅/BGM）或 Close
- `Overlay`：与 Fullscreen 共存，多层并列
- 信号：`OnPanelOpen`/`OnPanelClose`/`OnPanelBeginOpen` 等
- 动画：`UIAnimation` 抽象基类（内置 `UIAnimationFade` DOTween 淡入淡出、`UIAnimationMMF`）

### Sound 系统

- `SoundCategory`（ScriptableObject）：Mixer 路由、并发上限、冷却、衰减、随机音高
- `SoundData`：AudioSource 参数模板
- API：`PlaySound(clip, category)`、`PlayMusic(clip)`、`SetMixerVolume(param, vol)`、`TransitionToSnapshot(snap)`、`DuckBGM(duration)`

### Action 系统（Flow + KTrigger）

- **Flow**：链式编排（顺序/分支/循环/并行/等信号），`Flow.Create().Do(...).Wait(0.5f).Build().Run(this)`
- **KTrigger**：事件-条件-动作模式，`KTrigger.Once().On<T>().When(pred).Do(action).BuildAndRegister()`
- 并行分支共享 `FlowContext`，写同一 key 是竞态
- Trigger 执行期间忽略新事件（不排队）
- `Flow.Run(this)` 的 `this` 必须是激活的 MonoBehaviour

## 依赖

| 依赖 | 用途 |
|------|------|
| UniTask 2.5.10+ | 异步（UI、Asset 加载） |
| DOTween | 动画（UI 淡入淡出、通用补间） |
| Unity InputSystem 1.4.0 | 输入 |
| Unity Addressables 1.19.0 | 资源加载 |
| Newtonsoft.Json | 序列化（存档、配置） |
| Odin Inspector | 编辑器增强（可选） |
| MoreMountains Feel | 反馈系统（可选） |

## 项目结构

```
Runtime/          KFramework.asmdef（运行时代码）
  Foundation/     基础层
  Core/           核心层（GameCore, GameMode, TModule, Signal, FSM, BT, Command, Action）
  Subscriber/     KSignal / Subscriber
  EventBus/       全局事件总线
  Coroutine/      自定义协程
  UI/             UI 栈管理
  Sound/          音频系统
  Assets/         Addressables 封装
  ObjectPool/     GameObjectPool / PoolManager / CSharpPool
  FrameworkExt/   Unit / Player / Vfx / HUD / Camera
  ...
Editor/          编辑器扩展
Tests/           测试
```

详见 `README.md` 获取完整模块清单、服务接口速查表、架构评价。
