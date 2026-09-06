using System.Collections;
using UnityEngine;

namespace Framework.Coroutine.Examples
{
    /// <summary>
    /// KCoroutine 和 CoroutineManager 测试
    /// 测试WaitSeconds、WaitUntil、WaitWhile、嵌套协程、可控协程等
    /// </summary>
    public class CoroutineTest : MonoBehaviour
    {
        [Header("协程管理器")]
        private CoroutineManager _coroutineManager = new CoroutineManager();
        
        [Header("测试开关")]
        [SerializeField] private bool testBasic = true;
        [SerializeField] private bool testWaitSeconds = true;
        [SerializeField] private bool testNestedCoroutines = true;
        [SerializeField] private bool testWaitConditions = true;
        [SerializeField] private bool testDifferentTiming = true;
        [SerializeField] private bool testControllable = true;
        
        [Header("可控协程")]
        private KCoroutine _controllableCoroutine;
        
        [Header("自定义Tick")]
        [SerializeField] private bool useCustomTick = false;
        [SerializeField] private float customTimeScale = 1f;
        
        private void Start()
        {
            Debug.Log("========== KCoroutine 测试开始 ==========");
            
            // 测试 1: 基础协程
            if (testBasic)
            {
                _coroutineManager.StartCoroutine(BasicCoroutineTest());
            }
            
            // 测试 2: WaitSeconds 和 WaitForSecondsRealtime
            if (testWaitSeconds)
            {
                _coroutineManager.StartCoroutine(WaitSecondsTest());
            }
            
            // 测试 3: 嵌套协程
            if (testNestedCoroutines)
            {
                _coroutineManager.StartCoroutine(NestedCoroutineTest());
            }
            
            // 测试 4: WaitUntil 和 WaitWhile
            if (testWaitConditions)
            {
                _coroutineManager.StartCoroutine(WaitConditionsTest());
            }
            
            // 测试 5: 不同的Tick时机
            if (testDifferentTiming)
            {
                DifferentTimingTest();
            }
            
            // 测试 6: 可控协程
            if (testControllable)
            {
                _controllableCoroutine = _coroutineManager.StartCoroutine(ControllableCoroutineTest());
            }
        }
        
