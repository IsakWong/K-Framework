# Unit 系统

游戏实体基类，提供完整的生命周期管理、组件系统和 Socket 挂点。

## 生命周期

```
None → Spawning → Alive → Dying → Dead → Deleting → Deleted
```

## 基本用法

```csharp
public class Enemy : UnitBase
{
    protected override void OnSpawn()
    {
        // 生成时初始化
        health = maxHealth;
    }

    protected override void OnLogic()
    {
        // 每 FixedUpdate 调用（仅在 Alive 状态）
        ChasePlayer();
    }

    protected override void OnDie()
    {
        // 死亡逻辑
        DropLoot();
    }
}

// 生成
unit.Spawn();

// 死亡
unit.Die();

// 删除
unit.Delete();
```

## UnitModule

全局管理器，批量处理 Unit 生命周期队列：

```csharp
// Unit 的 Spawn/Die/Delete 操作不是立即执行的
// 而是在 UnitModule 的 FixedUpdate 中批量处理
// 这避免了生命周期变更时的集合修改问题
```

## 状态检查

操作前应检查 Unit 是否存活：

```csharp
if (unit.IsAlive)
{
    unit.TakeDamage(10);
}
```

禁止直接调用 `Destroy()`，必须通过 `unit.Delete()` 走完整生命周期。

## UnitComponent

挂载在 `__Components__` 子 Transform 下的组件基类：

```csharp
public class HealthComponent : UnitComponent
{
    public int MaxHealth = 100;
    public int CurrentHealth = 100;

    // 1.7.0 新增：内置 Subscriber，End() 时自动清理
    // subscriber.Connect(...)

    // 1.7.0 新增：生命周期回调
    protected override void OnOwnerSpawn()
    {
        CurrentHealth = MaxHealth;
    }

    protected override void OnOwnerDie()
    {
        // 死亡时的清理逻辑
    }
}
```

获取组件：

```csharp
var health = unit.GetUnitComponent<HealthComponent>();
var health = unit.RequireUnitComponent<HealthComponent>(); // 不存在则创建
```

## OnModuleLogic

1.7.0 新增 — 每帧模块逻辑更新：

```csharp
protected override void OnModuleLogic(float deltaTime)
{
    // 每帧更新，仅在 Alive 状态
}
```
