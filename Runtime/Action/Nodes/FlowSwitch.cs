// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;
using System.Collections.Generic;

namespace KFramework.Action
{
    /// <summary>
    /// Switch 分支：按 Selector 返回的 key 选择对应分支执行。
    /// 没有匹配项时执行 DefaultBranch（可为 null）。
    /// </summary>
    public sealed class FlowSwitch<TKey> : IFlowNode
    {
        public Func<FlowContext, TKey> Selector;
        public Dictionary<TKey, IFlowNode> Cases = new();
        public IFlowNode DefaultBranch;

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Selector == null) yield break;
            var key = Selector(ctx);
            if (Cases.TryGetValue(key, out var branch))
            {
                if (branch != null) yield return branch.Execute(ctx);
            }
            else if (DefaultBranch != null)
            {
                yield return DefaultBranch.Execute(ctx);
            }
        }
    }
}
