# 状态机

有限状态机实现，支持层级状态和条件转换。

## 基本用法

```csharp
public enum CharacterState { Idle, Running, Jumping, Attacking }

var fsm = new StateMachine<CharacterState>();

fsm.AddState(CharacterState.Idle)
   .AddTransition(CharacterState.Running, () => input.Move != Vector3.zero)
   .AddTransition(CharacterState.Jumping, () => input.Jump)
   .AddTransition(CharacterState.Attacking, () => input.Attack);

fsm.AddState(CharacterState.Running)
   .AddTransition(CharacterState.Idle, () => input.Move == Vector3.zero)
   .AddTransition(CharacterState.Jumping, () => input.Jump);

// 设置状态变化回调
fsm.StateChanged += (from, to) => Debug.Log($"{from} → {to}");

// 启动
fsm.Start(CharacterState.Idle);

// 每帧更新
void Update()
{
    fsm.OnLogic();
}
```

## HybridStateMachine

混合状态机结合了 FSM 和行为树的设计：

```csharp
var hfsm = new HybridStateMachine<CharacterState>();

// 状态可包含子状态机
// 支持更复杂的层级转换逻辑
```

## 条件转换

状态转换由条件驱动，条件在 `OnLogic()` 中按注册顺序评估：

```csharp
.AddTransition(
    targetState,
    condition: () => somePredicate,
    priority: 0    // 多个条件同时满足时，选优先级最高的
)
```

## 状态回调

```csharp
fsm.AddState(CharacterState.Attacking)
   .OnEnter(() => animator.Play("Attack"))
   .OnExit(() => animator.ResetTrigger("Attack"))
   .OnLogic(() => { /* 每帧逻辑 */ });
```
