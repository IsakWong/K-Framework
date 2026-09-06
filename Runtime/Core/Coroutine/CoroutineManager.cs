
using System.Collections;

namespace Framework.Coroutine
{
    /// <summary>
    /// 协程管理器（非单例版本）
    /// 提供多种 Tick 时机：Update、FixedUpdate、LateUpdate、自定义
    /// 需要外部手动调用对应的 Tick 方法来驱动协程执行
    /// </summary>
    public class CoroutineManager
    {
        // 不同时机的协程处理器
        private CoroutineHandler _updateHandler = new CoroutineHandler();
        private CoroutineHandler _fixedUpdateHandler = new CoroutineHandler();
        private CoroutineHandler _lateUpdateHandler = new CoroutineHandler();
        private CoroutineHandler _customHandler = new CoroutineHandler();
        
        /// <summary>
        /// Tick 时机枚举
        /// </summary>
        public enum TickTiming
        {
            Update,
            Logic,
            LateUpdate,
            Custom  // 需要手动调用 TickCustom
        }
        
        #region Public API
        
        /// <summary>
        /// 启动协程（默认在 Update 时 Tick）
        /// </summary>
        public KCoroutine StartCoroutine(IEnumerator routine, TickTiming timing = TickTiming.Logic)
        {
            return GetHandler(timing).StartCoroutine(routine);
        }
        
        /// <summary>
        /// 停止协程
        /// </summary>
        public void StopCoroutine(KCoroutine coroutine)
        {
            if (coroutine == null) return;
            
            _updateHandler.StopCoroutine(coroutine);
            _fixedUpdateHandler.StopCoroutine(coroutine);
            _lateUpdateHandler.StopCoroutine(coroutine);
            _customHandler.StopCoroutine(coroutine);
        }
        
        /// <summary>
        /// 停止所有协程
        /// </summary>
        public void StopAllCoroutines(TickTiming? timing = null)
        {
            if (timing.HasValue)
            {
                GetHandler(timing.Value).StopAllCoroutines();
            }
            else
            {
                _updateHandler.StopAllCoroutines();
                _fixedUpdateHandler.StopAllCoroutines();
                _lateUpdateHandler.StopAllCoroutines();
                _customHandler.StopAllCoroutines();
            }
        }
        
        /// <summary>
        /// 外部调用 - Tick Update 协程
        /// </summary>
        public void TickUpdate(float deltaTime)
        {
            _updateHandler.Tick(deltaTime);
        }
        
        /// <summary>
        /// 外部调用 - Tick FixedUpdate 协程
        /// </summary>
        public void TickFixedUpdate(float deltaTime)
        {
            _fixedUpdateHandler.Tick(deltaTime);
        }
        
        /// <summary>
        /// 外部调用 - Tick LateUpdate 协程
        /// </summary>
        public void TickLateUpdate(float deltaTime)
        {
            _lateUpdateHandler.Tick(deltaTime);
        }
        
        /// <summary>
        /// 外部调用 - Tick 自定义时机的协程
        /// </summary>
        public void TickCustom(float deltaTime)
        {
            _customHandler.Tick(deltaTime);
        }
        
        /// <summary>
        /// 清理所有协程
        /// </summary>
        public void Clear()
        {
            _updateHandler.Clear();
            _fixedUpdateHandler.Clear();
            _lateUpdateHandler.Clear();
            _customHandler.Clear();
        }
        
        /// <summary>
        /// 获取活跃协程数量
        /// </summary>
        public int GetActiveCount(TickTiming timing)
        {
            return GetHandler(timing).ActiveCoroutineCount;
        }
        
        /// <summary>
        /// 获取指定处理器
        /// </summary>
        public CoroutineHandler GetHandler(TickTiming timing)
        {
            switch (timing)
            {
                case TickTiming.Update:
                    return _updateHandler;
                case TickTiming.Logic:
                    return _fixedUpdateHandler;
                case TickTiming.LateUpdate:
                    return _lateUpdateHandler;
                case TickTiming.Custom:
                    return _customHandler;
                default:
                    return _updateHandler;
            }
        }
        
        #endregion
    }
}

