// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KFramework
{
    public enum ParallelMode
    {
        /// <summary>等待所有分支完成才继续。</summary>
        All,
        /// <summary>任一分支完成即继续，其余分支被中止。</summary>
        Any,
    }

    /// <summary>
    /// 并行节点：同时启动所有 Branches 的子协程，按 Mode 决定何时结束。
    /// 取消时会 StopCoroutine 所有子协程。
    /// 注意：子分支共享同一个 FlowContext——共享数据要自己保证线程/顺序安全。
    /// </summary>
    public sealed class FlowParallel : IFlowNode
    {
        public ParallelMode Mode;
        public List<IFlowNode> Branches { get; } = new();

        public IEnumerator Execute(FlowContext ctx)
        {
            if (Branches.Count == 0) yield break;

            var owner = ctx.Owner;
            if (owner == null)
            {
                Debug.LogError("[FlowParallel] FlowContext.Owner is null. Did you start the flow via FlowRunner.Run(this)?");
                yield break;
            }

            int count = Branches.Count;
            var done = new bool[count];
            var coroutines = new Coroutine[count];

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var branch = Branches[idx];
                if (branch == null) { done[idx] = true; continue; }
                coroutines[idx] = owner.StartCoroutine(RunChild(branch, ctx, () => done[idx] = true));
            }

            while (true)
            {
                if (ctx.IsCancelled)
                {
                    StopAll(owner, coroutines);
                    yield break;
                }

                bool finished = Mode == ParallelMode.All ? AllDone(done) : AnyDone(done);
                if (finished) break;
                yield return null;
            }

            if (Mode == ParallelMode.Any) StopAll(owner, coroutines);
        }

        private static IEnumerator RunChild(IFlowNode node, FlowContext ctx, System.Action onDone)
        {
            yield return node.Execute(ctx);
            onDone?.Invoke();
        }

        private static bool AllDone(bool[] flags)
        {
            for (int i = 0; i < flags.Length; i++)
                if (!flags[i]) return false;
            return true;
        }

        private static bool AnyDone(bool[] flags)
        {
            for (int i = 0; i < flags.Length; i++)
                if (flags[i]) return true;
            return false;
        }

        private static void StopAll(MonoBehaviour owner, Coroutine[] coroutines)
        {
            foreach (var c in coroutines)
                if (c != null) owner.StopCoroutine(c);
        }
    }
}
