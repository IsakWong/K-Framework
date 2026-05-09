# 信号系统

K-Framework 的信号系统分为两部分：`KSignal`（点对点信号）和 `EventBus`（全局广播）。

## `KSignal` / `KSignal<T>`

`KSignal` 是轻量级的点对点信号机制，类似 C# event 但更安全，支持自动清理。

### 创建信号

```csharp
// 无参信号
public readonly KSignal OnDied = new KSignal();

// 单参数信号
public readonly KSignal<int> OnHealthChanged = new KSignal<int>();

// 四参数信号（最多支持 4 个参数）
public readonly KSignal<int, float, string, bool> OnComplexEvent = new();
```

### 连接信号

```csharp
// 单次连接
signal.Connect(() => Debug.Log("触发了"));

// 带参数的连接
signal.Connect(data => Debug.Log($"收到: {data}"));

// 一次性连接（触发后自动断开）
signal.ConnectOnce(() => Debug.Log("只触发一次"));

// 获取连接句柄（可用于手动断开）
var handle = signal.Connect(handler);
handle.Disconnect();
```

### 触发信号

```csharp
signal.Invoke();           // 无参
signal.Invoke(data);       // 带参数
```

## Subscriber

`Subscriber` 管理一组信号连接，支持一键清理，防止内存泄漏。

```csharp
public class MyClass
{
    private Subscriber subscriber = new Subscriber();

    public void Init(Unit unit)
    {
        // 通过 Subscriber 连接，自动追踪
        subscriber.Connect(unit.OnDied, OnUnitDied);
        subscriber.Connect(unit.OnHealthChanged, OnHealthChanged);
    }

    public void Cleanup()
    {
        // 一键断开所有连接
        subscriber.DisconnectAll();
    }
}
```

## 命名变更

在 1.7.0 中，四参数信号统一命名为 `KSignal<T1,T2,T3,T4>`（原 `KAction` 保留为 Obsolete 别名）。