        private void Update()
        {
            // 驱动协程执行
            _coroutineManager.TickUpdate(Time.deltaTime);
            
            // 测试控制协程
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_controllableCoroutine != null)
                {
                    if (_controllableCoroutine.IsPaused)
                    {
                        _controllableCoroutine.Resume();
                        Debug.Log("[控制] 协程已恢复");
                    }
                    else
                    {
                        _controllableCoroutine.Pause();
                        Debug.Log("[控制] 协程已暂停");
                    }
                }
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (_controllableCoroutine != null)
                {
                    _controllableCoroutine.Stop();
                    Debug.Log("[控制] 协程已停止");
                    _controllableCoroutine = null;
                }
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_controllableCoroutine == null || _controllableCoroutine.IsDone)
                {
                    _controllableCoroutine = _coroutineManager.StartCoroutine(ControllableCoroutineTest());
                    Debug.Log("[控制] 协程已重新启动");
                }
            }
        }
        
        private void FixedUpdate()
        {
            _coroutineManager.TickFixedUpdate(Time.fixedDeltaTime);
        }
        
        private void LateUpdate()
        {
            _coroutineManager.TickLateUpdate(Time.deltaTime);
            
            if (useCustomTick)
            {
                float customDelta = Time.deltaTime * customTimeScale;
                _coroutineManager.TickCustom(customDelta);
            }
        }
        
        private void OnDestroy()
        {
            // 清理所有协程
            _coroutineManager.Clear();
        }
        
        #region 测试 1: 基础协程
        
        /// <summary>
        /// 基础协程测试：演示KCoroutine的基本用法
        /// </summary>
        private IEnumerator BasicCoroutineTest()
        {
            Debug.Log("[基础测试] 协程开始");
            
            // WaitForNextFrame: 等待下一帧
            yield return new WaitForNextFrame();
            Debug.Log("[基础测试] 等待一帧后");
            
            // 连续等待多帧
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForNextFrame();
                Debug.Log($"[基础测试] 第 {i + 1} 帧");
            }
            
            Debug.Log("[基础测试] 协程结束");
        }
        
        #endregion
        
        #region 测试 2: WaitSeconds 和 WaitForSecondsRealtime
        
        /// <summary>
        /// WaitSeconds测试：演示时间等待
        /// </summary>
        private IEnumerator WaitSecondsTest()
        {
            Debug.Log("[WaitSeconds测试] 开始");
            float startTime = Time.time;
            
            // 等待1秒 (使用KCoroutine的WaitSeconds)
            Debug.Log($"[WaitSeconds测试] 等待1秒... Time: {Time.time:F2}");
            yield return new WaitSeconds(1f);
            Debug.Log($"[WaitSeconds测试] 1秒后 Time: {Time.time:F2}, 实际经过: {Time.time - startTime:F2}秒");
            
            // 等待2秒
            startTime = Time.time;
            Debug.Log($"[WaitSeconds测试] 等待2秒... Time: {Time.time:F2}");
            yield return new WaitSeconds(2f);
            Debug.Log($"[WaitSeconds测试] 2秒后 Time: {Time.time:F2}, 实际经过: {Time.time - startTime:F2}秒");
            
            // WaitForSecondsRealtime (不受Time.timeScale影响)
            startTime = Time.realtimeSinceStartup;
            Debug.Log($"[WaitSeconds测试] 使用WaitForSecondsRealtime等待1秒...");
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log($"[WaitSeconds测试] 真实时间1秒后, 实际经过: {Time.realtimeSinceStartup - startTime:F2}秒");
            
            Debug.Log("[WaitSeconds测试] 结束");
        }
        
        #endregion
        
        #region 测试 3: 嵌套协程
        
        /// <summary>
        /// 嵌套协程测试：演示yield return 嵌套协程的正确用法
        /// </summary>
        private IEnumerator NestedCoroutineTest()
        {
            Debug.Log("[嵌套协程测试] 父协程开始");
            
            // 正确方式: yield return 子协程（会等待）
            Debug.Log("[嵌套协程测试] 调用子协程1 (yield return)");
            yield return ChildCoroutine("子协程1", 1f);
            Debug.Log("[嵌套协程测试] 子协程1已完成 ✓");
            
            // 再次测试
            Debug.Log("[嵌套协程测试] 调用子协程2 (yield return)");
            yield return ChildCoroutine("子协程2", 0.5f);
            Debug.Log("[嵌套协程测试] 子协程2已完成 ✓");
            
            // 不等待的方式（用StartCoroutine）
            Debug.Log("[嵌套协程测试] 启动不等待的子协程3");
            _coroutineManager.StartCoroutine(ChildCoroutine("子协程3-不等待", 2f));
            Debug.Log("[嵌套协程测试] 立即继续执行，没有等待子协程3 ✗");
            
            yield return new WaitSeconds(0.5f);
            
            // 并行执行多个协程
            Debug.Log("[嵌套协程测试] 并行启动多个协程");
            _coroutineManager.StartCoroutine(ChildCoroutine("并行1", 1f));
            _coroutineManager.StartCoroutine(ChildCoroutine("并行2", 1.5f));
            _coroutineManager.StartCoroutine(ChildCoroutine("并行3", 0.5f));
            
            yield return new WaitSeconds(2f);
            Debug.Log("[嵌套协程测试] 父协程结束");
        }
        
        /// <summary>
        /// 子协程 - 第一层嵌套
        /// </summary>
        private IEnumerator ChildCoroutine(string name, float waitTime)
        {
            Debug.Log($"  [{name}] ★ 开始，将等待 {waitTime} 秒");
            Debug.Log($"  [{name}] → 调用嵌套协程 ChildCoroutine2");
            yield return ChildCoroutine2("Child of " + name, waitTime / 2f);
            Debug.Log($"  [{name}] ← ChildCoroutine2 已完成");
            Debug.Log($"  [{name}] → 等待 {waitTime / 2f} 秒");
            yield return new WaitSeconds(waitTime / 2f);
            Debug.Log($"  [{name}] ★ 结束");
        }

        /// <summary>
        /// 子协程 - 第二层嵌套
        /// </summary>
        private IEnumerator ChildCoroutine2(string name, float waitTime)
        {
            Debug.Log($"    [{name}] ★★ 开始，将等待 {waitTime} 秒");
            Debug.Log($"    [{name}] → 等待 {waitTime} 秒");
            yield return new WaitSeconds(waitTime);
            Debug.Log($"    [{name}] → yield return null");
            yield return null;
            Debug.Log($"    [{name}] ★★ 结束");
        }

        #endregion
        
        #region 测试 4: WaitUntil 和 WaitWhile
        
        /// <summary>
        /// 条件等待测试
        /// </summary>
        private IEnumerator WaitConditionsTest()
        {
            Debug.Log("[条件等待测试] 开始");
            
            // WaitUntil: 等待条件为true
            float startTime = Time.time;
            Debug.Log("[条件等待测试] WaitUntil - 等待Time.time > 开始时间+1秒");
            yield return new WaitUntil(() => Time.time > startTime + 1f);
            Debug.Log($"[条件等待测试] WaitUntil完成, 经过 {Time.time - startTime:F2} 秒");
            
            // WaitWhile: 等待条件为false
            bool isWaiting = true;
            Debug.Log("[条件等待测试] WaitWhile - 等待isWaiting变为false");
            _coroutineManager.StartCoroutine(SetFalseAfterDelay(1f, () => isWaiting = false));
            yield return new WaitWhile(() => isWaiting);
            Debug.Log("[条件等待测试] WaitWhile完成");
            
            Debug.Log("[条件等待测试] 结束");
        }
        
        /// <summary>
        /// 延迟后执行回调
        /// </summary>
        private IEnumerator SetFalseAfterDelay(float delay, System.Action callback)
        {
            yield return new WaitSeconds(delay);
            callback?.Invoke();
            Debug.Log("  [SetFalseAfterDelay] 回调已执行");
        }
        
        #endregion
        
        #region 测试 5: 不同的Tick时机
        
        /// <summary>
        /// 不同Tick时机测试
        /// </summary>
        private void DifferentTimingTest()
        {
            Debug.Log("[不同Tick时机测试] 开始");
            
            // Update 时机（默认）
            _coroutineManager.StartCoroutine(
                TimingCoroutine("Update", 3),
                CoroutineManager.TickTiming.Update
            );
            
            // Logic 时机（FixedUpdate）
            _coroutineManager.StartCoroutine(
                TimingCoroutine("Logic/FixedUpdate", 3),
                CoroutineManager.TickTiming.Logic
            );
            
            // LateUpdate 时机
            _coroutineManager.StartCoroutine(
                TimingCoroutine("LateUpdate", 3),
                CoroutineManager.TickTiming.LateUpdate
            );
            
            // 自定义 时机
            if (useCustomTick)
            {
                _coroutineManager.StartCoroutine(
                    TimingCoroutine("Custom", 3),
                    CoroutineManager.TickTiming.Custom
                );
            }
        }
        
        /// <summary>
        /// 计时协程
        /// </summary>
        private IEnumerator TimingCoroutine(string timingName, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Debug.Log($"[{timingName}] 计数: {i}, Time: {Time.time:F2}");
                yield return new WaitSeconds(1f);
            }
            Debug.Log($"[{timingName}] 结束");
        }
        
        #endregion
        
        #region 测试 6: 可控协程
        
        /// <summary>
        /// 可控协程：演示暂停/恢复/停止功能
        /// 按P键切换暂停，按S键停止，按R键重启
        /// </summary>
        private IEnumerator ControllableCoroutineTest()
        {
            Debug.Log("[可控协程] 开始 (按P切换暂停, S停止, R重启)");
            Debug.Log("[可控协程] 状态查询 - IsPaused: False, IsStopped: False");
            
            for (int i = 0; i < 20; i++)
            {
                Debug.Log($"[可控协程] 计数: {i}");
                yield return new WaitSeconds(1f);
            }
            
            Debug.Log("[可控协程] 正常结束");
            _controllableCoroutine = null;
        }
        
        #endregion
        
        #region 测试 7: yield break 提前退出
        
        /// <summary>
        /// 提前退出的协程
        /// </summary>
        private IEnumerator EarlyExitCoroutine()
        {
            Debug.Log("  [提前退出协程] 开始");
            yield return new WaitSeconds(0.5f);
            
            Debug.Log("  [提前退出协程] 使用 yield break 提前退出");
            yield break; // 提前终止协程
            
            // 下面的代码不会执行
            Debug.Log("  [提前退出协程] 这行不会被执行");
        }
        
        #endregion
    }
}

