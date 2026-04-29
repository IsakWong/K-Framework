// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 把 IFlowNode 跑起来的入口。
    /// 用法：
    ///   flow.Run(this);                              // this 是任意 MonoBehaviour
    ///   var handle = flow.Run(this, presetContext);  // 复用预设上下文
    /// </summary>
    public static class FlowRunner
    {
        public static FlowHandle Run(this IFlowNode node, MonoBehaviour owner, FlowContext ctx = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            ctx ??= new FlowContext();
            ctx.ResetCancel();
            ctx.Owner = owner;

            var handle = new FlowHandle(ctx);
            var coroutine = owner.StartCoroutine(RunInternal(node, ctx, handle));
            handle.AttachCoroutine(coroutine, owner);
            return handle;
        }

        private static IEnumerator RunInternal(IFlowNode node, FlowContext ctx, FlowHandle handle)
        {
            IEnumerator inner;
            try
            {
                inner = node.Execute(ctx);
            }
            catch (Exception ex)
            {
                handle.MarkFailed(ex);
                Debug.LogException(ex);
                yield break;
            }

            // 手动驱动外层 IEnumerator，每一步前检查取消，捕获异常
            while (true)
            {
                if (ctx.IsCancelled)
                {
                    handle.MarkCancelled();
                    yield break;
                }

                bool moved;
                try
                {
                    moved = inner.MoveNext();
                }
                catch (Exception ex)
                {
                    handle.MarkFailed(ex);
                    Debug.LogException(ex);
                    yield break;
                }

                if (!moved) break;
                yield return inner.Current;
            }

            if (ctx.IsCancelled) handle.MarkCancelled();
            else handle.MarkCompleted();
        }
    }
}
