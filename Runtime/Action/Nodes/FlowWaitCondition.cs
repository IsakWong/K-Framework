// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;

namespace KFramework
{
    public enum WaitConditionMode
    {
        /// <summary>等待 Predicate 返回 true 才继续。</summary>
        Until,
        /// <summary>Predicate 为 true 期间持续等待，false 才继续。</summary>
        While,
    }

    /// <summary>
    /// 自定义条件等待。每帧 yield null 后重新评估。
    /// </summary>
    public sealed class FlowWaitCondition : IFlowNode
    {
        public WaitConditionMode Mode;
        public Func<FlowContext, bool> Predicate;

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Predicate == null) yield break;

            while (!ctx.IsCancelled)
            {
                bool current = Predicate(ctx);
                bool finished = Mode == WaitConditionMode.Until ? current : !current;
                if (finished) yield break;
                yield return null;
            }
        }
    }
}
