// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;

namespace KFramework
{
    /// <summary>
    /// 子流程节点：把另一个 IFlowNode 当作单个节点嵌入。
    /// 子流程**共享外层 FlowContext**——如需隔离请显式构造新 ctx 自行 Run。
    /// </summary>
    public sealed class FlowSubFlow : IFlowNode
    {
        public IFlowNode Inner;

        public FlowSubFlow(IFlowNode inner)
        {
            Inner = inner;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Inner != null) yield return Inner.Execute(ctx);
        }
    }
}
