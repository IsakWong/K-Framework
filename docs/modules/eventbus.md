# 事件总线

`EventBus` 是全局类型路由事件系统，支持优先级排序、粘性事件、一次性订阅，与 `Subscriber` 集成。

## 基本用法

```csharp
// 定义事件（struct + IEvent 接口）
public struct EnemyKilledEvent : IEvent
{
    public string EnemyId;
    public int ScoreReward;
}

// 订阅事件
EventBus.Instance.Subscribe<EnemyKilledEvent>(OnEnemyKilled);

// 发布事件
EventBus.Instance.Publish(new EnemyKilledEvent { EnemyId = "boss", ScoreReward = 1000 });

// 取消订阅
EventBus.Instance.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
```

## 粘性事件

粘性事件保留最后发布的值，新订阅者立即收到历史值。

```csharp
// 发布粘性事件
EventBus.Instance.PublishSticky(new GameStateEvent { State = "Playing" });

// 稍后订阅，仍能收到上面的值
EventBus.Instance.Subscribe<GameStateEvent>(OnGameStateChanged);

// 查询粘性事件
var sticky = EventBus.Instance.QuerySticky<GameStateEvent>();
```

## 服务接口

```csharp
var eventBus = ServiceLocator.Get<IEventBusService>();
eventBus.Subscribe<EnemyKilledEvent>(handler);
eventBus.Publish(new EnemyKilledEvent());
eventBus.Unsubscribe<EnemyKilledEvent>(handler);
```

## 高级特性

### 优先级

订阅时可以指定优先级，高优先级的处理器先执行：

```csharp
EventBus.Instance.Subscribe<EnemyKilledEvent>(OnEnemyKilled, priority: 100);
```

### 一次性订阅

触发后自动取消订阅：

```csharp
EventBus.Instance.SubscribeOnce<EnemyKilledEvent>(OnFirstKill);
```

### 与 Subscriber 集成

```csharp
subscriber.Subscribe(EventBus.Instance, OnEnemyKilled);
// subscriber.DisconnectAll() 自动取消所有 EventBus 订阅
```

## 与 KSignal 的区别

| 特性 | KSignal | EventBus |
|------|---------|----------|
| 通信方式 | 点对点（持有引用） | 全局广播（类型路由） |
| 粘性事件 | ❌ | ✅ |
| 优先级 | ❌ | ✅ |
| 适用场景 | 组件内部通信 | 跨模块通信 |
