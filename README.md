# KFramework — Unity 游戏框架

> 面向 Unity 2D/3D 游戏的轻量级模块化游戏框架  
> **框架版本**: 1.6.0 | **Unity 兼容**: 2021.3+  
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
https://github.com/isakwong/KFramework.git#v1.6.0
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
├── Runtime/                  # 运行时代码
│   ├── KFramework.asmdef
│   ├── Foundation/           # 基础层 (Singleton, ServiceLocator, Log)
│   ├── Core/                 # 核心层 (KGameCore, GameMode, TModule)
│   ├── Sound/                # 音频系统 (SoundManager, SoundCategory)
│   ├── UI/                   # UI 管理
│   ├── ObjectPool/           # 对象池
│   ├── EventBus/             # 事件总线
│   ├── Fsm/                  # 状态机
│   ├── BehaviorTree/         # 行为树
│   └── ...                   # 更多模块
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
│                      Application Layer                              │
│         GameMode 子类 / Controller 子类 / 业务逻辑                     │
├─────────────────────────────────────────────────────────────────────┤
│                    FrameworkExt Layer (游戏扩展层)                     │
│   UnitBase · UnitModule · PlayerModule · VfxManager · HUD · Camera  │
├─────────────────────────────────────────────────────────────────────┤
│                     Module Layer (功能模块层)                          │
│   UIManager · SoundManager · AssetManager · ConfigManager           │
│   PersistentDataManager · SettingsManager · DebugManager            │
│   SceneManager · EventBus · KVersion                                │
├─────────────────────────────────────────────────────────────────────┤
│                      Core Layer (核心层)                              │
│   KGameCore · GameCoreProxy · GameMode · TModule<T>                 │
│   Signal/Subscriber · KTimer · Command · KCoroutine · FSM · BT     │
├─────────────────────────────────────────────────────────────────────┤
│                    Foundation Layer (基础层)                           │
│   KSingleton · PersistentSingleton · ServiceLocator                 │
│   EnhancedLog · Variant · MathExtension · AutoBind                  │
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
| **Object Pool** | `GameObjectPool` / `PoolManager` / `CSharpPool<T>` | GameObject 对象池 + 纯 C# 对象池，统一管理实例复用 |
| **Module** | `TModule<T>` | 运行时可热插拔功能模块 |

### 1.3 服务访问方式

```csharp
// ① 经典单例访问（向后兼容）
AssetManager.Instance.LoadAsset<T>(path);

// ② Service Locator + 接口访问（推荐）
var assets = ServiceLocator.Get<IAssetService>();
assets.LoadAsset<T>(path);

// ③ 安全访问（不抛异常）
if (ServiceLocator.TryGet<ISoundService>(out var sound))
    sound.PlayMusic(clip);
```

---

## 二、模块清单

### 2.1 Foundation Layer — 基础层

| 文件 | 类/接口 | 说明 |
|---|---|---|
| `Base.cs` | `KSingleton<T>` | 纯 C# 单例基类，自动注册 ServiceLocator |
| `PersistentSingleton.cs` | `PersistentSingleton<T>` | MonoBehaviour 单例，DontDestroyOnLoad |
| `InstanceBehaviour.cs` | `InstanceBehaviour<T>` | 非单例 MonoBehaviour 基类 |
| `ServiceLocator.cs` | `ServiceLocator` | 静态服务注册中心 (Register/Get/TryGet/Unregister/Reset) |
| `EnhancedLog.cs` | `EnhancedLog` / `ILogService` | 分级日志系统 (6级: Verbose/Debug/Info/Warning/Error/Fatal)，模块 Tag 过滤，文件轮转，平台适配 |
| `Timer.cs` | `KTimer` / `KTimerManager` | 定时器系统，支持循环/暂停/停止 |
| `Variant.cs` | `Variant` | 通用变体类型 |
| `MathExtension.cs` | — | 数学扩展方法 |
| `GameObjectExtensions.cs` | — | GameObject 扩展方法 |
| `SerializeType.cs` | `SerializeType` | 类型序列化辅助 |
| `KConstraint.cs` | — | 约束系统 |
| `Selection2D.cs` | — | 2D 选择辅助 |
| `Utility.cs` | — | 通用工具方法 |

