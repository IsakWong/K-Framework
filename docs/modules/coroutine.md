# 协程系统

`KCoroutine` 是框架自有的协程实现，**不依赖 MonoBehaviour**，可在纯 C# 上下文中使用。

## 核心特性

- 不依赖 Unity MonoBehaviour
- 支持暂停/恢复
- 支持多种执行时机（Update、FixedUpdate、LateUpdate）
- 统一的 `CoroutineManager` 管理

## 基本用法

```csharp
// 启动协程
var coroutine = KCoroutine.Create(MyRoutine());
coroutine.Start();

// 或者通过 CoroutineManager
CoroutineManager.Instance.StartCoroutine(MyRoutine());

IEnumerator MyRoutine()
{
    yield return new WaitForSeconds(1f);
    Debug.Log("1 秒后执行");
}
```

## 控制协程

```csharp
var coroutine = KCoroutine.Create(MyRoutine());
coroutine.Start();

// 暂停
coroutine.Pause();

// 恢复
coroutine.Resume();

// 停止
coroutine.Stop();
```

## 执行时机

```csharp
// 在 Update 中驱动（默认）
var coroutine = KCoroutine.Create(MyRoutine());

// 在 FixedUpdate 中驱动
var coroutine = KCoroutine.Create(MyRoutine(), CoroutineTiming.FixedUpdate);
```

## 与 MonoBehaviour 协程的区别

| 特性 | KCoroutine | MonoBehaviour |
|------|------------|---------------|
| 依赖 | 无 | GameObject + MonoBehaviour |
| 纯 C# 使用 | ✅ | ❌ |
| 暂停/恢复 | ✅ | ❌ |
| 多时机支持 | ✅ | ❌ |
| WaitForSeconds | ✅ | ✅ |
