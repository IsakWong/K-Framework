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
| `ObjectPool/` | `TObjectPool<T>` — MonoBehaviour 对象池 (基于 Unity ObjectPool) |
| `Attributes/` | `[AutoBind]` 自动绑定组件 + `[DisplayName]` 编辑器显示名 |
| `JsonConverter/` | Newtonsoft.Json 的 Unity 类型转换器 (Vector/Quaternion/Addressables 等) |
| `Audio/` | `AudioConfig` 音频配置 |
| `SerializedCollections/` | 可序列化字典 |
| `Utils/` | `AutoRotate` / `RandomAudioClip` 工具组件 |
| `Testing/` | `Tests` 简易单元测试 (Signal + PersistentData) |
| `Editor/` | 自定义 Inspector、PropertyDrawer、编辑器工具 |

---

## 三、缺失模块分析

### 🔴 高优先级 — 生产项目必需

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 1 | **InputManager (输入管理)** | `ControllerBase` 引用了 Unity InputSystem 但无统一输入管理层。缺少输入映射切换、按键重绑定、多设备支持、输入缓冲 | 封装 InputSystem 的 `PlayerInput`，提供 `IInputService` 接口，支持 Action Map 切换和运行时重绑定 |
| 2 | **NetworkManager (网络模块)** | 完全无网络代码。联机游戏需要 HTTP 请求、WebSocket、状态同步/帧同步 | 至少提供 `INetworkService` 抽象 + HTTP 客户端 (UnityWebRequest 封装)；高级可选 WebSocket/Netcode |
| 3 | **异步任务管理** | `AssetManager` 用了 async/await 但无统一的异步任务管理。缺少取消 (CancellationToken)、超时、重试、并发控制 | 提供 `AsyncTaskRunner`：支持 CancellationToken 传播、超时包装、并发限制队列 |

### 🟡 中优先级 — 正式上线前应补全

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 5 | **Localization (本地化/多语言)** | 无国际化模块，UI 文本硬编码 | `ILocalizationService` + 多语言表 (CSV/JSON/ScriptableObject)；支持运行时切换语言、文本/图片/音频本地化 |
| 6 | **热更新流程** | 有 Addressables 但无热更流程 (版本检查、资源对比、增量下载、进度回调) | 在 `AssetManager` 基础上增加 `IHotUpdateService`：CheckUpdate → DownloadDiff → Apply |
| 7 | **Camera 管理增强** | `CameraInstance` 仅持有引用，缺少跟随、震动、过渡、多相机切换 | 新增 `CameraManager`：跟随策略 (平滑/弹性/锁定)、屏幕震动、相机混合过渡 |
| 9 | **Analytics (数据埋点)** | 无事件追踪/数据分析模块 | `IAnalyticsService` 接口 + 可插拔后端 (Firebase/自建)，自动追踪场景切换、UI 停留时长 |
| 10 | **红点系统 (Badge/Notification)** | 无 UI 红点提示系统，常见功能型游戏必需 | 树状红点管理器：父节点自动聚合子节点状态，支持数字/点两种模式 |

