# KFramework — Unity 游戏框架

> 面向 Unity 2D/3D 游戏的轻量级模块化游戏框架  
> **框架版本**: 2.0.0 | **Unity 兼容**: 2021.3+  
> **许可证**: MIT | **包名**: `com.isakwong.kframework`

---

## 安装

### 方式一：Unity Package Manager (推荐)

1. 打开 Unity → **Window** → **Package Manager**
2. 点击左上角 **+** → **Add package from git URL...**
3. 输入：
```
https://github.com/isakwong/KFramework.git
```

或在 `Packages/manifest.json` 中添加：
```json
{
  "dependencies": {
    "com.isakwong.kframework": "https://github.com/isakwong/KFramework.git"
  }
}
```

指定版本 tag：
```
https://github.com/isakwong/KFramework.git#v2.0.0
```

### 方式二：手动安装

将仓库克隆到 Unity 项目的 `Packages/` 目录下：
```bash
cd YourProject/Packages
git clone https://github.com/isakwong/KFramework.git com.isakwong.kframework
```

### 额外依赖

以下依赖需要单独安装（非 UPM 包）：

| 依赖 | 说明 | 安装方式 |
|------|------|----------|
| DOTween | 动画缓动 | Asset Store |
| Newtonsoft.Json | JSON 序列化 | UPM: `com.unity.nuget.newtonsoft-json` |
| Odin Inspector | Inspector 增强 | Asset Store (可选) |
| MoreMountains | 反馈系统 | Asset Store (可选) |

### 项目结构

```
KFramework/
├── package.json              # UPM 包清单
├── LICENSE                   # MIT 许可证
├── CHANGELOG.md              # 版本变更日志
├── README.md
├── Runtime/                  # 运行时代码 (KFramework.asmdef)
│   ├── Core/                 #   引擎无关内核（纯 C#）
│   ├── Unity/                #   引擎适配层（MonoBehaviour / ScriptableObject 宿主）
│   ├── Modules/              #   通用功能模块
│   ├── Extensions/           #   可选游戏扩展（HUD / Player）
│   └── ThirdParty/           #   内嵌三方（各自 namespace / asmdef）
├── Editor/                   # 编辑器扩展
│   └── KFramework.Editor.asmdef
├── Tests/                    # 测试
│   └── Runtime/
│       └── KFramework.Tests.asmdef
├── Documentation~/           # 设计文档 (Unity 忽略)
└── Samples~/                 # 示例 (Unity 忽略)
```

---

## 一、总体设计

### 1.1 架构概览

KFramework 采用 **4 层分层架构**，自下而上职责递增、依赖递减：

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Extensions Layer (可选扩展)                        │
│   ControllerBase · PlayerModule · HUDBase                          │
├─────────────────────────────────────────────────────────────────────┤
│                     Modules Layer (功能模块)                          │
│   UIService · SoundService · AssetService · ConfigService           │
│   SceneService · PoolService · VfxService · DebugService            │
│   PersistentDataService · SettingsService · CameraInstance          │
├─────────────────────────────────────────────────────────────────────┤
│                      Unity Layer (引擎适配)                           │
│   KGameCore · GameCoreProxy · SceneController · TModule<T>          │
│   PersistentSingleton · KTime · EnhancedLog · Variant · AutoBind    │
├─────────────────────────────────────────────────────────────────────┤
│                       Core Layer (引擎无关)                           │
│   KSingleton<T> · ServiceLocator · IService · ILogService           │
│   KSignal/Subscriber · EventBus · Command · KCoroutine              │
│   CSharpPool<T> · KStateMachine                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 1.2 核心设计模式

| 模式 | 实现 | 用途 |
|---|---|---|
| **Service Locator** | `ServiceLocator` 静态类 | 统一服务注册与发现，接口隔离，支持 Mock 测试 |
| **Singleton** | `KSingleton<T>` / `PersistentSingleton<T>` | Manager 生命周期管理 (自动注册到 ServiceLocator) |
| **Observer** | `KSignal` / `Subscriber` / `EventBus` | 点对点信号 + 全局事件解耦通信 |
| **Command** | `Command` / `CommandQueue` | 命令模式，支持优先级和队列执行 |
| **State Machine** | `StateMachine` / `HybridStateMachine` | 层级状态机，条件转换 |
| **Object Pool** | `GameObjectPool` / `PoolService` / `CSharpPool<T>` | GameObject 对象池 + 纯 C# 对象池，统一管理实例复用 |
| **Module** | `TModule<T>` | 运行时可热插拔功能模块 |

### 1.3 服务访问方式

