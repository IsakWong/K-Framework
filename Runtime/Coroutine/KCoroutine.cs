using System.Collections;
using System.Collections.Generic;

namespace Framework.Coroutine
{
    /// <summary>
    /// 协程实例，可以控制暂停、恢复、停止
    /// </summary>
    public class KCoroutine
    {
        private IEnumerator _routine;
        private Stack<IEnumerator> _enumeratorStack = new Stack<IEnumerator>();
        private bool _isPaused;
        private bool _isStopped;
        private object _current;
        
        public bool IsRunning => !_isStopped && _routine != null;
        public bool IsPaused => _isPaused;
        public bool IsDone => _isStopped;
        public object Current => _current;
        
        internal KCoroutine(IEnumerator routine)
        {
            _routine = routine;
            _enumeratorStack.Push(routine);
        }
        
        /// <summary>
        /// Tick 协程，返回是否继续执行
        /// </summary>
        internal bool Tick(float deltaTime)
        {
            if (_isStopped || _isPaused)
                return !_isStopped;
            
            // 处理当前的 yield 对象
            if (_current != null)
            {
                if (_current is WaitSeconds waitForSeconds)
                {
                    if (!waitForSeconds.Tick(deltaTime))
                        return true; // 还在等待
                    _current = null;
                }
                else if (_current is WaitForSecondsRealtime waitRealtime)
                {
                    if (!waitRealtime.Tick(deltaTime))
                        return true;
                    _current = null;
                }
                else if (_current is WaitUntil waitUntil)
                {
                    if (!waitUntil.Tick())
                        return true;
                    _current = null;
                }
                else if (_current is WaitWhile waitWhile)
                {
                    if (!waitWhile.Tick())
                        return true;
                    _current = null;
                }
                else if (_current is WaitForNextFrame)
                {
                    // WaitForNextFrame 只等待一帧
                    _current = null;
                }
                else if (_current is KCoroutine nestedCoroutine)
                {
                    if (!nestedCoroutine.IsDone)
                        return true; // 嵌套协程还在执行
                    _current = null;
                }
            }
            
            // 执行协程栈中的当前层
            while (_enumeratorStack.Count > 0)
            {
                var currentEnumerator = _enumeratorStack.Peek();
                
                if (currentEnumerator.MoveNext())
                {
                    _current = currentEnumerator.Current;
                    
                    // 如果返回的是嵌套的 IEnumerator，压入栈中
                    if (_current is IEnumerator nested)
                    {
                        _enumeratorStack.Push(nested);
                        // 继续处理嵌套的IEnumerator（递归）
                        continue;
                    }
                    
                    // 如果是其他类型（Wait对象等），返回并等待下次Tick
                    return true;
                }
                else
                {
                    // 当前层执行完毕，弹出栈
                    _enumeratorStack.Pop();
                    _current = null;
                    
                    // 如果还有父层，继续执行父层
                    // 如果栈空了，while会退出，下面会标记为完成
                }
            }
            
            // 协程执行完毕
            _isStopped = true;
            return false;
        }
        
        /// <summary>
        /// 暂停协程
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
        }
        
        /// <summary>
        /// 恢复协程
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }
        
        /// <summary>
        /// 停止协程
        /// </summary>
        public void Stop()
        {
            _isStopped = true;
            _routine = null;
            _enumeratorStack.Clear();
            _current = null;
        }
    }
}

