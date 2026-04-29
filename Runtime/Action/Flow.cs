// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// Flow 静态入口。所有链式构建从这里开始。
    /// </summary>
    public static class Flow
    {
        public static FlowBuilder Create() => new();

        /// <summary>
        /// 把单个节点直接当作 IFlowNode 包一层（少数场景需要——比如想给单节点 .Run(this)）。
        /// 单节点已经实现 IFlowNode，可以直接 .Run，无需此包装。
        /// </summary>
        public static IFlowNode From(IFlowNode node) => node;
    }

    /// <summary>
    /// 链式 Flow 构建器。
    ///
    /// 设计规则：
    ///   - 每个 Builder 实例对应一个根 FlowSequence
    ///   - 嵌套（If/Then/Loop/Parallel）通过 Action&lt;FlowBuilder&gt; 闭包传入
    ///   - .If(...).Then(...).Else(...) 是状态机，未配 Then 直接 Else 会抛
    ///   - Build() 返回根节点；可重复 Build（不修改 builder 状态）
    /// </summary>
    public sealed class FlowBuilder
    {
        private readonly FlowSequence _root = new();
        private FlowIf _pendingIf;   // .If(...) 后等待 .Then(...) 的节点

        public FlowBuilder() { }

        // ─── 基础动作 ───

        public FlowBuilder Do(Action<FlowContext> action)
        {
            return Do(null, action);
        }

        public FlowBuilder Do(string name, Action<FlowContext> action)
        {
            FinalizePendingIf();
            _root.Add(new FlowAction { Name = name, SyncAction = action });
            return this;
        }

        public FlowBuilder Do(Func<FlowContext, IEnumerator> coroutine)
        {
            FinalizePendingIf();
            _root.Add(new FlowAction { AsyncRoutine = coroutine });
            return this;
        }

        public FlowBuilder Do(IFlowNode node)
        {
            FinalizePendingIf();
            if (node != null) _root.Add(node);
            return this;
        }

        // ─── 时间等待 ───

        public FlowBuilder Wait(float seconds, bool realtime = false)
        {
            FinalizePendingIf();
            _root.Add(new FlowWait(seconds, realtime));
            return this;
        }

        public FlowBuilder WaitFrames(int count)
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitFrames(count));
            return this;
        }

        // ─── 信号 / 事件 / 条件 等待 ───

        public FlowBuilder WaitFor(KSignal signal)
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitSignal(signal));
            return this;
        }

        public FlowBuilder WaitFor<T>(KSignal<T> signal, Func<T, bool> filter = null, Action<FlowContext, T> onFired = null)
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitSignal<T>(signal, filter, onFired));
            return this;
        }

        public FlowBuilder WaitForEvent<T>(Func<T, bool> filter = null) where T : struct, IEvent
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitEvent<T> { Filter = filter });
            return this;
        }

        public FlowBuilder WaitUntil(Func<FlowContext, bool> predicate)
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitCondition { Mode = WaitConditionMode.Until, Predicate = predicate });
            return this;
        }

        public FlowBuilder WaitUntil(Func<bool> predicate)
        {
            return WaitUntil(_ => predicate?.Invoke() ?? true);
        }

        public FlowBuilder WaitWhile(Func<FlowContext, bool> predicate)
        {
            FinalizePendingIf();
            _root.Add(new FlowWaitCondition { Mode = WaitConditionMode.While, Predicate = predicate });
            return this;
        }

        public FlowBuilder WaitWhile(Func<bool> predicate)
        {
            return WaitWhile(_ => predicate?.Invoke() ?? false);
        }

        // ─── 条件分支 ───

        /// <summary>
        /// 开启 If 节点，必须紧接 .Then(...)。可选 .Else(...) 后会自动闭合。
        /// 例：.If(ctx => ...).Then(b => b.Do(...)).Else(b => b.Do(...))
        /// </summary>
        public FlowBuilder If(Func<FlowContext, bool> predicate)
        {
            FinalizePendingIf();
            _pendingIf = new FlowIf { Condition = new FlowConditionFunc(predicate) };
            return this;
        }

        public FlowBuilder If(IFlowCondition condition)
        {
            FinalizePendingIf();
            _pendingIf = new FlowIf { Condition = condition };
            return this;
        }

        public FlowBuilder Then(Action<FlowBuilder> branch)
        {
            if (_pendingIf == null)
                throw new InvalidOperationException("Then() must follow If(...).");
            _pendingIf.ThenBranch = BuildBranch(branch);
            return this;
        }

        public FlowBuilder Else(Action<FlowBuilder> branch)
        {
            if (_pendingIf == null || _pendingIf.ThenBranch == null)
                throw new InvalidOperationException("Else() must follow If(...).Then(...).");
            _pendingIf.ElseBranch = BuildBranch(branch);
            FinalizePendingIf();
            return this;
        }

        // ─── 循环 ───

        public FlowBuilder Repeat(int times, Action<FlowBuilder> body)
        {
            FinalizePendingIf();
            _root.Add(new FlowLoop
            {
                Mode = FlowLoopMode.Repeat,
                Times = times,
                Body = BuildBranch(body),
            });
            return this;
        }

        public FlowBuilder While(Func<FlowContext, bool> predicate, Action<FlowBuilder> body)
        {
            FinalizePendingIf();
            _root.Add(new FlowLoop
            {
                Mode = FlowLoopMode.While,
                Condition = new FlowConditionFunc(predicate),
                Body = BuildBranch(body),
            });
            return this;
        }

        public FlowBuilder Until(Func<FlowContext, bool> predicate, Action<FlowBuilder> body)
        {
            FinalizePendingIf();
            _root.Add(new FlowLoop
            {
                Mode = FlowLoopMode.Until,
                Condition = new FlowConditionFunc(predicate),
                Body = BuildBranch(body),
            });
            return this;
        }

        // ─── 并行 ───

        public FlowBuilder ParallelAll(params Action<FlowBuilder>[] branches)
        {
            return Parallel(ParallelMode.All, branches);
        }

        public FlowBuilder ParallelAny(params Action<FlowBuilder>[] branches)
        {
            return Parallel(ParallelMode.Any, branches);
        }

        private FlowBuilder Parallel(ParallelMode mode, Action<FlowBuilder>[] branches)
        {
            FinalizePendingIf();
            var parallel = new FlowParallel { Mode = mode };
            foreach (var b in branches)
            {
                if (b != null) parallel.Branches.Add(BuildBranch(b));
            }
            _root.Add(parallel);
            return this;
        }

        // ─── 嵌套 ───

        public FlowBuilder SubFlow(IFlowNode flow)
        {
            FinalizePendingIf();
            _root.Add(new FlowSubFlow(flow));
            return this;
        }

        // ─── 构建 ───

        public IFlowNode Build()
        {
            FinalizePendingIf();
            return _root;
        }

        // ─── 内部 ───

        private void FinalizePendingIf()
        {
            if (_pendingIf == null) return;
            // 若 If 后没接 Then，把它当作"空 then"（不执行）；这通常是误用，警告一下
            if (_pendingIf.ThenBranch == null)
            {
                Debug.LogWarning("[FlowBuilder] If() was not followed by Then(); branch will be empty.");
                _pendingIf.ThenBranch = new FlowSequence();
            }
            _root.Add(_pendingIf);
            _pendingIf = null;
        }

        private static IFlowNode BuildBranch(Action<FlowBuilder> branch)
        {
            if (branch == null) return new FlowSequence();
            var b = new FlowBuilder();
            branch(b);
            return b.Build();
        }
    }
}