### 🟢 低优先级 — 按需添加

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 11 | **引导/教程系统** | 无新手引导框架 | 步骤驱动引导系统：高亮遮罩、强制点击、对话气泡、条件触发 |
| 12 | **Download Manager** | 缺少大文件下载管理 (断点续传、多线程下载) | 独立于 Addressables，用于补丁包/DLC 资源下载 |
| 13 | **Shader/Material 管理** | 无全局 Shader 变体收集和材质参数管理 | ShaderManager：变体预热、全局属性设置 |
| 14 | **平台适配层** | 微信小游戏 (#if WEIXINMINIGAME) 散落在各处，无统一平台抽象 | `IPlatformService`：统一文件系统、分享、支付、广告等平台差异 |
| 15 | **Timeline/Cutscene** | 无过场动画管理 | 可选集成 Unity Timeline 的管理层 |
| 16 | **AI 模块扩展** | 行为树已有，但缺少黑板 (Blackboard)、感知系统 (Perception) | 扩展 BehaviorTree：共享黑板 + 视觉/听觉感知组件 |

---

## 四、架构评价与改进建议

### 4.1 架构优点 ✅

1. **分层清晰** — Foundation → Core → Module → FrameworkExt 四层职责分明
2. **Service Locator + 接口隔离** — 支持 Mock 测试和运行时服务替换，同时保持向后兼容
3. **信号系统完善** — KSignal (点对点) + EventBus (全局广播) 双轨通信
4. **模块可热插拔** — TModule 运行时动态装卸
5. **协程独立** — KCoroutine 不依赖 MonoBehaviour，可在纯 C# 上下文使用
6. **场景管理完整** — 支持 Addressables/Built-in、叠加场景、历史回退、过渡效果
7. **存档系统** — JSON + Base64 加密 + 场景数据
8. **音频系统完整** — 对象池、并发限制/冷却/逐实例音量衰减、AudioMixer 参数控制、Snapshot 过渡、BGM Ducking 侧链压缩、交叉淡入淡出、3D 音效、帧去重

### 4.2 已知设计问题 ⚠️

| # | 问题 | 严重度 | 现状 | 建议 |
|---|---|---|---|---|
| 1 | **缺少命名空间** | 🟡中 | 大量类在全局命名空间 | 统一 `KFramework.*` 命名空间 |
| 2 | **GameMode 耦合业务** | 🟡中 | `OnPlayerDeath/OnPlayerRespawn` 等业务回调在框架层 | 改为泛型事件或移到扩展层 |
| 3 | **UnitModule 封装不足** | 🟡中 | `_toSpawnUnits` 等内部队列为 public | 改为 internal 或只暴露方法 |
| 4 | **线程安全** | 🟢低 | 所有 Manager 无线程保护 | 加锁或标注 `[MainThread]` 限制 |
| 5 | **硬编码路径** | 🟡中 | `UIManager.UIPrefix` / `ConfigManager.ConfigPrefix` | 移至 GameCoreConfig ScriptableObject |
| 6 | **测试覆盖不足** | 🟡中 | 仅 Signal + PersistentData 有简易测试 | 利用 ServiceLocator + Mock 补充单元测试 |

### 4.3 推荐改进路线图

```
Phase 1 (核心补全)         Phase 2 (质量提升)         Phase 3 (功能扩展)
 ├─ InputManager            ├─ 命名空间规范化           ├─ 本地化模块
 ├─ C# 通用对象池           ├─ 单元测试覆盖             ├─ 红点系统
 ├─ 异步任务管理            ├─ GameMode 解耦业务        ├─ 引导系统
 └─ HTTP 网络客户端         └─ 路径配置化               ├─ Camera 管理增强
                                                        └─ 热更新流程
```

---

## 五、服务接口速查表

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

## 六、目录结构

```
Framework/
├── Foundation/          # 基础层：单例、ServiceLocator、日志、定时器、数学扩展
├── Core/                # 核心层：KGameCore、GameMode、TModule
├── Subscriber/          # 信号系统：KSignal、Subscriber
├── EventBus/            # 事件总线：全局类型路由
├── Coroutine/           # 自定义协程系统
├── Fsm/                 # 有限状态机
├── BehaviorTree/        # 行为树 (Fluid BT)
├── Cmd/                 # 命令队列
├── Action/              # 动作序列
├── Assets/              # 资源管理 (Addressables + AssetDatabase)
├── Config/              # 配置管理 (ScriptableObject)
├── UI/                  # UI 栈管理
├── Sound/               # 音效/音乐管理
├── Settings/            # 游戏设置
├── PersistentData/      # 持久化存档
├── Scene/               # 场景管理
├── Debug/               # 调试工具
├── Version/             # 版本信息
├── ObjectPool/          # 对象池 (GameObjectPool/PoolManager/CSharpPool)
├── FrameworkExt/        # 游戏扩展层 (Unit/Player/Vfx/HUD/Camera)
├── Attributes/          # 特性 (AutoBind/DisplayName)
├── JsonConverter/        # JSON 转换器
├── Audio/               # 音频配置
├── SerializedCollections/ # 可序列化字典
├── Utils/               # 工具组件
├── Testing/             # 单元测试
└── Editor/              # 编辑器扩展
```

---

## 七、变更记录

| 版本 | 变更 |
|---|---|
| **1.0.0** | 初始框架：KGameCore、GameMode、TModule、Signal/Subscriber、Timer、Coroutine、FSM、BT、Command、Action、UIManager、SoundManager、AssetManager、ConfigManager、PersistentDataManager、SettingsManager、DebugManager、VfxManager、ObjectPool |
| **1.1.0** | Bug 修复 (RequireModule NullRef、KSignal Invoke 可见性、AssetManager 缓存)；统一 API 命名 |
| **1.2.0** | 新增 KVersion 版本模块；新增 SceneManager (Addressables/Built-in、叠加场景、历史回退、过渡效果)；新增 EventBus (类型路由、优先级、粘性、一次性订阅) |
| **1.3.0** | Service Locator 改造：ServiceLocator 静态注册中心 + 10 个服务接口 + 所有 Manager 接口化注册；修复 UIManager.Awake 漏调 base；SoundManager.MusicVolume 字段→属性 |
| **1.4.0** | 日志系统重构：6 级日志 (Verbose/Debug/Info/Warning/Error/Fatal) + 模块 Tag 过滤 + 本地文件日志 (大小轮转、历史保留) + ILogService 接口 + 全部调用点迁移为结构化日志 |
| **1.5.0** | 通用对象池重构：`GameObjectPool` (单 Prefab 池 + IPoolable 回调) + `PoolManager` (多 Prefab 注册中心 + 实例跟踪) + `CSharpPool<T>` (纯 C# 泛型池) + `ListPool/DictionaryPool/HashSetPool` 集合池；VfxManager/VfxAPI 全部迁移至 PoolManager；UnitBase 实现 IPoolable 支持对象池回收复用；IPoolService 接口 + ServiceLocator 注册 |
| **1.6.0** | 音频系统全面重构：SoundData 新增 `maxConcurrent/cooldown/volumeDecayPerInstance/randomPitchRange` 并发控制字段；SoundEmitter 解耦 (`OnFinished` 回调替代硬引用)；SoundManager 新增 per-SoundData 并发跟踪 + 冷却 + 音量衰减 + AudioMixer 参数控制 (`SetMixerVolume/GetMixerVolume`) + Snapshot 过渡 + BGM Ducking (侧链压缩)；修复 PlayMusic 内存泄漏 (AudioSource 无限增长)；修复 PlaySoundLimitFrame 忽略 pos；修复 CrossFade 使用 deltaTime 而非 unscaledDeltaTime；AudioConfig 新增 Mixer 参数名配置；RandomAudioClip 初始化从 FixedUpdate 改为 OnEnable |
