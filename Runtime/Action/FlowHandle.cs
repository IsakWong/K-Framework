// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections;
using UnityEngine;

namespace KFramework
{
    /// <summary>
    /// 运行中 Flow 的句柄。FlowRunner.Run() 返回。
    /// 用于：取消、查询完成状态、等待完成（IEnumerator）。
    /// </summary>
    public sealed class FlowHandle
    {
        public FlowContext Context { get; }
        public bool IsRunning => !IsCompleted && !IsCancelled && !IsFailed;
        public bool IsCompleted { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsFailed { get; private set; }
        public Exception Error => Context?.LastError;

        /// <summary>完成回调（成功/失败/取消都会触发，可用于链式后续动作）。</summary>
        public event Action<FlowHandle> OnFinished;

        private Coroutine _coroutine;
        private MonoBehaviour _owner;

        internal FlowHandle(FlowContext ctx)
        {
            Context = ctx;
        }

        internal void AttachCoroutine(Coroutine coroutine, MonoBehaviour owner)
        {
            _coroutine = coroutine;
            _owner = owner;
        }

        internal void MarkCompleted()
        {
            if (IsCompleted || IsCancelled || IsFailed) return;
            IsCompleted = true;
            OnFinished?.Invoke(this);
        }

        internal void MarkCancelled()
        {
            if (IsCompleted || IsCancelled || IsFailed) return;
            IsCancelled = true;
            OnFinished?.Invoke(this);
        }

        internal void MarkFailed(Exception ex)
        {
            if (IsCompleted || IsCancelled || IsFailed) return;
            Context.LastError = ex;
            IsFailed = true;
            OnFinished?.Invoke(this);
        }

        /// <summary>取消运行中的 Flow。已经启动的子协程会在下个 yield 后退出。</summary>
        public void Cancel()
        {
            if (!IsRunning) return;
            Context.Cancel();
            // 注意：不主动 StopCoroutine，让 FlowRunner 在 RunInternal 检查 ctx.IsCancelled 后正常退出，
            // 这样 finally 块（订阅清理）能保证执行。
        }

        /// <summary>
        /// 协程内等待此 Flow 完成：yield return handle.Wait();
        /// </summary>
        public IEnumerator Wait()
        {
            while (IsRunning) yield return null;
        }
    }
}
