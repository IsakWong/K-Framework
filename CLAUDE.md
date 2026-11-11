# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

KFramework 是一个 Unity UPM 包（`com.isakwong.kframework`），面向 2D/3D 游戏的轻量级模块化框架。Unity 2021.3+，MIT 许可证。

## 架构分层

```
Core（引擎无关内核） → Unity（引擎适配） → Modules（功能模块） → Extensions（可选扩展）
```

- **Core**（`Runtime/Core/`）：纯 C# 内核，不含 `MonoBehaviour` / `ScriptableObject`。`IService`、`ServiceLocator`、`KSingleton<T>`、`KSignal`/`Subscriber`/`EventBus`、`KCoroutine`、`Command`、`CSharpPool<T>`、`KStateMachine`
- **Unity**（`Runtime/Unity/`）：引擎适配层。`KGameCore`（模块容器）、`TModule<T>`（可热插拔模块）、`PersistentSingleton<T>`、`KTime`、`EnhancedLog`、`AutoBind`、`Variant`、`SceneController`
- **Modules**（`Runtime/Modules/`）：通用功能模块。`UIService`、`SoundService`、`AssetService`、`ConfigService`、`SceneService`、`PersistentDataService`、`SettingsService`、`DebugService`、`PoolService`、`VfxService`、`CameraInstance`
- **Extensions**（`Runtime/Extensions/`）：可选游戏扩展。`ControllerBase`/`PlayerModule`、`HUDBase`

依赖方向严格单向：`Extensions → Modules → Unity → Core`。`Runtime/ThirdParty/` 为内嵌三方代码，保持各自 namespace 与 asmdef。

## 关键约定

- **服务访问**：推荐 `ServiceLocator.Get<ISoundService>()`（接口隔离，可 Mock）；兼容 `SoundService.Instance`（经典单例）。安全访问用 `ServiceLocator.TryGet<T>(out var svc)`
- **信号系统**：用 `KSignal`/`KSignal<T>`（点对点）和 `EventBus`（全局广播），禁止 C# 原生事件或 `UnityEvent`
- **UI 管理**：所有层操作必须走 `UIService`（`PushAsync<T>()`、`CloseAsync()`、`BringToFrontAsync<T>()`），`UIPanel` 不暴露公开 Open/Close 方法。返回 `UniTask`，fire-and-forget 必须显式 `.Forget()`
- **资源加载**：`AssetService` 封装 Addressables（Runtime）+ AssetDatabase（Editor），禁止 `Resources.Load()`
- **对象池**：`PoolService` 统一管理，实现 `IPoolable` 接口接收回调。纯 C# 池用 `CSharpPool<T>`
- **协程**：`KCoroutine` 不依赖 MonoBehaviour，可在纯 C# 上下文使用
- **命名空间**：框架自有代码统一为扁平 `KFramework`；Editor 程序集为 `KFramework.Editor`；Tests 为 `KFramework.Tests`；`Runtime/ThirdParty/` 保持上游 namespace（`FSM`、`CleverCrow.Fluid.BTs`、`AYellowpaper.SerializedCollections`、`DG.Tweening` 等）

## 核心系统速查

### KGameCore + SceneController + TModule

```csharp
KGameCore.RequireSystem<T>()  // 获取或自动创建模块
KGameCore.SystemAt<T>()       // 获取，不存在返回 null

// TModule<T>：首次 RequireModule 时创建并注册到 KGameCore
// SceneController：场景生命周期宿主
//   Awake → OnSceneAwake → InitializeScene → OnSceneInitialize → OnSceneStart
//   OnSceneRestart / OnSceneEnd（OnDestroy 时调用）
```

### UI 系统

- `Hud` / `Window` / `Popup` / `Service`：四层 Canvas，层间 sortingOrder 固定；Window 是栈，压栈即 Suspend（保留状态/订阅/BGM）
- `Popup`：叠在 Window 之上；`Service`：永远最上，`CloseAll` 不清
- 信号：`OnPanelOpen`/`OnPanelClose`/`OnPanelBeginOpen` 等
- 动画：`UIAnimation` 抽象基类（内置 `UIAnimationFade` DOTween 淡入淡出、`UIAnimationMMF`）

