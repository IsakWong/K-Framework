# TODO

## 缺失模块

### 高优先级 — 生产项目必需

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 1 | **InputManager（输入管理）** | `ControllerBase` 引用了 Unity InputSystem 但无统一输入管理层。缺少输入映射切换、按键重绑定、多设备支持、输入缓冲 | 封装 InputSystem 的 `PlayerInput`，提供 `IInputService` 接口，支持 Action Map 切换和运行时重绑定 |
| 2 | **NetworkManager（网络模块）** | 完全无网络代码。联机游戏需要 HTTP 请求、WebSocket、状态同步/帧同步 | 至少提供 `INetworkService` 抽象 + HTTP 客户端（UnityWebRequest 封装）；高级可选 WebSocket/Netcode |
| 3 | **异步任务管理** | `AssetManager` 用了 async/await 但无统一的异步任务管理。缺少取消（CancellationToken）、超时、重试、并发控制 | 提供 `AsyncTaskRunner`：支持 CancellationToken 传播、超时包装、并发限制队列 |
| 4 | **命名空间规范化** | 154 个文件使用 `Framework.*`，但仍有部分类在全局命名空间 | 统一为 `KFramework.*` 命名空间层级 |

### 中优先级 — 正式上线前应补全

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 5 | **Localization（本地化/多语言）** | 无国际化模块，UI 文本硬编码 | `ILocalizationService` + 多语言表（CSV/JSON/ScriptableObject）；支持运行时切换语言、文本/图片/音频本地化 |
| 6 | **热更新流程** | 有 Addressables 但无热更流程（版本检查、资源对比、增量下载、进度回调） | 在 `AssetManager` 基础上增加 `IHotUpdateService`：CheckUpdate → DownloadDiff → Apply |
| 7 | **Camera 管理增强** | `CameraInstance` 仅持有引用，缺少跟随、震动、过渡、多相机切换 | 新增 `CameraManager`：跟随策略（平滑/弹性/锁定）、屏幕震动、相机混合过渡 |
| 9 | **Analytics（数据埋点）** | 无事件追踪/数据分析模块 | `IAnalyticsService` 接口 + 可插拔后端（Firebase/自建），自动追踪场景切换、UI 停留时长 |
| 10 | **红点系统（Badge/Notification）** | 无 UI 红点提示系统，常见功能型游戏必需 | 树状红点管理器：父节点自动聚合子节点状态，支持数字/点两种模式 |

### 低优先级 — 按需添加

| # | 缺失模块 | 说明 | 建议方案 |
|---|---|---|---|
| 11 | **引导/教程系统** | 无新手引导框架 | 步骤驱动引导系统：高亮遮罩、强制点击、对话气泡、条件触发 |
| 12 | **Download Manager** | 缺少大文件下载管理（断点续传、多线程下载） | 独立于 Addressables，用于补丁包/DLC 资源下载 |
| 13 | **Shader/Material 管理** | 无全局 Shader 变体收集和材质参数管理 | ShaderManager：变体预热、全局属性设置 |
| 14 | **平台适配层** | 微信小游戏（#if WEIXINMINIGAME）散落在各处，无统一平台抽象 | `IPlatformService`：统一文件系统、分享、支付、广告等平台差异 |
| 15 | **Timeline/Cutscene** | 无过场动画管理 | 可选集成 Unity Timeline 的管理层 |
| 16 | **AI 模块扩展** | 行为树已有，但缺少黑板（Blackboard）、感知系统（Perception） | 扩展 BehaviorTree：共享黑板 + 视觉/听觉感知组件 |

## 已知设计问题

| # | 问题 | 严重度 | 现状 | 建议 |
|---|---|---|---|---|
| 1 | **缺少命名空间** | 🟡中 | 大量类在全局命名空间 | 统一 `KFramework.*` 命名空间 |
| 2 | **GameMode 耦合业务** | 🟡中 | `OnPlayerDeath/OnPlayerRespawn` 等业务回调在框架层 | 改为泛型事件或移到扩展层 |
| 3 | **UnitModule 封装不足** | 🟡中 | `_toSpawnUnits` 等内部队列为 public | 改为 internal 或只暴露方法 |
| 4 | **线程安全** | 🟢低 | 所有 Manager 无线程保护 | 加锁或标注 `[MainThread]` 限制 |
| 5 | **硬编码路径** | 🟢低 | `UIManager.UIPrefix` / `ConfigManager.ConfigPrefix` | 已改为实例属性，业务在 GameCore.OnInit() 中配置 |
| 6 | **测试覆盖不足** | 🟡中 | 仅 Signal + PersistentData 有简易测试 | 利用 ServiceLocator + Mock 补充单元测试 |

## 推荐改进路线图

```
Phase 1（核心补全）         Phase 2（质量提升）         Phase 3（功能扩展）
 ├─ InputManager            ├─ 命名空间规范化           ├─ 本地化模块
 ├─ 异步任务管理            ├─ 单元测试覆盖             ├─ 红点系统
 └─ HTTP 网络客户端         ├─ GameMode 解耦业务        ├─ 引导系统
                            └─ 路径配置化               ├─ Camera 管理增强
                                                        └─ 热更新流程
```

> 已完成：C# 通用对象池（v1.5.0）· 音频系统全面重构（v1.6.0）· UPM 包结构（v1.6.0）