```csharp
// ① 经典单例访问（向后兼容）
AssetService.Instance.LoadAsset<T>(path);

// ② Service Locator + 接口访问（推荐）
var assets = ServiceLocator.Get<IAssetService>();
assets.LoadAsset<T>(path);

// ③ 安全访问（不抛异常）
if (ServiceLocator.TryGet<ISoundService>(out var sound))
    sound.PlayMusic(clip);
```

---

## 二、模块清单

### 2.1 Core Layer — 引擎无关内核

| 目录 | 类/接口 | 说明 |
|---|---|---|
| `Services/` | `IService` / `ServiceLocator` / `KSingleton<T>` / `ILogService` / `LogLevel` | 服务契约、静态服务注册中心、纯 C# 单例基类、日志接口 |
| `Events/` | `KSignal` / `Subscriber` / `EventBus` / `IEvent` | 点对点信号（0–4 参数）+ 全局事件总线 + 订阅管理器 |
| `Commands/` | `Command` / `CommandQueue` | 命令模式：优先级 + 三种结果状态 |
| `Async/` | `KCoroutine` / `CoroutineHandler` / `CoroutineManager` / `Wait` | 不依赖 MonoBehaviour 的协程系统（暂停/恢复/多时机） |
| `Pooling/` | `CSharpPool<T>` | 纯 C# 泛型对象池 |
| `FSM/` | `KStateMachine` | 轻量状态机 |

### 2.2 Unity Layer — 引擎适配

| 目录 | 类/接口 | 说明 |
|---|---|---|
| `Hosting/` | `KGameCore` / `TModule<T>` / `SceneController` / `GameCoreProxy` / `ModuleLocator` / `IPersistentRoot` | 模块容器、可热插拔模块、场景生命周期、驱动代理 |
| `Foundation/` | `PersistentSingleton<T>` / `InstanceBehaviour<T>` / `KConstraint` / `Selection2D` | MonoBehaviour 单例与组件基类 |
| `Time/` | `KTime` / `KTimer` / `KTimerManager` | 时间缩放与定时器 |
| `Logging/` | `EnhancedLog` | 分级日志（6 级）、模块 Tag 过滤、文件轮转 |
| `Attributes/` | `[AutoBind]` / `[DisplayName]` | 自动绑定与编辑器显示名 |
| `Serialization/` | `Variant` / `SerializeType<T>` | 通用变体类型与序列化辅助 |
| `Extensions/` | `GameObjectExtensions` / `MathExtension` / `DoTweenExtension` | 扩展方法 |
| `Utilities/` | `Utility` / `DoubleArray<T>` | 通用工具方法 |
| `Components/` | `AutoRotate` / `RandomAudioClip` | 通用小工具组件 |
| `Version/` | `KVersion` / `VersionConfig` | 框架版本 + 游戏版本 + 构建信息 |

### 2.3 Module Layer — 功能模块

| 模块 | 管理器 | 接口 | 单例类型 | 核心功能 |
|---|---|---|---|---|
| **Assets** | `AssetService` | `IAssetService` | KSingleton | Editor: AssetDatabase; Runtime: Addressables; 异步加载/实例化/释放 |
| **Config** | `ConfigService` | `IConfigService` | KSingleton | ScriptableObject 配置加载 + 缓存 |
| **UI** | `UIService` | `IUIService` | PersistentSingleton | UI 栈管理、全屏/普通分层、Push/Pop/Close/Destroy、Canvas 管理 |
| **Sound** | `SoundService` | `ISoundService` | PersistentSingleton | 音效/音乐管理、对象池、并发限制/冷却/音量衰减、AudioMixer 控制、BGM Ducking、Snapshot 过渡、交叉淡入淡出、3D 音效 |
| **Settings** | `SettingsService` | `ISettingsService` | KSingleton | 游戏设置 (画质/分辨率/全屏)，JSON 持久化 |
| **PersistentData** | `PersistentDataService` | `IPersistentDataService` | KSingleton | JSON 存档系统、Base64 加密、场景数据 |
| **Scene** | `SceneService` | `ISceneService` | PersistentSingleton | 场景加载/卸载/叠加、Addressables/Built-in、历史记录/GoBack、过渡效果 |
| **ObjectPool** | `PoolService` | `IPoolService` | PersistentSingleton | 多 Prefab 注册中心 + 实例跟踪 + IPoolable 回调 |
| **Vfx** | `VfxService` | `IVfxService` | PersistentSingleton | 特效对象池管理与播放 |
| **Camera** | `CameraInstance` | `ICameraService` | PersistentSingleton | 相机单例管理 |
| **Debug** | `DebugService` | `IDebugService` | PersistentSingleton | Gizmos 绘制管理、调试 GUI |

### 2.4 Extensions Layer — 可选扩展

