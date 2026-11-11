using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace KFramework
{
    /// <summary>状态处理器：进入/退出。缺失的处理器按空实现处理。</summary>
    public interface IKState<TState> where TState : struct, Enum
    {
        UniTask EnterAsync(CancellationToken ct);
        UniTask ExitAsync(CancellationToken ct);
    }

    /// <summary>
    /// 纯 C# 显式状态机：单一当前状态、串行转场、状态变更信号。
    /// 不负责场景加载——那属于状态处理器内部的业务动作。
    /// </summary>
    public sealed class KStateMachine<TState> where TState : struct, Enum
    {
        private readonly Dictionary<TState, IKState<TState>> _states = new();
        private TState? _pendingNext;

        public TState State { get; private set; }

        public TState? LastState { get; private set; }

        public bool IsTransitioning { get; private set; }

        public KSignal<TState, TState> OnStateChanged { get; } = new();

        public void Register(TState state, IKState<TState> handler)
        {
            if (handler == null)
                _states.Remove(state);
            else
                _states[state] = handler;
        }

        /// <summary>设置初始状态，不触发 Enter 与状态变更信号。</summary>
        public void SetInitial(TState state)
        {
            State = state;
            LastState = null;
        }

        /// <summary>
        /// 在 EnterAsync 内请求紧接着切到下一状态（如 Loading 完成后进入目标状态）。
        /// 当前转场收尾后会自动继续，避免重入本状态机。
        /// </summary>
        public void RequestNext(TState target) => _pendingNext = target;

        /// <summary>
        /// 串行转场：Exit 旧状态 → Enter 新状态 → 广播。转场中或目标相同返回 false。
        /// 状态在 Enter 中调用 <see cref="RequestNext"/> 时会链式继续。
        /// </summary>
        public async UniTask<bool> TransitionAsync(TState target, CancellationToken ct = default)
        {
            if (IsTransitioning)
                return false;

            bool changed = false;
            var next = target;

            while (true)
            {
                if (EqualityComparer<TState>.Default.Equals(State, next))
                    return changed;

                IsTransitioning = true;
                var previous = State;
                try
                {
                    if (_states.TryGetValue(previous, out var exit) && exit != null)
                        await exit.ExitAsync(ct);

                    if (_states.TryGetValue(next, out var enter) && enter != null)
                        await enter.EnterAsync(ct);

                    LastState = previous;
                    State = next;
                    OnStateChanged.Invoke(previous, next);
                    changed = true;
                }
                finally
                {
                    IsTransitioning = false;
                }

                if (!_pendingNext.HasValue)
                    return changed;

                next = _pendingNext.Value;
                _pendingNext = null;
            }
        }
    }
}
