// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;

namespace KFramework
{
    public enum FlowLoopMode
    {
        /// <summary>固定次数循环。</summary>
        Repeat,
        /// <summary>While 循环：每轮**前**评估，true 才执行。</summary>
        While,
        /// <summary>Until 循环：每轮**后**评估，true 时退出（do-while 风格的 break-on-true）。</summary>
        Until,
    }

    /// <summary>
    /// 循环节点。Body 是子流程（通常是 FlowSequence）。
    /// 循环体内可通过 ctx.IsCancelled 退出整个 Flow，从而打断循环。
    /// </summary>
    public sealed class FlowLoop : IFlowNode
    {
        public FlowLoopMode Mode;
        public int Times;                      // Repeat 用
        public IFlowCondition Condition;       // While/Until 用
        public IFlowNode Body;

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Body == null) yield break;

            switch (Mode)
            {
                case FlowLoopMode.Repeat:
                    for (int i = 0; i < Times; i++)
                    {
                        if (ctx.IsCancelled) yield break;
                        yield return Body.Execute(ctx);
                        if (ctx.LastError != null) yield break;
                    }
                    break;

                case FlowLoopMode.While:
                    while (!ctx.IsCancelled && (Condition?.Evaluate(ctx) ?? false))
                    {
                        yield return Body.Execute(ctx);
                        if (ctx.LastError != null) yield break;
                    }
                    break;

                case FlowLoopMode.Until:
                    do
                    {
                        if (ctx.IsCancelled) yield break;
                        yield return Body.Execute(ctx);
                        if (ctx.LastError != null) yield break;
                    } while (!(Condition?.Evaluate(ctx) ?? true));
                    break;
            }
        }
    }
}