| 模块 | 类 | 说明 |
|---|---|---|
| **Player** | `ControllerBase` | 玩家输入控制器，内置 CommandQueue |
| **Player** | `PlayerModule` | 玩家控制器注册管理 |
| **HUD** | `HUDBase` | HUD 基类 |

### 2.5 工具 & 辅助

| 目录 | 说明 |
|---|---|
| `Runtime/Modules/ObjectPool/` | `GameObjectPool`（单 Prefab 池 + IPoolable 回调）· `PoolService`（多 Prefab 注册中心） |
| `Runtime/Core/Pooling/` | `CSharpPool<T>`（纯 C# 泛型池）· 集合池 |
| `Runtime/Unity/Attributes/` | `[AutoBind]` 自动绑定组件 + `[DisplayName]` 编辑器显示名 |
| `Runtime/ThirdParty/JsonConverter/` | Newtonsoft.Json 的 Unity 类型转换器（Vector/Quaternion/Addressables 等） |
| `Runtime/ThirdParty/SerializedCollections/` | 可序列化字典 |
| `Runtime/Unity/Components/` | `AutoRotate` / `RandomAudioClip` 工具组件 |
| `Tests/` | 简易单元测试（Signal / EventBus / Coroutine / Pool / FSM） |
| `Editor/` | 自定义 Inspector、PropertyDrawer、工具窗口 |

---

## 三、架构优点

1. **分层清晰** — Core → Unity → Modules → Extensions 四层职责分明，依赖单向
2. **Service Locator + 接口隔离** — 支持 Mock 测试和运行时服务替换，同时保持向后兼容
3. **信号系统完善** — KSignal（点对点）+ EventBus（全局广播）双轨通信
4. **模块可热插拔** — TModule 运行时动态装卸
5. **协程独立** — KCoroutine 不依赖 MonoBehaviour，可在纯 C# 上下文使用
6. **场景管理完整** — 支持 Addressables/Built-in、叠加场景、历史回退、过渡效果
7. **存档系统** — JSON + Base64 加密 + 场景数据
8. **音频系统完整** — 对象池、并发限制/冷却/逐实例音量衰减、AudioMixer 参数控制、Snapshot 过渡、BGM Ducking 侧链压缩、交叉淡入淡出、3D 音效、帧去重

> 缺失模块和已知设计问题见 [TODO.md](TODO.md)。

---

## 四、服务接口速查表

| 接口 | 实现类 | 主要 API |
|---|---|---|
| `IAssetService` | `AssetService` | `LoadAsset<T>(path)` · `LoadAssetAsync<T>(path)` · `LoadAsset<T>(AssetReference)` · `Instantiate(ref)` · `Release(handle)` |
| `IConfigService` | `ConfigService` | `GetConfig<T>(name)` |
| `IUIService` | `UIService` | `Push<T>()` · `Pop()` · `Close<T>()` · `Get<T>()` · `DestroyUI<T>()` · `VisibleStack` · `OverlayCanvas` |
| `ISoundService` | `SoundService` | `PlaySound(clip)` · `PlaySound3D(clip,pos)` · `PlayMusic(clip)` · `PopTrack()` · `SetMixerVolume(param,vol)` · `GetMixerVolume(param)` · `TransitionToSnapshot(snap)` · `DuckBGM(duration)` · `UnduckBGM()` · `MusicVolume` · `CanPlaySound(data)` · `GetEffectiveVolume(data,vol)` |
| `ISettingsService` | `SettingsService` | `CurrentSettings` · `SaveSettings()` · `SetQuality(level)` · `SetResolution(w,h)` |
| `IPersistentDataService` | `PersistentDataService` | `SaveData(key,data)` · `LoadData<T>(key)` · `DeleteData(key)` · `UpdateData(key,action)` · `GetScenePersistentData(path)` |
| `ISceneService` | `SceneService` | `LoadScene(name)` · `LoadSceneAsync(name)` · `LoadAdditiveAsync(name)` · `UnloadAdditiveAsync(name)` · `GoBack()` · `OnSceneLoadBegin/Progress/Complete` |
| `IEventBusService` | `EventBus` | `Subscribe<T>(handler)` · `Publish<T>(event)` · `PublishSticky<T>(event)` · `QuerySticky<T>()` · `Unsubscribe<T>(handler)` |
| `IDebugService` | `DebugService` | `DrawGizmos(action)` · `DrawRectangle(pos,size,color)` · `DrawSphere(pos,radius,color)` |
| `ILogService` | `EnhancedLog` | `Verbose/Debug/Info/Warning/Error/Fatal(tag,msg)` · `SetGlobalLevel(level)` · `SetTagLevel(tag,level)` |
| `IVfxService` | `VfxService` | `Get(prefab,pos,rot)` · `Release(vfx)` · `Preload(prefab,count)` |
| `IPoolService` | `PoolService` | `Get(prefab,pos,rot)` · `Get<T>(prefab,pos,rot)` · `Release(instance)` · `Preload(prefab,count)` · `IsPooled(instance)` · `ClearAll()` |

