# K-Framework 测试文档

## 概览

K-Framework 测试套件基于 **Unity Test Framework**（UTF），遵循 Unity 官方推荐的测试分层策略：

- **EditMode 测试**（`[TestFixture]` + `[Test]`）：纯 C# 逻辑测试，不依赖 PlayMode，运行速度快，适合 CI
- **PlayMode 测试**（`[UnityTest]` + `IEnumerator`）：依赖场景/GameObject/MonoBehaviour，验证运行时行为

测试文件位于 `Tests/Runtime/`，通过 `KFramework.Tests.asmdef` 程序集引用 `KFramework` 和 `nunit.framework.dll`。

---

## 测试架构约定

### 命名规范
```
{被测类名}Tests.cs
```

### 结构规范
```csharp
[TestFixture]
public class XxxTests
{
    [SetUp]    public void Setup()    { /* 每个 Test 前执行 */ }
    [TearDown] public void TearDown() { /* 每个 Test 后执行 */ }

    [Test]
    public void MethodName_Scenario_ExpectedBehavior() { }

    [UnityTest]
    public IEnumerator AsyncScenario_ExpectedBehavior() { }
}
```

### 断言风格
使用 NUnit 断言（`Assert.AreEqual`、`Assert.IsTrue`、`Assert.Throws<T>` 等），禁止 `Debug.Assert`。

### 测试隔离
- 静态状态（ServiceLocator、EventBus）在 `[SetUp]`/`[TearDown]` 中 Reset/Clear
- 单例（KSingleton）因不可重置，避免跨测试污染，单独成 fixture

---

## 测试覆盖清单

### Foundation 层

| 系统 | 文件 | 覆盖内容 | 类型 |
|------|------|----------|------|
| **KSignal** | `SignalTests.cs` | Invoke 0~4 参数、Connect/Disconnect by handle & delegate、ConnectOnce 一次性、Subscriber 批量管理、operator +/-、空订阅安全 | EditMode |
| **Subscriber** | `SignalTests.cs` | DisconnectAll、Dispose、多信号管理、EventBus 联动 | EditMode |
| **ServiceLocator** | `ServiceLocatorTests.cs` | Register 泛型/Type、覆盖策略、Get/TryGet/GetOrDefault、IsRegistered、Unregister、Reset、Count | EditMode |
| **KTimer** | `TimerTests.cs` | Start/Pause/Stop/Reset 状态机、OnTimeout 回调、Loops（单次/多次/无限-1）、OnFinish、Progress/RemainingTime/ElapsedTime、Interval 运行时修改 | EditMode |
| **KTimerManager** | `TimerTests.cs` | AddTimer、RemoveTimer、StopAllTimer、自动移除已完成 Timer、批量 OnLogic | EditMode |
| **CSharpPool** | `ObjectPoolTests.cs` | Get/Release 复用、onGet/onRelease/onDestroy 回调、MaxSize 超限销毁、Preload/Clear、null factory 异常 | EditMode |
| **ListPool / DictionaryPool / HashSetPool** | `ObjectPoolTests.cs` | Get 返回空集合、Release 清空并复用 | EditMode |
| **Variant** | `VariantTests.cs` | 构造（int/float/bool/string/Vector2/3/4/double）、Set/Get 类型切换、GetRaw、Copy 构造、SetVariant、VariantTypeHelper、VariantRef 隐式转换 | EditMode |
| **MathExtension** | `MathExtensionTests.cs` | NormalizeAngle、GetAngleInXZ、ClampAngle（含跨0度）、QuadraticBezierPoint、Bezier、CaclulateAcc/CalculateSpeed、RotateDirectionY、IsInSection、projectedOnPlane | EditMode |
| **Utility** | `UtilityTests.cs` | Vector 转换（ToVector2/ToVector3/ToVector3Int...）、VectorInt 转换、FloorToInt/CeilToInt、DistanceBetweenPosition、GetRandomElements、SafeAccess、NormalizedInXY、RectInt.Outter | EditMode |
| **DoubleArray\<T\>** | `UtilityTests.cs` | 构造/索引/GetLength/Rank、IsValidIndex、Clear/Fill、边界异常 | EditMode |

### Core 层

