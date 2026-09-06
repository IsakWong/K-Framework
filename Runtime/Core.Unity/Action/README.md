# Action: Flow & Trigger

> 一套统一的可执行单元抽象——既支持流式编排（Flow），也支持事件驱动（Trigger）。

## 这是什么

`KFramework.Action` 提供两个互补的入口，共享同一套节点库：

- **`Flow`** —— 链式构建一段可编排的逻辑：顺序、分支、循环、并行、等待信号/事件/条件
- **`KTrigger`** —— 事件-条件-动作模式：等某个事件触发 → 评估条件 → 执行一段 Flow

两者底层都是 `IFlowNode`，可互相嵌套。

## 核心抽象

| 类型 | 作用 |
|------|------|
| `IFlowNode` | 可执行单元（`IEnumerator Execute(FlowContext)`） |
| `IFlowCondition` | 条件抽象（`bool Evaluate(FlowContext)`） |
| `FlowContext` | 共享上下文（节点间传值、取消标志、异常记录） |
| `FlowHandle` | 运行句柄（取消、查询、等待完成） |
| `Flow` | 静态入口，`Flow.Create()` 开始链式构建 |
| `KTrigger` | 触发器，`KTrigger.Once()` / `KTrigger.Repeating()` 开始构建 |

## Flow 用法示例

### 1. 最简单的顺序

```csharp
Flow.Create()
    .Do(_ => Debug.Log("Step 1"))
    .Wait(0.5f)
    .Do(_ => Debug.Log("Step 2"))
    .Build()
    .Run(this);   // this = 任意 MonoBehaviour
```

### 2. 等信号

```csharp
Flow.Create()
    .Do(_ => Debug.Log("等待 UI 关闭..."))
    .WaitFor(uiPanel.OnClosed)         // KSignal
    .Do(_ => Debug.Log("UI 已关闭"))
    .Build()
    .Run(this);
```

### 3. 玩家选择遗物 → UI 动画结束 → 进入战斗

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

### 4. 分支

```csharp
Flow.Create()
    .If(_ => player.HasWand())
        .Then(b => b.Do(_ => Debug.Log("有武器")))
        .Else(b => b.Do(_ => GiveStarterWand()))
    .Build()
    .Run(this);
```

### 5. 循环

```csharp
Flow.Create()
    .Repeat(3, b => b.Do(_ => SpawnEnemy()).Wait(0.5f))
    .Build()
    .Run(this);

// While：直到条件不满足才退出循环体
Flow.Create()
    .While(_ => enemyCount > 0,
        b => b.Wait(0.1f).Do(_ => CheckEnemies()))
    .Build()
    .Run(this);
```

### 6. 并行

```csharp
// 同时播放音效 + 动画 + 等待玩家输入，全部完成才继续
Flow.Create()
    .ParallelAll(
        b => b.Do(_ => SoundManager.Play("alert")),
        b => b.Do(_ => animator.SetTrigger("entry")).Wait(1.0f),
        b => b.WaitFor(player.OnAnyInput)
    )
    .Do(_ => Debug.Log("一切就绪"))
    .Build()
    .Run(this);

// 任一完成即继续：超时退出
Flow.Create()
    .ParallelAny(
        b => b.WaitFor(player.OnConfirm),
        b => b.Wait(5f).Do(_ => Debug.Log("超时"))
    )
    .Build()
    .Run(this);
```

### 7. 取消

```csharp
var handle = myFlow.Run(this);
// ...
if (someCondition) handle.Cancel();   // 立即停止后续节点；订阅会被 finally 清理
```

### 8. 协程内等 Flow 完成

```csharp
IEnumerator MyCoroutine()
{
    var handle = Flow.Create().Wait(2f).Build().Run(this);
    yield return handle.Wait();
    Debug.Log("Flow 完成");
}
```

### 9. 在 Flow 中嵌入自定义协程

```csharp
Flow.Create()
    .Do(ctx => MyCustomCoroutine(ctx))   // Func<FlowContext, IEnumerator>
    .Build()
    .Run(this);

IEnumerator MyCustomCoroutine(FlowContext ctx)
{
    yield return new WaitForSeconds(0.5f);
    ctx.Set("result", 42);
}
```

## KTrigger 用法示例

### 1. 一次性成就

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

### 2. 持续监听低血量

```csharp
KTrigger.Repeating()
    .On(player.OnHitTaken)                              // KSignal
    .When(_ => player.CurrentHealth < player.MaxHealth * 0.3f)
    .Do(_ => UIManager.Show<LowHealthWarning>())
    .BuildAndRegister();
```

### 3. EventBus + 过滤

```csharp
KTrigger.Repeating()
    .On<EnemyKilledEvent>(e => e.EnemyId == bossId)     // 仅 Boss
    .Do(_ => SoundManager.Play("victory_fanfare"))
    .BuildAndRegister();
```

### 4. 触发器的动作是一段 Flow

```csharp
var unlockFlow = Flow.Create()
    .Do(_ => UIManager.Show<AchievementUI>())
    .WaitFor(achievementUI.OnClosed)
    .Do(_ => Inventory.Add(reward))
    .Build();

KTrigger.Once()
    .On<BossDefeatedEvent>()
    .Do(unlockFlow)
    .BuildAndRegister();
```

### 5. 手动触发

```csharp
var trigger = KTrigger.Repeating()
    .OnManual()
    .Do(() => Debug.Log("手动触发"))
    .BuildAndRegister();

// 任意时机：
trigger.Fire();
```

