# 变更日志

本文件记录 KFramework 的所有重要变更，格式遵循 [Keep a Changelog](https://keepachangelog.com/)，
版本号遵循 [语义化版本](https://semver.org/)。

## [2.0.0] - 2026-09-20

### 破坏性变更
- **`UnitBase` 由 `MonoBehaviour` 改为纯 C# 逻辑内核** — 不再继承 `MonoBehaviour`、不再实现 `IPoolable`，不持有 `GameObject` / `Transform` 引用，可脱离场景构造。生命周期状态机、运行时计时器、逻辑开关（`EnableOnLogic`）与三个信号（`onLogic` / `OnUnitDie` / `OnUnitSpawn`）留在内核。
- **新增 `UnityUnit : MonoBehaviour, IUnitHost, IPoolable`** 承接全部引擎职责 — 改名 / `SetActive` 切换 / `Invoke` 延迟删除 / `Destroy` 或回池 / 跨场景持久化标记 / 挂点 VFX / Gizmos / transform 读写。与内核为**组合**关系（C# 单继承位已被 `MonoBehaviour` 占用），经 `UnityUnit.Core` 取内核、`Core.Host` 取回宿主；子类覆写 `CreateCore()` 提供自己的内核类型。
- **新增 `IUnitHost` 宿主回调接口** — 内核只认该接口：`OnHostSpawned` / `OnHostDying` / `OnHostDied` / `OnHostDeleted` / `OnHostLogic` / `OnHostStateChanged` 由宿主实现。`OnSpawn()` / `OnDie()` / `OnLogic()` 保留为 `UnityUnit` 的子类覆写钩子，`IUnitHost` 显式实现转发到它们。
- **`UnitComponent` 体系整体废除** — `UnitComponent` 与 `IUnitComponent` 删除，内核的组件容器与 `__Components__` 自动扫描移除；原组件子类改为直接继承 `MonoBehaviour`。
- **7 个序列化字段迁至 `UnityUnit`** — `DefaultEnableLogic` / `SpawnOnStart` / `DeleteDelay` / `UnityDestroyDelay` / `DieDisableGameObjects` / `DieEnableGameObjects` / `Selectable`，字段名、类型、默认值与特性均保持不变，既有 Prefab 与场景无需数据迁移。
- **`UnitBase.SelfTimeScale` 不再序列化** — 改由内核持有（原为 `[HideInInspector] public float`）。

### 新增
- **`UnitBase.Host` / `UnitBase.Name`** — 内核经宿主取显示名与世界坐标；`unit.Host as XXX` 成为从内核取回表现宿主的惯用法。
- **`UnityUnit` 转发门面** — `IsAlive` / `IsSpawned` / `IsDeleted` / `LifecycleState` / `Spawn()` / `Die()` / `Delete()` / `SetLogicEnable()` / `PreventDestroy` / `TimerManager` / 三个信号等，原调用点无需改动。
- **`UnitBase.ResetForPool()` / `CleanupForPool()`** — 池复用的内核态复位与清理，由宿主的 `IPoolable` 实现调用。

### 变更
- **`UnitModule` 的队列与 tick 目标为纯 C# 内核**（`List<UnitBase>`）；7 处日志由 `unit.gameObject.name` 改读 `unit.Name`。
- **`Utility.DirectionBetweenUnit` / `DistanceBetweenGameUnit`**、**`VfxAPI.CreateVisualEffectAtUnit`** 的接收者类型由 `UnitBase` 改为 `UnityUnit`。
- **`GameMode.OnPlayerDeath` / `OnPlayerRespawn`** 与 **`IGameModeEventListener`** 的同名回调形参保持 `UnitBase`，日志改读 `Name`。
- **`UnitBaseEditor`** 的 `[CustomEditor]` 目标由 `UnitBase` 改为 `UnityUnit`；**`UnitMonitorPage`** 改从 `Host` 取 GameObject 做选中与 Inspector 预览。

## [1.7.0] - 2026-04-18

### 新增
- **TModule.OnModuleLogic(float)** — 虚方法，子类可重写以实现每帧模块逻辑更新
- **UnitComponent 生命周期回调** — `OnOwnerSpawn()` / `OnOwnerDie()`，由 UnitBase 自动调用
- **UnitComponent.subscriber** — 内置 Subscriber，在 `End()` 时自动清理
- **KTimer 进度属性** — `ElapsedTime`、`RemainingTime`、`Progress` (0~1)
- **KSignal<T1,T2,T3,T4>** — 四参数信号统一命名为 KSignal（原 KAction 保留为 Obsolete 别名）

### 变更
- **Signal 字段封装** — `_delegates`、`_delegatesOnce`、`_handlesMap` 从 `public` 改为 `protected`
- **UIPanel.OnHide()** 自动调用 `subscriber.DisconnectAll()`，防止信号泄漏
- **UnitBase.RequireUnitComponent<T>()** 现在先查找已有组件，不存在时才创建（原来与 AddUnitComponent 行为完全相同）
- **KTimerManager.OnLogic** 优化为单次反向遍历移除，消除双重 `_toRemove` 处理
- **Module.GetData<T>()** 已移除（原来直接抛 NotImplementedException）
- **Timer.cs** 移除无用的 `CleverCrow.Fluid.BTs` using 引用

### 修复
- **GameMode.SavePersistentData()** 返回类型从 `IEnumerable` 修正为 `IEnumerator`

## [1.6.0] - 2026-04-17

### 新增
- **SoundCategory** (ScriptableObject) — 基于身份的音频分类，提供 Mixer 路由、并发上限、冷却时间、逐实例音量衰减、随机音高
- **Mixer 控制 API** — `SetMixerVolume` / `GetMixerVolume`、Snapshot 过渡、BGM Ducking 侧链压缩
- **SoundEmitter 实现 IPoolable** — 音效对象池统一由 PoolManager 管理，不再使用独立的 Unity ObjectPool

### 变更
- **SoundData** 精简为纯 AudioSource 参数模板（移除 clip、mixerGroup、并发控制字段）
- **SoundBuilder.Play()** 接受 `SoundCategory` 参数用于路由与并发控制
- **AudioConfig** 预设类型从 `SoundData` 改为 `SoundCategory` 引用
- **项目结构** 重组为 UPM 包格式 (`Runtime/` + `Editor/` + `Tests/`)

### 修复
- PlayMusic 内存泄漏 — 旧 AudioSource 组件从未销毁，musicStack 无限增长
- PlaySoundLimitFrame 忽略 position 参数（缺少 With3DPosition 调用）
- CrossFade 使用 `Time.deltaTime` 而非 `Time.unscaledDeltaTime`，暂停时淡入淡出停止
- SoundEmitter.Stop() 硬编码 SoundManager.Instance 耦合（改为 OnFinished 回调）
- RandomAudioClip 在 FixedUpdate 中执行一次性初始化（改为 OnEnable）
- StopAll() 清空 FrequentSoundEmitters 后异步淡出回调尝试 Remove 导致崩溃
- PlaySoundLimitFrame 延迟到 FixedUpdate 时丢失 SoundCategory

## [1.5.0] - 2026-04-16

### 新增
- **通用对象池系统** — `GameObjectPool` (单 Prefab 池 + IPoolable 回调) + `PoolManager` (多 Prefab 注册中心 + 实例跟踪) + `CSharpPool<T>` (纯 C# 泛型池) + `ListPool/DictionaryPool/HashSetPool` 集合池
- **UnitBase** 通过 `CanRecycle` 重写支持 opt-in 对象池回收
- **IPoolService** 接口 + ServiceLocator 注册
- VfxManager 全部迁移至 PoolManager

## [1.4.0] - 2026-04-15

### 新增
- **EnhancedLog** — 结构化日志系统：6 级日志 (Verbose/Debug/Info/Warning/Error/Fatal)、模块 Tag 过滤、本地文件日志 (大小轮转 + 历史保留)
- **ILogService** 接口通过 ServiceLocator 访问

### 变更
- 全部调用点从 `Debug.Log` 迁移为结构化日志 `EnhancedLog.Info("Module", msg)`

## [1.3.0] - 2026-04-14

### 新增
- **Service Locator** 模式 — `ServiceLocator.Register<T>()` / `Get<T>()` / `TryGet<T>()` / `Unregister<T>()`
- 10 个服务接口 (IAssetService, IUIService, ISoundService 等)
- 所有 Manager 自动注册到 ServiceLocator

### 修复
- UIManager.Awake 漏调 base 导致单例未注册
- SoundManager.MusicVolume 字段改为属性

## [1.2.0] - 2026-04-13

### 新增
- **EventBus** — 全局类型路由事件：优先级排序、粘性事件、一次性订阅、与 Subscriber 集成
- **IEventBusService** 接口

## [1.1.0] - 2026-04-12

### 新增
- **SceneManager** — 异步场景加载：支持 Addressables/Built-in、叠加场景、历史回退、过渡效果回调
- **KVersion** — 框架版本 + 游戏版本 + 构建信息追踪

### 修复
- RequireModule 空引用异常
- KSignal.Invoke 可见性问题
- AssetManager 缓存未释放

## [1.0.0] - 2026-04-10

### 新增
- 初始发布
- 核心架构：KGameCore、KSingleton、PersistentSingleton、TModule 可热插拔模块
- 配置系统 (ConfigManager)、有限状态机 (FSM)、行为树 (Fluid BT)、命令模式 (Command/CommandQueue)
- UI 系统 (UIPanel、UIManager、InfiniteScroll)
- 音效系统 (SoundManager、SoundEmitter、SoundBuilder)
- 持久化存档 (PersistentDataManager)、协程系统 (KCoroutine)、Addressables 资源管理
- SerializedCollections 可序列化字典、Unity 类型 JsonConverter
