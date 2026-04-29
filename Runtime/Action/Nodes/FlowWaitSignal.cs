// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;

namespace KFramework.Action
{
    /// <summary>
    /// 等待 KSignal（无参）触发后继续。
    /// 在 Execute 进入时挂载 handler，finally 中清理——保证取消/异常时不泄漏订阅。
    /// </summary>
    public sealed class FlowWaitSignal : IFlowNode
    {
        public KSignal Signal;

        public FlowWaitSignal(KSignal signal)
        {
            Signal = signal;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Signal == null) yield break;

            bool fired = false;
            Action handler = () => fired = true;
            Signal.Add(handler);
            try
            {
                while (!fired && !ctx.IsCancelled) yield return null;
            }
            finally
            {
                Signal.Remove(handler);
            }
        }
    }

    /// <summary>带 1 个参数的信号等待，可选过滤器（默认任意触发都满足）。</summary>
    public sealed class FlowWaitSignal<T> : IFlowNode
    {
        public KSignal<T> Signal;
        public Func<T, bool> Filter;
        public Action<FlowContext, T> OnFired;

        public FlowWaitSignal(KSignal<T> signal, Func<T, bool> filter = null, Action<FlowContext, T> onFired = null)
        {
            Signal = signal;
            Filter = filter;
            OnFired = onFired;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Signal == null) yield break;

            bool fired = false;
            T captured = default;
            Action<T> handler = arg =>
            {
                if (Filter != null && !Filter(arg)) return;
                captured = arg;
                fired = true;
            };
            Signal.Add(handler);
            try
            {
                while (!fired && !ctx.IsCancelled) yield return null;
            }
            finally
            {
                Signal.Remove(handler);
            }

            if (fired && OnFired != null) OnFired(ctx, captured);
        }
    }
}
