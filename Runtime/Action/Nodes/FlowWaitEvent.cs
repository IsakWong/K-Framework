// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;

namespace KFramework.Action
{
    /// <summary>
    /// 等待 EventBus 上的特定事件类型。可选过滤器（filter）。
    /// 触发后通过 ctx.Set&lt;T&gt;(evt) 把事件实例写入上下文，方便下游节点取用。
    /// </summary>
    public sealed class FlowWaitEvent<T> : IFlowNode where T : struct, IEvent
    {
        public Func<T, bool> Filter;

        /// <summary>触发后是否将事件实例写入 ctx.Set&lt;T&gt;()。默认 true。</summary>
        public bool StoreInContext = true;

        public FlowWaitEvent(Func<T, bool> filter = null)
        {
            Filter = filter;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            bool fired = false;
            T captured = default;

            Action<T> handler = e =>
            {
                if (Filter != null && !Filter(e)) return;
                captured = e;
                fired = true;
            };

            EventBus.Instance.Subscribe(handler);
            try
            {
                while (!fired && !ctx.IsCancelled) yield return null;
            }
            finally
            {
                EventBus.Instance.Unsubscribe(handler);
            }

            if (fired && StoreInContext)
            {
                // 注：Set<T>() 限制 T : class，这里 T 是 struct，所以用字符串 key
                ctx.Set(typeof(T).FullName, captured);
            }
        }
    }
}
