using System;

namespace Framework.Coroutine
{
    /// <summary>
    /// 等待指定秒数（使用 deltaTime）
    /// </summary>
    public class WaitSeconds
    {
        private float _duration;
        private float _elapsed;
        
        public WaitSeconds(float seconds)
        {
            _duration = seconds;
            _elapsed = 0f;
        }
        
        internal bool Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            return _elapsed >= _duration;
        }
    }
    
    /// <summary>
    /// 等待指定秒数（使用真实时间，不受 TimeScale 影响）
    /// </summary>
    public class WaitForSecondsRealtime
    {
        private float _duration;
        private float _elapsed;
        
        public WaitForSecondsRealtime(float seconds)
        {
            _duration = seconds;
            _elapsed = 0f;
        }
        
        internal bool Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            return _elapsed >= _duration;
        }
    }
    
    /// <summary>
    /// 等待直到条件为真
    /// </summary>
    public class WaitUntil
    {
        private Func<bool> _predicate;
        
        public WaitUntil(Func<bool> predicate)
        {
            _predicate = predicate;
        }
        
        internal bool Tick()
        {
            return _predicate?.Invoke() ?? true;
        }
    }
    
    /// <summary>
    /// 等待直到条件为假
    /// </summary>
    public class WaitWhile
    {
        private Func<bool> _predicate;
        
        public WaitWhile(Func<bool> predicate)
        {
            _predicate = predicate;
        }
        
        internal bool Tick()
        {
            return !(_predicate?.Invoke() ?? false);
        }
    }
    
    /// <summary>
    /// 等待一帧（在下次 Tick 时继续）
    /// </summary>
    public class WaitForNextFrame
    {
        // 这个类仅作为标记，在 Coroutine.Tick 中会被识别并在下一帧继续
    }
}

