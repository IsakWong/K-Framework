// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KFramework
{
    public enum TriggerLifecycle
    {
        /// <summary>触发一次后自动 Unregister。</summary>
        Once,
        /// <summary>每次事件都重新评估条件并执行动作。</summary>
        Repeating,
    }

    /// <summary>
    /// 事件-条件-动作触发器。
    ///
    /// 用法：
    ///   KTrigger.Once()
    ///       .On(player.OnDeath)
    ///       .When(ctx => achievementUnlocked == false)
    ///       .Do(_ => UnlockAchievement())
    ///       .BuildAndRegister();
    ///
    /// 注意：
    ///   - 同一个 trigger 在执行 KAction（Flow）期间，新的事件触发会被忽略（避免并发执行）
    ///   - Once 模式在 KAction 完成后才 Unregister——确保 KAction 跑完
    ///   - KAction 是任意 IFlowNode，可以是单个 Do 也可以是完整的 Flow
    /// </summary>
    public sealed class KTrigger
    {
        public string Name;
        public TriggerLifecycle Lifecycle;
        public IFlowCondition Condition;
        public IFlowNode KAction;

        public bool IsRegistered { get; private set; }
        public int FireCount { get; private set; }
        public bool IsExecuting => _currentHandle is { IsRunning: true };

        private MonoBehaviour _runner;
        private readonly List<ITriggerSource> _sources = new();
        private FlowHandle _currentHandle;

        public static KTriggerBuilder Once() => new(TriggerLifecycle.Once);
        public static KTriggerBuilder Repeating() => new(TriggerLifecycle.Repeating);

        // ─── 由 Builder 调用 ───

        internal void AddSource(ITriggerSource source)
        {
            _sources.Add(source);
        }

        // ─── 注册 / 反注册 ───

        public void Register(MonoBehaviour runner = null)
        {
            if (IsRegistered) return;
            _runner = runner != null ? runner : TriggerManager.Instance;
            if (_runner == null)
            {
                Debug.LogError($"[KTrigger '{Name}'] Cannot register: no runner available (TriggerManager not ready).");
                return;
            }

            foreach (var s in _sources) s.Bind(OnFired);
            TriggerManager.Instance?.RegisterInternal(this);
            IsRegistered = true;
        }

        public void Unregister()
        {
            if (!IsRegistered) return;
            foreach (var s in _sources) s.Unbind();
            TriggerManager.Instance?.UnregisterInternal(this);
            IsRegistered = false;
        }

        /// <summary>手动触发（无视事件源——会执行条件评估）。</summary>
        public void Fire()
        {
            OnFired();
        }

        // ─── 触发执行 ───

        private void OnFired()
        {
            if (!IsRegistered) return;
            if (IsExecuting) return;     // 上一次还在跑，忽略本次

            var ctx = new FlowContext();
            ctx.Set("trigger", this);

            if (Condition != null && !Condition.Evaluate(ctx)) return;

            FireCount++;

            if (KAction == null)
            {
                // 没有动作直接视为完成
                if (Lifecycle == TriggerLifecycle.Once) Unregister();
                return;
            }

            _currentHandle = KAction.Run(_runner, ctx);
            _currentHandle.OnFinished += OnActionFinished;
        }

        private void OnActionFinished(FlowHandle handle)
        {
            handle.OnFinished -= OnActionFinished;
            _currentHandle = null;

            if (Lifecycle == TriggerLifecycle.Once)
            {
                Unregister();
            }
        }
    }

    /// <summary>KTrigger 链式构建器。</summary>
    public sealed class KTriggerBuilder
    {
        private readonly KTrigger _trigger;

        internal KTriggerBuilder(TriggerLifecycle lifecycle)
        {
            _trigger = new KTrigger { Lifecycle = lifecycle };
        }

        public KTriggerBuilder Named(string name)
        {
            _trigger.Name = name;
            return this;
        }

        // ─── 事件源 ───

        public KTriggerBuilder On(KSignal signal)
        {
            _trigger.AddSource(new SignalTriggerSource(signal));
            return this;
        }

        public KTriggerBuilder On<T>(KSignal<T> signal, Func<T, bool> filter = null)
        {
            _trigger.AddSource(new SignalTriggerSource<T>(signal, filter));
            return this;
        }

        public KTriggerBuilder On<T>(Func<T, bool> filter = null) where T : struct, IEvent
        {
            _trigger.AddSource(new EventBusTriggerSource<T>(filter));
            return this;
        }

        public KTriggerBuilder OnManual()
        {
            _trigger.AddSource(new ManualTriggerSource());
            return this;
        }

        // ─── 条件 ───

        public KTriggerBuilder When(Func<FlowContext, bool> predicate)
        {
            _trigger.Condition = new FlowConditionFunc(predicate);
            return this;
        }

        public KTriggerBuilder When(Func<bool> predicate)
        {
            _trigger.Condition = new FlowConditionFunc(predicate);
            return this;
        }

        public KTriggerBuilder When(IFlowCondition condition)
        {
            _trigger.Condition = condition;
            return this;
        }

        // ─── 动作 ───

        public KTriggerBuilder Do(IFlowNode node)
        {
            _trigger.KAction = node;
            return this;
        }

        public KTriggerBuilder Do(Action action)
        {
            _trigger.KAction = new FlowAction { SyncAction = _ => action?.Invoke() };
            return this;
        }

        public KTriggerBuilder Do(Action<FlowContext> action)
        {
            _trigger.KAction = new FlowAction { SyncAction = action };
            return this;
        }

        // ─── 收尾 ───

        public KTrigger Build() => _trigger;

        public KTrigger BuildAndRegister(MonoBehaviour runner = null)
        {
            _trigger.Register(runner);
            return _trigger;
        }
    }
}
