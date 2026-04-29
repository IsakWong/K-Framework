// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;

namespace KFramework.Action
{
    /// <summary>
    /// 条件抽象：FlowIf / FlowLoop.While / KTrigger.When 共用。
    /// 必须是同步 + 无副作用：仅根据当前 ctx 与外部状态判断，不应触发其他逻辑。
    /// </summary>
    public interface IFlowCondition
    {
        bool Evaluate(FlowContext ctx);
    }

    /// <summary>
    /// 包装一个 Func 作为条件（Builder 内部最常用的实现）。
    /// </summary>
    public sealed class FlowConditionFunc : IFlowCondition
    {
        public Func<FlowContext, bool> Predicate;

        public FlowConditionFunc() { }

        public FlowConditionFunc(Func<FlowContext, bool> predicate)
        {
            Predicate = predicate;
        }

        public FlowConditionFunc(Func<bool> predicate)
        {
            // 适配无参谓词
            if (predicate != null) Predicate = _ => predicate();
        }

        public bool Evaluate(FlowContext ctx) => Predicate?.Invoke(ctx) ?? true;
    }
}
