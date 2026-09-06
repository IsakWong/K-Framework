using System.Collections;
using UnityEngine;

namespace Framework.Coroutine.Examples
{
    /// <summary>
    /// 协程系统使用示例
    /// </summary>
    public class CoroutineExample : MonoBehaviour
    {
        private CoroutineManager _coroutineManager = new CoroutineManager();
        private KCoroutine _controllableCoroutine;
        
        [Header("自定义 Tick 示例")]
        [SerializeField] private bool useCustomTick = false;
        [SerializeField] private float customTimeScale = 1f;
        
        private void Start()
        {
            // 示例 1: 基础用法
            BasicExample();
            
            // 示例 2: 不同 Tick 时机
            DifferentTimingExample();
            
            // 示例 3: 可控协程
            _controllableCoroutine = ControllableCoroutineExample();
        }
        
        private void Update()
        {
            // 驱动协程执行
            _coroutineManager.TickUpdate(Time.deltaTime);
            
            // 测试控制协程
            if (Input.GetKeyDown(KeyCode.P))
            {
                _controllableCoroutine?.Pause();
                Debug.Log("协程已暂停");
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                _controllableCoroutine?.Resume();
                Debug.Log("协程已恢复");
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                _controllableCoroutine?.Stop();
                Debug.Log("协程已停止");
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
        
        #region Example 1: Basic Usage
        
        private void BasicExample()
        {
            _coroutineManager.StartCoroutine(BasicCoroutine());
        }
        
        private IEnumerator BasicCoroutine()
        {
            Debug.Log("基础协程开始");
            
            // 等待 1 秒
            yield return new WaitSeconds(1f);
            Debug.Log("1 秒后");
            
            // 等待 2 秒
            yield return new WaitSeconds(2f);
            Debug.Log("又过了 2 秒");
            
            // 等待条件满足
            yield return new WaitUntil(() => Time.time > 5f);
            Debug.Log("时间超过 5 秒了");
            
            Debug.Log("基础协程结束");
        }
        
        #endregion
        
        #region Example 2: Different Timing
        
        private void DifferentTimingExample()
        {
            // Update 时机（默认）
            _coroutineManager.StartCoroutine(
                LogCoroutine("Update", 1f),
                CoroutineManager.TickTiming.Update
            );
            
            // FixedUpdate 时机（物理更新）
            _coroutineManager.StartCoroutine(
                LogCoroutine("FixedUpdate", 1f),
                CoroutineManager.TickTiming.Logic
            );
            
            // LateUpdate 时机（晚于 Update）
            _coroutineManager.StartCoroutine(
                LogCoroutine("LateUpdate", 1f),
                CoroutineManager.TickTiming.LateUpdate
            );
        }
        
        private IEnumerator LogCoroutine(string name, float interval)
        {
            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"[{name}] Count: {i}");
                yield return new WaitSeconds(interval);
            }
        }
        
        #endregion
        
        #region Example 3: Controllable Coroutine
        
        private KCoroutine ControllableCoroutineExample()
        {
            return _coroutineManager.StartCoroutine(ControllableCoroutine());
        }
        
        private IEnumerator ControllableCoroutine()
        {
            Debug.Log("可控协程开始 (按 P 暂停, R 恢复, S 停止)");
            
            for (int i = 0; i < 20; i++)
            {
                Debug.Log($"可控协程计数: {i}");
                yield return new WaitSeconds(1f);
            }
            
            Debug.Log("可控协程结束");
        }
        
        #endregion
        
        #region Example 4: Nested Coroutines
        
        private void NestedCoroutineExample()
        {
            _coroutineManager.StartCoroutine(ParentCoroutine());
        }
        
        private IEnumerator ParentCoroutine()
        {
            Debug.Log("父协程开始");
            
            // 嵌套执行子协程
            yield return ChildCoroutine(1);
            Debug.Log("子协程 1 完成");
            
            yield return ChildCoroutine(2);
            Debug.Log("子协程 2 完成");
            
            Debug.Log("父协程结束");
        }
        
        private IEnumerator ChildCoroutine(int id)
        {
            Debug.Log($"子协程 {id} 开始");
            yield return new WaitSeconds(1f);
            Debug.Log($"子协程 {id} 结束");
        }
        
        #endregion
        
        #region Example 5: Custom Timing
        
        private void CustomTimingExample()
        {
            _coroutineManager.StartCoroutine(
                CustomCoroutine(),
                CoroutineManager.TickTiming.Custom
            );
        }
        
        private IEnumerator CustomCoroutine()
        {
            Debug.Log("自定义时机协程开始");
            
            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"自定义 Tick: {i}");
                yield return new WaitSeconds(1f);
            }
            
            Debug.Log("自定义时机协程结束");
        }
        
        #endregion
        
        #region Example 6: Wait Conditions
        
        private void WaitConditionsExample()
        {
            _coroutineManager.StartCoroutine(WaitConditionsCoroutine());
        }
        
        private IEnumerator WaitConditionsCoroutine()
        {
            Debug.Log("等待条件示例开始");
            
            // WaitForSeconds
            yield return new WaitSeconds(2f);
            Debug.Log("等待 2 秒完成");
            
            // WaitForSecondsRealtime (不受 Time.timeScale 影响)
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log("等待真实时间 1 秒完成");
            
            // WaitUntil (等待条件为真)
            float startTime = Time.time;
            yield return new WaitUntil(() => Time.time - startTime > 1f);
            Debug.Log("WaitUntil 完成");
            
            // WaitWhile (等待条件为假)
            bool waiting = true;
            StartCoroutine(SetFalseAfterDelay(() => waiting = false, 1f));
            yield return new WaitWhile(() => waiting);
            Debug.Log("WaitWhile 完成");
            
            Debug.Log("等待条件示例结束");
        }
        
        private IEnumerator SetFalseAfterDelay(System.Action action, float delay)
        {
            yield return new WaitSeconds(delay);
            action?.Invoke();
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // 清理所有协程
            _coroutineManager.Clear();
        }
    }
}