| 系统 | 文件 | 覆盖内容 | 类型 |
|------|------|----------|------|
| **EventBus** | `EventBusTests.cs` | Subscribe/Publish/Unsubscribe、SubscribeOnce、优先级排序验证、粘性事件（PublishSticky/SubscribeSticky/TryGetSticky/RemoveSticky）、Subscriber 联动、UnsubscribeAll、Clear、GetSubscriberCount/HasSubscribers、异常处理器隔离 | EditMode |
| **FSM StateMachine** | `StateMachineTests.cs` | Init/StartState 自动设置、OnEnter/OnExit/OnLogic 生命周期、Transition 条件转换、TransitionFromAny、Trigger 事件驱动、TwoWay 双向转换、needsExitTime + canExit/forceInstantly、层次化子状态机、GetState/GetStates、RequestStateChange、OnExit 清理 | EditMode |
| **Command / CommandQueue** | `CommandTests.cs` | Push/PushCmd、ProcessOnce（Success/Continue/Fail 行为差异）、ProcessUntilEmpty、混合结果队列、Priority 默认值 | EditMode |
| **KCoroutine** | `KCoroutineTests.cs` | 基础执行、WaitSeconds/WaitForNextFrame/WaitUntil/WaitWhile、嵌套/深层嵌套协程、Pause/Resume/Stop 状态控制、yield break 提前退出 | EditMode |
| **CoroutineHandler** | `KCoroutineTests.cs` | StartCoroutine/Clear/StopAllCoroutines/GetActiveCoroutines、自动移除已完成协程 | EditMode |
| **CoroutineManager** | `KCoroutineTests.cs` | 默认TickTiming、多Timing独立运行、Clear 全量清理 | EditMode |

### Action 层

| 系统 | 文件 | 覆盖内容 | 类型 |
|------|------|----------|------|
| **Flow** | `FlowTests.cs` | 顺序执行、Wait 精度、If/Then/Else 路由、Repeat(N)、While 循环、ParallelAll/ParallelAny 并行、WaitFor(KSignal)、WaitForEvent\<T\>、Cancel 取消、异常处理（IsFailed/Error）、SubFlow 嵌套、FlowContext 传值 | PlayMode |
| **KTrigger** | `TriggerTests.cs` | Once 自动 Unregister、Repeating 多次触发、Condition 过滤、KSignal 源、EventBus 源、EventBus filter、Manual Fire、Action as SubFlow 异步完成 | PlayMode |

---

## 尚未覆盖的系统

以下系统因依赖 Unity 运行时（GameObject/MonoBehaviour/场景/Addressables）暂未纳入纯 C# EditMode 测试，建议在后续迭代中添加 PlayMode 测试：

| 系统 | 原因 | 建议 |
|------|------|------|
| `KGameCore` | 依赖 GameObject/MonoBehaviour 创建 Module | PlayMode 集成测试 |
| `GameMode` | 场景生命周期 | PlayMode + 场景测试 |
| `TModule<T>` | Awake 自动注册 | PlayMode |
| `PersistentSingleton<T>` | DontDestroyOnLoad | PlayMode |
| `GameObjectPool / PoolManager` | 依赖 Instantiate/Destroy | PlayMode |
| `UIManager / UIPanel` | 需要 Canvas/场景 | PlayMode |
| `SoundManager` | 需要 AudioSource/Mixer | PlayMode |
| `AssetManager` | 依赖 Addressables | PlayMode |
| `ConfigManager` | 需要 ScriptableObject 资源 | PlayMode |
| `KCoroutine / CoroutineManager` | ✅ 已有 EditMode 测试 (KCoroutineTests.cs) | - |
| `BehaviorTree` | 复杂决策树系统 | 需专项测试设计 |
| `UnitBase / UnitModule` | 依赖 GameObject/场景 | PlayMode |
| `KConstraint` | 依赖 Transform | PlayMode |
| `EnhancedLog` | 日志系统 | 可做 EditMode 输出验证 |

---

## 运行测试

### Unity Editor
1. 打开 **Window → General → Test Runner**
2. 选择 **EditMode** 标签，点击 **Run All**（纯 C# 测试）
3. 选择 **PlayMode** 标签，点击 **Run All**（运行时测试）

### 命令行（CI）
```bash
# EditMode
unity -runTests -testPlatform EditMode -projectPath . -testResults editmode-results.xml

# PlayMode
unity -runTests -testPlatform PlayMode -projectPath . -testResults playmode-results.xml
```

---

## 添加新测试的 Checklist

1. [ ] 在 `Tests/Runtime/` 下创建 `{ClassName}Tests.cs`
2. [ ] 使用 `[TestFixture]`（EditMode）或 `[UnityTest]`（PlayMode）
3. [ ] 添加 `[SetUp]`/`[TearDown]` 确保测试隔离
4. [ ] 使用 NUnit 断言，覆盖正常路径 + 边界 + 异常
5. [ ] 创建对应的 `.meta` 文件（GUID 唯一）
6. [ ] 确保 `KFramework.Tests.asmdef` 引用了必要的程序集
7. [ ] 在 Test Runner 中验证通过
