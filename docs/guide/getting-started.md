# 快速开始

## 项目结构

```
KFramework/
├── package.json              # UPM 包清单
├── LICENSE                   # MIT 许可证
├── CHANGELOG.md              # 版本变更日志
├── README.md
├── Runtime/                  # 运行时代码
│   ├── KFramework.asmdef
│   ├── Foundation/           # 基础层（Singleton, ServiceLocator, Log）
│   ├── Core/                 # 核心层（KGameCore, GameMode, TModule）
│   ├── Sound/                # 音频系统
│   ├── UI/                   # UI 管理
│   ├── ObjectPool/           # 对象池
│   ├── EventBus/             # 事件总线
│   ├── Fsm/                  # 状态机
│   ├── BehaviorTree/         # 行为树
│   └── ...                   # 更多模块
├── Editor/                   # 编辑器扩展
├── Tests/                    # 测试
├── Documentation~/           # 设计文档（Unity 忽略）
└── Samples~/                 # 示例（Unity 忽略）
```

## 初始化框架

框架的核心是 `KGameCore`，它是一个全局单例，管理所有模块的生命周期。

### 创建 GameCore

在你的启动场景中创建一个空 GameObject，挂载 `GameCoreProxy` 组件即可驱动整个框架。

### 访问服务

框架提供三种服务访问方式：

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

## 创建模块

继承 `TModule<T>` 创建可热插拔模块：

```csharp
public class MyModule : TModule<MyModule>
{
    // Awake 中自动注册到 KGameCore
}
```

通过 `KGameCore` 获取模块：

```csharp
KGameCore.RequireSystem<MyModule>();  // 获取或自动创建
KGameCore.SystemAt<MyModule>();       // 获取，不存在返回 null
```

## GameMode 场景生命周期

```csharp
public class MyGameMode : GameMode
{
    public override IEnumerator Init()     { /* 初始化 */ }
    public override IEnumerator Start()    { /* 开始游戏 */ }
    public override IEnumerator End()      { /* 结束游戏 */ }
}
```

生命周期：`Awake → Init → Start → End`
