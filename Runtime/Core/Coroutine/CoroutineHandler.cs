using System.Collections;
using System.Collections.Generic;

namespace Framework.Coroutine
{
    /// <summary>
    /// 协程处理器，负责管理和执行协程
    /// 你可以自己控制何时调用 Tick 方法
    /// </summary>
    public class CoroutineHandler
    {
        private List<KCoroutine> _coroutines = new List<KCoroutine>();
        private List<KCoroutine> _coroutinesToAdd = new List<KCoroutine>();
        private List<KCoroutine> _coroutinesToRemove = new List<KCoroutine>();
        private bool _isTicking = false;
        
        /// <summary>
        /// 当前活跃的协程数量
        /// </summary>
        public int ActiveCoroutineCount => _coroutines.Count;
        
        /// <summary>
        /// 启动一个协程
        /// </summary>
        /// <param name="routine">协程迭代器</param>
        /// <returns>协程实例，可用于控制（暂停/恢复/停止）</returns>
        public KCoroutine StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
                return null;
                
            var coroutine = new KCoroutine(routine);
            
            // 如果正在 Tick 过程中，延迟添加以避免修改正在遍历的集合
            if (_isTicking)
            {
                _coroutinesToAdd.Add(coroutine);
            }
            else
            {
                _coroutines.Add(coroutine);
            }
            
            return coroutine;
        }
        
        /// <summary>
        /// 停止一个协程
        /// </summary>
        /// <param name="coroutine">要停止的协程实例</param>
        public void StopCoroutine(KCoroutine coroutine)
        {
            if (coroutine == null)
                return;
                
            coroutine.Stop();
            
            // 如果正在 Tick 过程中，延迟移除
            if (_isTicking)
            {
                if (!_coroutinesToRemove.Contains(coroutine))
                {
                    _coroutinesToRemove.Add(coroutine);
                }
            }
            else
            {
                _coroutines.Remove(coroutine);
            }
        }
        
        /// <summary>
        /// 停止所有协程
        /// </summary>
        public void StopAllCoroutines()
        {
            foreach (var coroutine in _coroutines)
            {
                coroutine.Stop();
            }
            
            if (!_isTicking)
            {
                _coroutines.Clear();
            }
            else
            {
                // 如果正在 Tick，标记所有协程待移除
                foreach (var coroutine in _coroutines)
                {
                    if (!_coroutinesToRemove.Contains(coroutine))
                    {
                        _coroutinesToRemove.Add(coroutine);
                    }
                }
            }
        }
        
        /// <summary>
        /// Tick 所有协程
        /// 你可以在 Update、FixedUpdate 或任何自定义时机调用此方法
        /// </summary>
        /// <param name="deltaTime">增量时间</param>
        public void Tick(float deltaTime)
        {
            _isTicking = true;
            
            // 执行所有协程
            for (int i = _coroutines.Count - 1; i >= 0; i--)
            {
                var coroutine = _coroutines[i];
                
                // Tick 返回 false 表示协程已完成
                if (!coroutine.Tick(deltaTime))
                {
                    // 协程已完成，添加到待移除列表
                    if (!_coroutinesToRemove.Contains(coroutine))
                    {
                        _coroutinesToRemove.Add(coroutine);
                    }
                }
            }
            
            _isTicking = false;
            
            // 处理待移除的协程
            if (_coroutinesToRemove.Count > 0)
            {
                foreach (var coroutine in _coroutinesToRemove)
                {
                    _coroutines.Remove(coroutine);
                }
                _coroutinesToRemove.Clear();
            }
            
            // 处理待添加的协程
            if (_coroutinesToAdd.Count > 0)
            {
                _coroutines.AddRange(_coroutinesToAdd);
                _coroutinesToAdd.Clear();
            }
        }
        
        /// <summary>
        /// 清理所有协程
        /// </summary>
        public void Clear()
        {
            StopAllCoroutines();
            
            if (!_isTicking)
            {
                _coroutines.Clear();
                _coroutinesToAdd.Clear();
                _coroutinesToRemove.Clear();
            }
        }
        
        /// <summary>
        /// 获取所有活跃的协程（只读）
        /// </summary>
        public IReadOnlyList<KCoroutine> GetActiveCoroutines()
        {
            return _coroutines.AsReadOnly();
        }
    }
}