### 2.2 Core Layer — 核心层

| 文件/目录 | 类 | 说明 |
|---|---|---|
| `GameCore.cs` | `KGameCore` | 全局核心单例：Module 容器 + GameMode 管理 + Timer + JSON 初始化 |
| `GameCoreProxy.cs` | `GameCoreProxy` | MonoBehaviour 代理：驱动 FixedUpdate + 场景加载 |
| `GameMode.cs` | `GameMode` | 场景生命周期 (Awake→Init→Start→End)，持久化数据 |
| `Module.cs` | `TModule<T>` / `IModule` | 可热插拔模块基类，自带协程管理 |
| `IGameModeEventListener.cs` | `IGameModeEventListener` | GameMode 事件监听接口 |
| `GameCoreConfig.cs` | `GameCoreConfig` | 核心配置 ScriptableObject |
| `Subscriber/` | `KSignal<T>` / `Subscriber` | 泛型信号 (0-4参数) + 订阅管理器 (IDisposable) |
| `Coroutine/` | `KCoroutine` / `CoroutineManager` | 不依赖 MonoBehaviour 的协程系统 (暂停/恢复/多时机) |
| `Fsm/` | `StateMachine` / `HybridStateMachine` | 有限状态机：层级状态、条件转换 |
| `BehaviorTree/` | Fluid BehaviorTree | 行为树：Decorators / TaskParents / Tasks |
| `Cmd/` | `Command` / `CommandQueue` | 命令模式：优先级 + 三种结果状态 |
| `Action/` | `KActionBase` / `KActionSequence` | 动作序列：顺序执行 + 时间轴插入 |

### 2.3 Module Layer — 功能模块层

| 模块 | 管理器 | 接口 | 单例类型 | 核心功能 |
|---|---|---|---|---|
| **Assets** | `AssetManager` | `IAssetService` | KSingleton | Editor: AssetDatabase; Runtime: Addressables; 异步加载/实例化/释放 |
| **Config** | `ConfigManager` | `IConfigService` | KSingleton | ScriptableObject 配置加载 + 缓存 |
| **UI** | `UIManager` | `IUIService` | PersistentSingleton | UI 栈管理、全屏/普通分层、Push/Pop/Close/Destroy、Canvas 管理 |
| **Sound** | `SoundManager` | `ISoundService` | PersistentSingleton | 音效/音乐管理、对象池、并发限制/冷却/音量衰减、AudioMixer 控制、BGM Ducking、Snapshot 过渡、交叉淡入淡出、3D 音效 |
| **Settings** | `SettingsManager` | `ISettingsService` | KSingleton | 游戏设置 (画质/分辨率/全屏)，JSON 持久化 |
| **PersistentData** | `PersistentDataManager` | `IPersistentDataService` | KSingleton | JSON 存档系统、Base64 加密、场景数据 |
| **Scene** | `SceneManager` | `ISceneService` | PersistentSingleton | 场景加载/卸载/叠加、Addressables/Built-in、历史记录/GoBack、过渡效果 |
| **EventBus** | `EventBus` | `IEventBusService` | KSingleton | 全局类型路由事件、优先级/粘性/一次性订阅、Subscriber 集成 |
| **Debug** | `DebugManager` | `IDebugService` | PersistentSingleton | Gizmos 绘制管理、调试 GUI |
| **Version** | `KVersion` (static) | — | 静态类 | 框架版本 + 游戏版本 + 构建信息 |

### 2.4 FrameworkExt Layer — 游戏扩展层

