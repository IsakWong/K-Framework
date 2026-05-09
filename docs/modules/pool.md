# 对象池

K-Framework 提供三层对象池：`GameObjectPool`（单个 Prefab 池）、`PoolManager`（多 Prefab 注册中心）和 `CSharpPool<T>`（纯 C# 泛型池）。

## 池层级

| 层级 | 类型 | 用途 |
|------|------|------|
| PoolManager | `IPoolService` | 多 Prefab 注册中心 + 实例跟踪 |
| GameObjectPool | — | 单个 Prefab 的对象池（GameObject） |
| CSharpPool\<T\> | — | 纯 C# 泛型对象池 |

## PoolManager

通过服务接口访问：

```csharp
var pool = ServiceLocator.Get<IPoolService>();

// 获取实例
var instance = pool.Get(prefab, position, rotation);

// 获取带类型的实例
var instance = pool.Get<MyComponent>(prefab, position, rotation);

// 释放
pool.Release(instance);

// 预加载
pool.Preload(prefab, count: 10);

// 查询
bool isPooled = pool.IsPooled(instance);

// 清空
pool.ClearAll();
```

## IPoolable

实现 `IPoolable` 接口接收池回调：

```csharp
public class PooledVfx : MonoBehaviour, IPoolable
{
    public void OnSpawned()
    {
        // 从池中取出时调用
        particleSystem.Play();
    }

    public void OnDespawned()
    {
        // 回池时调用
        particleSystem.Stop();
    }
}
```

## CSharpPool\<T\>

纯 C# 泛型池，不涉及 GameObject：

```csharp
var pool = new CSharpPool<MyClass>(
    createFunc: () => new MyClass(),
    onGet: obj => obj.Reset(),
    onRelease: obj => obj.Cleanup()
);

var obj = pool.Get();
pool.Release(obj);
```

## 集合池

框架内置了常用集合的池化版本：

```csharp
using (var list = ListPool<int>.Get())
{
    list.Add(1);
    list.Add(2);
    // 自动回池
}

using (var dict = DictionaryPool<string, int>.Get())
{
    dict["key"] = 42;
    // 自动回池
}
```

## UnitBase 集成

`UnitBase` 通过 `CanRecycle` 支持 opt-in 对象池回收：

```csharp
public class MyUnit : UnitBase
{
    public override bool CanRecycle => true; // 允许回池
}
```