## API 速查

### FlowBuilder

| 方法 | 说明 |
|------|------|
| `Do(Action<FlowContext>)` | 单步同步动作 |
| `Do(Func<FlowContext, IEnumerator>)` | 单步协程动作 |
| `Do(IFlowNode)` | 嵌入任意节点 |
| `Wait(seconds, realtime=false)` | 等待秒数 |
| `WaitFrames(n)` | 等待 n 帧 |
| `WaitFor(KSignal)` | 等待无参信号 |
| `WaitFor<T>(KSignal<T>, filter, onFired)` | 等待带参信号 |
| `WaitForEvent<T>(filter)` | 等待 EventBus 事件 |
| `WaitUntil(pred)` | 等待谓词为 true |
| `WaitWhile(pred)` | 谓词为 true 期间持续等待 |
| `If(pred).Then(...).Else(...)` | 分支 |
| `Repeat(n, body)` | 固定次数循环 |
| `While(pred, body)` | 循环（前判） |
| `Until(pred, body)` | 循环（后判 break-on-true） |
| `ParallelAll(...branches)` | 并行：等全部 |
| `ParallelAny(...branches)` | 并行：等任一 |
| `SubFlow(IFlowNode)` | 嵌入子流程 |
| `Build()` | 返回根 IFlowNode |

### FlowHandle

| 成员 | 说明 |
|------|------|
| `IsRunning / IsCompleted / IsCancelled / IsFailed` | 状态 |
| `Error` | 异常对象（IsFailed 时） |
| `Context` | 完整 FlowContext |
| `Cancel()` | 请求取消 |
| `Wait()` | 协程内等待完成（IEnumerator） |
| `OnFinished` | 完成回调 |

### FlowContext

```csharp
ctx.Set("key", value);   ctx.Get<T>("key", fallback);
ctx.Set(myObj);          ctx.Get<MyClass>();          // 类型化
ctx.Has(...) / TryGet(...) / Remove(...);
ctx.Cancel();
```

### KTriggerBuilder

| 方法 | 说明 |
|------|------|
| `Named(name)` | 命名（仅用于调试） |
| `On(KSignal)` / `On<T>(KSignal<T>, filter)` | KSignal 源 |
| `On<T>(filter)` | EventBus 源（T : struct, IEvent） |
| `OnManual()` | 仅手动 Fire() |
| `When(pred)` / `When(IFlowCondition)` | 条件 |
| `Do(IFlowNode / Action / Action<FlowContext>)` | 动作 |
| `Build()` | 返回未注册的 KTrigger |
| `BuildAndRegister(runner)` | 构建并立刻注册 |

## 常见陷阱

### ⚠️ 闭包捕获陷阱

```csharp
// ❌ 在循环里 Do(_ => doSomething(i))，i 只会是循环结束后的值
for (int i = 0; i < 5; i++)
    flow.Do(_ => Debug.Log(i));   // 全部打印 5

// ✅ 用局部变量
for (int i = 0; i < 5; i++)
{
    int idx = i;
    flow.Do(_ => Debug.Log(idx));
}
```

### ⚠️ ParallelAll 共享 ctx

并行的子分支共享同一个 `FlowContext`，对同一 key 写值是竞态。如果分支需要独立状态，自己定义 key 命名空间或不写 ctx。

### ⚠️ Trigger 执行期间忽略新事件

为了避免同一个触发器并发执行，Action 在跑的时候，新的事件触发会**直接被忽略**——而不是排队。如果需要排队语义，自己用 `Repeat` + `WaitFor` 实现。

### ⚠️ 必须有 MonoBehaviour 作为 Owner

`Flow.Run(this)` 的 `this` 必须是激活状态的 MonoBehaviour，否则协程不会跑。`KTrigger` 默认用 `TriggerManager.Instance`（DontDestroyOnLoad）。

### ⚠️ 取消的及时性

`handle.Cancel()` 不会立刻终止——节点会在下一个 `yield` 后退出。短任务会几乎立刻退；长循环节点应主动检查 `ctx.IsCancelled`。

## 文件结构

```
Runtime/Action/
├── IFlowNode.cs              核心接口
├── IFlowCondition.cs         条件抽象 + FlowConditionFunc
├── FlowContext.cs            上下文
├── FlowHandle.cs             运行句柄
├── FlowRunner.cs             MonoBehaviour 扩展 .Run()
├── Flow.cs                   静态入口 + FlowBuilder
├── Nodes/
│   ├── FlowSequence.cs       顺序
│   ├── FlowAction.cs         Do()
│   ├── FlowWait.cs           Wait/WaitFrames
│   ├── FlowIf.cs             If/Then/Else
│   ├── FlowSwitch.cs         Switch
│   ├── FlowLoop.cs           Repeat/While/Until
│   ├── FlowParallel.cs       ParallelAll/Any
│   ├── FlowSubFlow.cs        嵌入子 Flow
│   ├── FlowWaitSignal.cs     KSignal 等待
│   ├── FlowWaitEvent.cs      EventBus 等待
│   └── FlowWaitCondition.cs  WaitUntil/WaitWhile
└── Trigger/
    ├── KTrigger.cs           KTrigger + KTriggerBuilder
    ├── TriggerSource.cs      KSignal/EventBus/Manual 适配器
    └── TriggerManager.cs     全局注册表 + 协程驱动者
```
