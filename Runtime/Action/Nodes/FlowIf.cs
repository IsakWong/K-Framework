// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;

namespace KFramework
{
    /// <summary>
    /// If/Then/Else 分支节点。
    /// Condition.Evaluate 为 true 走 ThenBranch，否则走 ElseBranch（可为 null）。
    /// </summary>
    public sealed class FlowIf : IFlowNode
    {
        public IFlowCondition Condition;
        public IFlowNode ThenBranch;
        public IFlowNode ElseBranch;

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Condition == null || Condition.Evaluate(ctx))
            {
                if (ThenBranch != null) yield return ThenBranch.Execute(ctx);
            }
            else
            {
                if (ElseBranch != null) yield return ElseBranch.Execute(ctx);
            }
        }
    }
}