| 模块 | 类 | 说明 |
|---|---|---|
| **Unit** | `UnitBase` | 游戏实体基类：完整生命周期 (Spawn→Alive→Die→Dead→Delete)、组件系统、Socket 挂点 |
| **Unit** | `UnitModule` | Unit 全局管理器：延迟生命周期队列、时间缩放 |
| **Unit** | `UnitComponent` | Unit 组件基类 |
| **Player** | `ControllerBase` | 玩家输入控制器，内置 CommandQueue |
| **Player** | `PlayerModule` | 玩家控制器注册管理 |
| **Vfx** | `VfxManager` / `IVfxService` | 特效对象池管理 |
| **HUD** | `HUDBase` | HUD 基类 |
| **Camera** | `CameraInstance` | 相机单例管理 |

### 2.5 工具 & 辅助

| 目录 | 说明 |
|---|---|
| `ObjectPool/` | `GameObjectPool` (单 Prefab 池 + IPoolable 回调) · `PoolManager` (多 Prefab 注册中心 + 实例跟踪) · `CSharpPool<T>` (纯 C# 泛型池) · `ListPool/DictionaryPool/HashSetPool` 集合池 |
| `Attributes/` | `[AutoBind]` 自动绑定组件 + `[DisplayName]` 编辑器显示名 |
| `JsonConverter/` | Newtonsoft.Json 的 Unity 类型转换器 (Vector/Quaternion/Addressables 等) |
| `Audio/` | `AudioConfig` 音频配置 ScriptableObject (Mixer 参数名、SoundCategory 预设) |
| `SerializedCollections/` | 可序列化字典 |
| `Utils/` | `AutoRotate` / `RandomAudioClip` 工具组件 |
| `Tests/` | 简易单元测试 (Signal + PersistentData) |
| `Editor/` | 自定义 Inspector、PropertyDrawer、编辑器工具 |

---

## 三、架构优点

1. **分层清晰** — Foundation → Core → Module → FrameworkExt 四层职责分明
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
| `IAssetService` | `AssetManager` | `LoadAsset<T>(path)` · `LoadAssetAsync<T>(path)` · `LoadAsset<T>(AssetReference)` · `Instantiate(ref)` · `Release(handle)` |
| `IConfigService` | `ConfigManager` | `GetConfig<T>(name)` |
| `IUIService` | `UIManager` | `Push<T>()` · `Pop()` · `Close<T>()` · `Get<T>()` · `DestroyUI<T>()` · `VisibleStack` · `OverlayCanvas` |
| `ISoundService` | `SoundManager` | `PlaySound(clip)` · `PlaySound3D(clip,pos)` · `PlayMusic(clip)` · `PopTrack()` · `SetMixerVolume(param,vol)` · `GetMixerVolume(param)` · `TransitionToSnapshot(snap)` · `DuckBGM(duration)` · `UnduckBGM()` · `MusicVolume` · `CanPlaySound(data)` · `GetEffectiveVolume(data,vol)` |
| `ISettingsService` | `SettingsManager` | `CurrentSettings` · `SaveSettings()` · `SetQuality(level)` · `SetResolution(w,h)` |
| `IPersistentDataService` | `PersistentDataManager` | `SaveData(key,data)` · `LoadData<T>(key)` · `DeleteData(key)` · `UpdateData(key,action)` · `GetScenePersistentData(path)` |
| `ISceneService` | `SceneManager` | `LoadScene(name)` · `LoadSceneAsync(name)` · `LoadAdditiveAsync(name)` · `UnloadAdditiveAsync(name)` · `GoBack()` · `OnSceneLoadBegin/Progress/Complete` |
| `IEventBusService` | `EventBus` | `Subscribe<T>(handler)` · `Publish<T>(event)` · `PublishSticky<T>(event)` · `QuerySticky<T>()` · `Unsubscribe<T>(handler)` |
| `IDebugService` | `DebugManager` | `DrawGizmos(action)` · `DrawRectangle(pos,size,color)` · `DrawSphere(pos,radius,color)` |
| `ILogService` | `EnhancedLog` | `Verbose/Debug/Info/Warning/Error/Fatal(tag,msg)` · `SetGlobalLevel(level)` · `SetTagLevel(tag,level)` |
| `IVfxService` | `VfxManager` | `Get(prefab,pos,rot)` · `Release(vfx)` · `Preload(prefab,count)` |
| `IPoolService` | `PoolManager` | `Get(prefab,pos,rot)` · `Get<T>(prefab,pos,rot)` · `Release(instance)` · `Preload(prefab,count)` · `IsPooled(instance)` · `ClearAll()` |

---

## 五、目录结构

```
KFramework/                      # UPM 包根目录
├── package.json                 # UPM 包清单 (com.isakwong.kframework)
├── LICENSE                      # MIT 许可证
├── CHANGELOG.md                 # 版本变更日志
├── README.md
├── Runtime/                     # 运行时代码 (KFramework.asmdef)
│   ├── Foundation/              #   基础层：单例、ServiceLocator、日志、定时器
│   ├── Core/                    #   核心层：KGameCore、GameMode、TModule
│   ├── Subscriber/              #   信号系统：KSignal、Subscriber
│   ├── EventBus/                #   事件总线：全局类型路由
│   ├── Coroutine/               #   自定义协程系统
│   ├── Fsm/                     #   有限状态机
│   ├── BehaviorTree/            #   行为树 (Fluid BT)
│   ├── Cmd/                     #   命令队列
│   ├── Action/                  #   动作序列
│   ├── Assets/                  #   资源管理 (Addressables + AssetDatabase)
│   ├── Config/                  #   配置管理 (ScriptableObject)
│   ├── UI/                      #   UI 栈管理
│   ├── Sound/                   #   音效/音乐 (SoundManager/SoundCategory/SoundEmitter)
│   ├── Settings/                #   游戏设置
│   ├── PersistentData/          #   持久化存档
│   ├── Scene/                   #   场景管理
│   ├── Debug/                   #   调试工具
│   ├── Version/                 #   版本信息
│   ├── ObjectPool/              #   对象池 (GameObjectPool/PoolManager/CSharpPool)
│   ├── FrameworkExt/            #   游戏扩展层 (Unit/Player/Vfx/HUD/Camera)
│   ├── Attributes/              #   特性 (AutoBind/DisplayName)
│   ├── JsonConverter/           #   JSON 转换器
│   ├── Audio/                   #   音频配置
│   ├── SerializedCollections/   #   可序列化字典
│   └── Utils/                   #   工具组件
├── Editor/                      # 编辑器扩展 (KFramework.Editor.asmdef)
├── Tests/                       # 测试 (KFramework.Tests.asmdef)
│   └── Runtime/
├── Documentation~/              # 设计文档 (Unity 忽略)
└── Samples~/                    # 示例 (Unity 忽略)
```

---

## 六、变更记录

详见 [CHANGELOG.md](CHANGELOG.md)。

| 版本 | 摘要 |
|---|---|
| **1.6.0** | 音频系统全面重构 (SoundCategory/SoundData 双层架构 + Mixer API + BGM Ducking)；SoundEmitter 统一 PoolManager；UPM 包结构 |
| **1.5.0** | 通用对象池 (GameObjectPool + PoolManager + CSharpPool + 集合池)；UnitBase opt-in 回收 |
| **1.4.0** | 结构化日志系统 (6 级 + Tag 过滤 + 文件轮转) |
| **1.3.0** | Service Locator + 12 个服务接口 |
| **1.2.0** | EventBus 事件总线、SceneManager 场景管理、KVersion 版本模块 |
| **1.1.0** | Bug 修复、API 规范化 |
| **1.0.0** | 初始发布 |
