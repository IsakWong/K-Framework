# 快速开始

## 项目结构

```
KFramework/
├── package.json              # UPM 包清单
├── LICENSE                   # MIT 许可证
├── CHANGELOG.md              # 版本变更日志
├── README.md
├── Runtime/                  # 运行时代码（KFramework.asmdef）
│   ├── Core/                 #   引擎无关内核（EventBus / Command / 协程 / 对象池 / 状态机）
│   ├── Unity/                #   引擎适配层（KGameCore / TModule / SceneController / 基础组件）
│   ├── Modules/              #   功能模块（UI / Sound / Asset / Config / Scene / Vfx ...）
│   ├── Extensions/           #   可选扩展（HUD / Player）
│   └── ThirdParty/           #   内嵌三方（namespace / asmdef 不变）
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
AssetService.Instance.LoadAsset<T>(path);

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

## SceneController 场景生命周期

```csharp
public class MySceneController : SceneController
{
    public override void OnSceneAwake()                  { /* 场景唤醒 */ }
    protected override IEnumerator OnSceneInitialize()   { /* 异步初始化 */ }
    public override IEnumerator OnSceneStart()           { /* 开始游戏 */ }
    public override void OnSceneEnd()                    { /* 结束清理 */ }
}
```

生命周期：`Awake → OnSceneAwake → InitializeScene → OnSceneInitialize → OnSceneStart`，`OnSceneRestart` 可重开，`OnSceneEnd` 在 `OnDestroy` 时调用。