### Sound 系统

- `SoundCategory`（ScriptableObject）：Mixer 路由、并发上限、冷却、衰减、随机音高
- `SoundData`：AudioSource 参数模板
- API：`PlaySound(clip, category)`、`PlayMusic(clip)`、`SetMixerVolume(param, vol)`、`TransitionToSnapshot(snap)`、`DuckBGM(duration)`

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
Runtime/          KFramework.asmdef（单一运行时程序集）
  Core/                       引擎无关内核（纯 C#，不引用 UnityEngine）
    Services/                 IService · ServiceLocator · KSingleton<T> · ILogService · LogLevel
    Events/                   IEvent · EventBus · Signal · Subscriber
    Commands/                 Command · CommandQueue
    Async/                    KCoroutine · CoroutineHandler · CoroutineManager · Wait
    Pooling/                  CSharpPool<T>
    FSM/                      KStateMachine
  Unity/                      引擎适配层（MonoBehaviour / ScriptableObject 宿主）
    Hosting/                  KGameCore · GameCoreConfig · GameCoreProxy · IModule · TModule<T> · ModuleLocator · SceneController
    Foundation/               PersistentSingleton · InstanceBehaviour · KConstraint · Selection2D
    Time/                     KTime · KTimer · KTimerManager
    Logging/                  EnhancedLog
    Attributes/               AutoBindAttribute · AutoBinder · DisplayNameAttribute
    Serialization/            Variant · SerializeType
    Extensions/               GameObjectExtensions · MathExtension · DoTweenExtension
    Utilities/                Utility · DoubleArray<T>
    Components/               AutoRotate · RandomAudioClip
    Version/                  KVersion · VersionConfig
  Modules/                    通用功能模块（接口 / 实现 / 组件同目录）
    Assets/ Camera/ Config/ Debug/ ObjectPool/ PersistentData/ Scene/ Settings/ Sound/ UI/ Vfx/
  Extensions/                 可选游戏扩展
    HUD/                      HUDBase
    Player/                   ControllerBase · PlayerModule
  ThirdParty/                 内嵌三方（namespace / asmdef 不变）
    Fsm/                      UnityHFSM（namespace FSM）
    BehaviorTree/             CleverCrow Fluid BT（namespace CleverCrow.Fluid.BTs）
    JsonConverter/            Newtonsoft.Json.UnityConverters（+ 自有 AssetReferenceConverter）
    SerializedCollections/    AYellowpaper（Runtime + Editor 两 asmdef）
    Plugins/                  DOTween Modules（DG.Tweening）
Editor/          编辑器扩展（KFramework.Editor asmdef）
  Drawers/       DisplayNameDrawer · SerializeTypeDrawer · VariantPropertyDrawer · KGizmosDrawer
  Inspectors/    GameCoreProxyEditor · InfiniteScrollEditor · VersionConfigEditor
  Utilities/     KEditorGUILayout · KGizmos · KEditorSubscriber
  ToolsWindow/   工具窗口（IKToolPage / KToolPageRegistry / Pages）
  ThirdParty/    DOTween 预编译编辑器插件
Tests/           测试（KFramework.Tests）
  Runtime/
Samples~/        示例（Unity 忽略，不参与构建）：CoroutineExample · CoroutineTest · DirectionLightTweener
Plugins/         UPM 包根预编译插件（DOTween.dll）
```

> 单 asmdef 下 `Core / Unity / Modules / Extensions` 是**目录与命名约定上的分层**，不是编译器强制边界；跨层依赖违规不会在编译期被拦截。新增代码按目录归位，`MonoBehaviour` / `ScriptableObject` 只允许出现在 `Unity/`、`Modules/`、`Extensions/`。

详见 `README.md` 获取完整模块清单、服务接口速查表、架构评价。
