// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;

namespace KFramework.Action
{
    /// <summary>
    /// Flow 与 Trigger 共用的可执行单元抽象。
    /// 每个节点返回一个 IEnumerator，由 Unity 协程驱动；ctx.Owner.StartCoroutine 调用其 Execute 即可运行。
    /// 实现要求：
    ///   - 不要在 Execute 内吞掉异常，让上层 FlowRunner 统一捕获并写入 FlowContext.LastError
    ///   - 长循环节点应主动检查 ctx.IsCancelled，及时 yield break
    ///   - 订阅外部事件的节点必须在 finally 中清理订阅，防止取消时泄漏
    /// </summary>
    public interface IFlowNode
    {
        IEnumerator Execute(FlowContext ctx);
    }
}
