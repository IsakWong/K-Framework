# 基础层

Foundation Layer 提供框架最底层的基础设施：单例模式、服务定位、日志、定时器、数学工具等。

## 类清单

| 文件 | 类/接口 | 说明 |
|---|---|---|
| `Base.cs` | `KSingleton<T>` | 纯 C# 单例基类，自动注册 ServiceLocator |
| `PersistentSingleton.cs` | `PersistentSingleton<T>` | MonoBehaviour 单例，DontDestroyOnLoad |
| `InstanceBehaviour.cs` | `InstanceBehaviour<T>` | 非单例 MonoBehaviour 基类 |
| `ServiceLocator.cs` | `ServiceLocator` | 静态服务注册中心（Register/Get/TryGet/Unregister/Reset） |
| `EnhancedLog.cs` | `EnhancedLog` / `ILogService` | 分级日志系统（6 级：Verbose/Debug/Info/Warning/Error/Fatal），模块 Tag 过滤，文件轮转，平台适配 |
| `Timer.cs` | `KTimer` / `KTimerManager` | 定时器系统，支持循环/暂停/停止 |
| `Variant.cs` | `Variant` | 通用变体类型 |
| `MathExtension.cs` | — | 数学扩展方法 |
| `GameObjectExtensions.cs` | — | GameObject 扩展方法 |
| `SerializeType.cs` | `SerializeType` | 类型序列化辅助 |
| `KConstraint.cs` | — | 约束系统 |
| `Selection2D.cs` | — | 2D 选择辅助 |
| `Utility.cs` | — | 通用工具方法 |

## `KSingleton<T>`

纯 C# 单例基类。任何继承 `KSingleton<T>` 的类自动获得单例能力，并在构造时自动注册到 `ServiceLocator`。

```csharp
public class MyManager : KSingleton<MyManager>
{
    // Instance 属性自动可用
}
```

## `PersistentSingleton<T>`

MonoBehaviour 单例基类，带 `DontDestroyOnLoad`。适用于需要在场景切换时保持的 GameObject。

```csharp
public class MyMonoManager : PersistentSingleton<MyMonoManager>
{
    // 场景切换不会销毁此对象
}
```

## ServiceLocator

静态服务注册中心，是框架服务发现的核心。

```csharp
// 注册
ServiceLocator.Register<IMyService>(new MyService());

// 获取（不存在抛异常）
var svc = ServiceLocator.Get<IMyService>();

// 安全获取
if (ServiceLocator.TryGet<IMyService>(out var svc))
    svc.DoSomething();

// 注销
ServiceLocator.Unregister<IMyService>();

// 重置全部
ServiceLocator.Reset();
```

## EnhancedLog

结构化日志系统，支持 6 级日志、模块 Tag 过滤、文件轮转。

```csharp
// 基本用法
EnhancedLog.Info("MyModule", "操作完成");
EnhancedLog.Warning("MyModule", "资源即将超限");
EnhancedLog.Error("MyModule", "加载失败");

// 设置全局日志级别
EnhancedLog.SetGlobalLevel(LogLevel.Warning);

// 按模块设置日志级别
EnhancedLog.SetTagLevel("Sound", LogLevel.Verbose);
```

日志级别（从低到高）：`Verbose → Debug → Info → Warning → Error → Fatal`

## KTimer

不依赖 MonoBehaviour 的定时器系统。

```csharp
// 创建一次性定时器
var timer = KTimer.Create(3f, () => Debug.Log("3 秒后执行"));

// 循环定时器
var loop = KTimer.CreateLoop(1f, () => Debug.Log("每秒执行"));

// 查询进度
float elapsed = timer.ElapsedTime;
float remaining = timer.RemainingTime;
float progress = timer.Progress; // 0~1

// 控制
timer.Pause();
timer.Resume();
timer.Stop();
```
