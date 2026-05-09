# 变更日志

## [1.7.0] - 2026-04-18

### 新增
- **TModule.OnModuleLogic(float)** — 虚方法，子类可重写以实现每帧模块逻辑更新
- **UnitComponent 生命周期回调** — `OnOwnerSpawn()` / `OnOwnerDie()`，由 UnitBase 自动调用
- **UnitComponent.subscriber** — 内置 Subscriber，在 `End()` 时自动清理
- **KTimer 进度属性** — `ElapsedTime`、`RemainingTime`、`Progress` (0~1)
- **`KSignal<T1,T2,T3,T4>`** — 四参数信号统一命名（原 KAction 保留为 Obsolete 别名）

### 变更
- **Signal 字段封装** — `_delegates`、`_delegatesOnce`、`_handlesMap` 从 `public` 改为 `protected`
- **UIPanel.OnHide()** 自动调用 `subscriber.DisconnectAll()`
- **`UnitBase.RequireUnitComponent<T>()`** 先查找已有组件，不存在时才创建
- **KTimerManager.OnLogic** 优化为单次反向遍历移除

### 修复
- **GameMode.SavePersistentData()** 返回类型从 `IEnumerable` 修正为 `IEnumerator`

## [1.6.0] - 2026-04-17

### 新增
- **SoundCategory** (ScriptableObject) — 基于身份的音频分类：Mixer 路由、并发上限、冷却、衰减、随机音高
- **Mixer 控制 API** — `SetMixerVolume` / `GetMixerVolume`、Snapshot 过渡、BGM Ducking
- **SoundEmitter 实现 IPoolable** — 音效对象池统一由 PoolManager 管理

### 变更
- **SoundData** 精简为纯 AudioSource 参数模板
- **项目结构** 重组为 UPM 包格式

### 修复
- PlayMusic 内存泄漏
- PlaySoundLimitFrame 忽略 position 参数
- CrossFade 使用 `Time.deltaTime` 导致暂停时淡入淡出停止
- SoundEmitter.Stop() 硬编码耦合

## [1.5.0] - 2026-04-16

### 新增
- **通用对象池系统** — `GameObjectPool` + `PoolManager` + `CSharpPool<T>` + 集合池
- **UnitBase** 通过 `CanRecycle` 支持 opt-in 对象池回收
- **IPoolService** 接口 + ServiceLocator 注册

## [1.4.0] - 2026-04-15

### 新增
- **EnhancedLog** — 结构化日志系统：6 级日志 + Tag 过滤 + 文件轮转
- **ILogService** 接口

## [1.3.0] - 2026-04-14

### 新增
- **Service Locator** 模式 — `Register<T>()` / `Get<T>()` / `TryGet<T>()`
- 10 个服务接口（IAssetService, IUIService, ISoundService 等）

## [1.2.0] - 2026-04-13

### 新增
- **EventBus** — 全局类型路由事件：优先级排序、粘性事件、一次性订阅
- **IEventBusService** 接口

## [1.1.0] - 2026-04-12

### 新增
- **SceneManager** — 异步场景加载，支持 Addressables/Built-in、叠加场景、历史回退
- **KVersion** — 框架版本 + 游戏版本 + 构建信息

### 修复
- RequireModule 空引用异常
- KSignal.Invoke 可见性问题
- AssetManager 缓存未释放

## [1.0.0] - 2026-04-10

### 新增
- 初始发布
- 核心架构：KGameCore、KSingleton、PersistentSingleton、TModule
- 配置系统、FSM、行为树、命令模式
- UI 系统、音效系统、持久化存档、协程系统
- Addressables 资源管理
