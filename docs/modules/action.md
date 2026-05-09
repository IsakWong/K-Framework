# Flow & Trigger

一套统一的可执行单元抽象——既支持流式编排（Flow），也支持事件驱动（Trigger）。

## 核心抽象

| 类型 | 作用 |
|------|------|
| `IFlowNode` | 可执行单元（`IEnumerator Execute(FlowContext)`） |
| `IFlowCondition` | 条件抽象（`bool Evaluate(FlowContext)`） |
| `FlowContext` | 共享上下文（节点间传值、取消标志、异常记录） |
| `FlowHandle` | 运行句柄（取消、查询、等待完成） |
| `Flow` | 静态入口，`Flow.Create()` 开始链式构建 |
| `KTrigger` | 触发器，`KTrigger.Once()` / `KTrigger.Repeating()` 开始构建 |

## Flow 示例

### 基本顺序

```csharp
Flow.Create()
    .Do(_ => Debug.Log("Step 1"))
    .Wait(0.5f)
    .Do(_ => Debug.Log("Step 2"))
    .Build()
    .Run(this);   // this = 任意 MonoBehaviour
```

### 等信号

```csharp
Flow.Create()
    .Do(_ => Debug.Log("等待 UI 关闭..."))
    .WaitFor(uiPanel.OnClosed)         // KSignal
    .Do(_ => Debug.Log("UI 已关闭"))
    .Build()
    .Run(this);
```

### 游戏流程示例

```csharp
Flow.Create()
    .Do("打开遗物选择 UI", ctx => ctx.Set(UIManager.Push<RelicSelectUI>()))
    .WaitFor(ctx => ctx.Get<RelicSelectUI>().OnSelected, onFired: (ctx, relic) =>
    {
        player.AddRelic(relic);
    })
    .WaitFor(ctx => ctx.Get<RelicSelectUI>().OnCloseAnimDone)
    .Do("开始战斗", _ => roomCombat.StartCombat())
    .WaitFor(roomCombat.OnCombatCompleted)
    .Do("生成奖励", _ => rewardCtrl.SpawnRewards())
    .Build()
    .Run(this);
```

### 分支

```csharp
Flow.Create()
    .If(_ => player.HasWand())
        .Then(b => b.Do(_ => Debug.Log("有武器")))
        .Else(b => b.Do(_ => GiveStarterWand()))
    .Build()
    .Run(this);
```

### 循环

```csharp
// 固定次数
Flow.Create()
    .Repeat(3, b => b.Do(_ => SpawnEnemy()).Wait(0.5f))
    .Build()
    .Run(this);

// While 循环
Flow.Create()
    .While(_ => enemyCount > 0,
        b => b.Wait(0.1f).Do(_ => CheckEnemies()))
    .Build()
    .Run(this);
```

### 并行

```csharp
// 全部完成才继续
Flow.Create()
    .ParallelAll(
        b => b.Do(_ => SoundManager.Play("alert")),
        b => b.Do(_ => animator.SetTrigger("entry")).Wait(1.0f),
        b => b.WaitFor(player.OnAnyInput)
    )
    .Do(_ => Debug.Log("一切就绪"))
    .Build()
    .Run(this);

// 任一完成即继续
Flow.Create()
    .ParallelAny(
        b => b.WaitFor(player.OnConfirm),
        b => b.Wait(5f).Do(_ => Debug.Log("超时"))
    )
    .Build()
    .Run(this);
```

### 取消

```csharp
var handle = myFlow.Run(this);
// ...
if (someCondition) handle.Cancel();
```

## KTrigger 示例

### 一次性成就

```csharp
KTrigger.Once()
    .Named("击杀 100 个敌人")
    .On<EnemyKilledEvent>()
    .When(_ => SaveData.TotalKills + 1 >= 100)
    .Do(() =>
    {
        SaveData.UnlockAchievement("BloodSpiller");
        UIManager.Show<AchievementToast>();
    })
    .BuildAndRegister();
```

### 持续监听

```csharp
KTrigger.Repeating()
    .On(player.OnHitTaken)
    .When(_ => player.CurrentHealth < player.MaxHealth * 0.3f)
    .Do(_ => UIManager.Show<LowHealthWarning>())
    .BuildAndRegister();
```

### EventBus 过滤

```csharp
KTrigger.Repeating()
    .On<EnemyKilledEvent>(e => e.EnemyId == bossId)
    .Do(_ => SoundManager.Play("victory_fanfare"))
    .BuildAndRegister();
```

## FlowBuilder API 速查

| 方法 | 说明 |
|------|------|
| `Do(Action<FlowContext>)` | 单步同步动作 |
| `Wait(seconds)` | 等待秒数 |
| `WaitFor(KSignal)` | 等待无参信号 |
| `WaitFor<T>(KSignal<T>, filter, onFired)` | 等待带参信号 |
| `WaitForEvent<T>(filter)` | 等待 EventBus 事件 |
| `WaitUntil(pred)` | 等待谓词为 true |
| `If(pred).Then(...).Else(...)` | 分支 |
| `Repeat(n, body)` | 固定次数循环 |
| `While(pred, body)` | while 循环 |
| `ParallelAll(...branches)` | 并行：等全部 |
| `ParallelAny(...branches)` | 并行：等任一 |
| `Build()` | 返回根 IFlowNode |

## FlowContext

```csharp
ctx.Set("key", value);
ctx.Get<T>("key", fallback);
ctx.Set(myObj);
ctx.Get<MyClass>();
ctx.Has(...) / TryGet(...) / Remove(...);
ctx.Cancel();
```

## 常见陷阱

- **闭包捕获**：循环里 `Do(_ => doSomething(i))` 会捕获循环结束后的值，用局部变量解决
- **并行共享 ctx**：`ParallelAll` 子分支共享 `FlowContext`，对同一 key 写值是竞态
- **Trigger 执行期间忽略新事件**：不会排队
- **必须有 MonoBehaviour Owner**：`Flow.Run(this)` 的 `this` 必须是激活的 MonoBehaviour
