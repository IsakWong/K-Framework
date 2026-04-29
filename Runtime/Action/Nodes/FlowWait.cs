// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System.Collections;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 时间等待：使用 Time.deltaTime 累计秒数。受 Time.timeScale 影响。
    /// </summary>
    public sealed class FlowWait : IFlowNode
    {
        public float Seconds;
        public bool Realtime;

        public FlowWait(float seconds, bool realtime = false)
        {
            Seconds = seconds;
            Realtime = realtime;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            float elapsed = 0f;
            while (elapsed < Seconds)
            {
                if (ctx.IsCancelled) yield break;
                yield return null;
                elapsed += Realtime ? Time.unscaledDeltaTime : Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 帧数等待：等待 Count 帧（每帧 yield null）。
    /// </summary>
    public sealed class FlowWaitFrames : IFlowNode
    {
        public int Count;

        public FlowWaitFrames(int count)
        {
            Count = count;
        }

        public IEnumerator Execute(FlowContext ctx)
        {
            for (int i = 0; i < Count; i++)
            {
                if (ctx.IsCancelled) yield break;
                yield return null;
            }
        }
    }
}
