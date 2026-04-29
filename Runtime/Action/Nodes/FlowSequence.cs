// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;
using System.Collections.Generic;

namespace KFramework.Action
{
    /// <summary>
    /// 顺序容器：依次执行子节点。Builder 的 Then/Else/Repeat/Parallel 子分支底层都是它。
    /// 子节点抛异常或取消时立即停止剩余节点。
    /// </summary>
    public sealed class FlowSequence : IFlowNode, IFlowContainer
    {
        public List<IFlowNode> Children { get; } = new();

        public void Add(IFlowNode node)
        {
            if (node != null) Children.Add(node);
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            foreach (var child in Children)
            {
                if (ctx.IsCancelled) yield break;
                if (child == null) continue;
                yield return child.Execute(ctx);
                if (ctx.LastError != null) yield break;
            }
        }
    }

    /// <summary>
    /// 表示某个节点对外是"容器"——Builder 在嵌套（Then/ParallelAll/Body）时往里面塞子节点。
    /// </summary>
    public interface IFlowContainer
    {
        void Add(IFlowNode node);
    }
}
