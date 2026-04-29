// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;

namespace KFramework
{
    /// <summary>
    /// 单次动作节点。两种模式：
    ///   - SyncAction：同步执行一次（最常见，Do(ctx => ...)）
    ///   - AsyncRoutine：返回 IEnumerator，按协程驱动（Do(ctx => MyCoroutine(ctx))）
    /// 同时设置时优先 AsyncRoutine。
    /// </summary>
    public sealed class FlowAction : IFlowNode
    {
        public string Name;
        public Action<FlowContext> SyncAction;
        public Func<FlowContext, IEnumerator> AsyncRoutine;

        public IEnumerator Execute(FlowContext ctx)
        {
            if (AsyncRoutine != null)
            {
                yield return AsyncRoutine(ctx);
                yield break;
            }

            SyncAction?.Invoke(ctx);
        }
    }
}