---

## 五、目录结构

```
KFramework/                      # UPM 包根目录
├── package.json                 # UPM 包清单 (com.isakwong.kframework)
├── LICENSE                      # MIT 许可证
├── CHANGELOG.md                 # 版本变更日志
├── README.md
├── Runtime/                     # 运行时代码 (KFramework.asmdef)
│   ├── Core/                    #   引擎无关内核（纯 C#，不引用 UnityEngine）
│   │   ├── Services/            #     IService · ServiceLocator · KSingleton<T> · ILogService · LogLevel
│   │   ├── Events/              #     IEvent · EventBus · Signal · Subscriber
│   │   ├── Commands/            #     Command · CommandQueue
│   │   ├── Async/               #     KCoroutine · CoroutineHandler · CoroutineManager · Wait
│   │   ├── Pooling/             #     CSharpPool<T>
│   │   └── FSM/                 #     KStateMachine
│   ├── Unity/                   #   引擎适配层
│   │   ├── Hosting/             #     KGameCore · GameCoreProxy · TModule<T> · SceneController
│   │   ├── Foundation/          #     PersistentSingleton · InstanceBehaviour · KConstraint · Selection2D
│   │   ├── Time/                #     KTime · KTimer · KTimerManager
│   │   ├── Logging/             #     EnhancedLog
│   │   ├── Attributes/          #     AutoBind · DisplayName
│   │   ├── Serialization/       #     Variant · SerializeType
│   │   ├── Extensions/          #     GameObjectExtensions · MathExtension · DoTweenExtension
│   │   ├── Utilities/           #     Utility · DoubleArray<T>
│   │   ├── Components/          #     AutoRotate · RandomAudioClip
│   │   └── Version/             #     KVersion · VersionConfig
│   ├── Modules/                 #   通用功能模块
│   │   └── Assets/ Camera/ Config/ Debug/ ObjectPool/ PersistentData/ Scene/ Settings/ Sound/ UI/ Vfx/
│   ├── Extensions/              #   可选游戏扩展
│   │   ├── HUD/                 #     HUDBase
│   │   └── Player/              #     ControllerBase · PlayerModule
│   └── ThirdParty/              #   内嵌三方（namespace/asmdef 不变）
│       ├── Fsm/                 #     UnityHFSM
│       ├── BehaviorTree/        #     CleverCrow Fluid BT
│       ├── JsonConverter/       #     Newtonsoft UnityConverters
│       ├── SerializedCollections/
│       └── Plugins/             #     DOTween Modules
├── Editor/                      # 编辑器扩展 (KFramework.Editor.asmdef)
│   ├── Drawers/                 #     PropertyDrawer
│   ├── Inspectors/              #     CustomEditor
│   ├── Utilities/               #     KEditorGUILayout · KGizmos · KEditorSubscriber
│   ├── ToolsWindow/             #     KFramework 工具窗口
│   └── ThirdParty/              #     DOTween 预编译编辑器插件
├── Tests/                       # 测试 (KFramework.Tests.asmdef)
│   └── Runtime/
├── Documentation~/              # 设计文档 (Unity 忽略)
└── Samples~/                    # 示例 (Unity 忽略，不参与构建)
```

---

## 六、变更记录

详见 [CHANGELOG.md](CHANGELOG.md)。

| 版本 | 摘要 |
|---|---|
| **2.0.0** | UnitBase 纯 C# 化 + UnityUnit 表现宿主（组合式）；UnitComponent 体系废除，改用 IUnitHost 宿主回调 |
| **1.7.0** | TModule.OnModuleLogic 模块帧逻辑；KTimer 进度属性；KSignal 四参数版本统一命名 |
| **1.6.0** | 音频系统全面重构 (SoundCategory/SoundData 双层架构 + Mixer API + BGM Ducking)；SoundEmitter 统一 PoolService；UPM 包结构 |
| **1.5.0** | 通用对象池 (GameObjectPool + PoolService + CSharpPool + 集合池)；UnitBase opt-in 回收 |
| **1.4.0** | 结构化日志系统 (6 级 + Tag 过滤 + 文件轮转) |
| **1.3.0** | Service Locator + 12 个服务接口 |
| **1.2.0** | EventBus 事件总线、SceneService 场景管理、KVersion 版本模块 |
| **1.1.0** | Bug 修复、API 规范化 |
| **1.0.0** | 初始发布 |
