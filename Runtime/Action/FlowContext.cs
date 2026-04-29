// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KFramework.Action
{
    /// <summary>
    /// Flow / Trigger 节点共享的运行时上下文。
    ///
    /// 职责：
    ///   - 跨节点传值（字符串 key 或类型化 key）
    ///   - 持有协程驱动者（Owner，必须是 MonoBehaviour）
    ///   - 取消标志（Cancel 后所有节点应在下个 yield 点退出）
    ///   - 异常记录（节点抛异常时，FlowRunner 写入 LastError）
    /// </summary>
    public class FlowContext
    {
        /// <summary>启动 Flow 的 MonoBehaviour，用于 StartCoroutine。FlowRunner.Run 时注入。</summary>
        public MonoBehaviour Owner { get; internal set; }

        /// <summary>取消标记。Cancel() 后只读为 true，节点应在下次 yield 检查。</summary>
        public bool IsCancelled => _cancelled;

        /// <summary>最近一次未捕获异常，由 FlowRunner 写入。</summary>
        public Exception LastError { get; internal set; }

        private bool _cancelled;
        private readonly Dictionary<string, object> _bag = new();
        private readonly Dictionary<Type, object> _typed = new();

        /// <summary>请求取消整个 Flow。已经在执行的节点不会立即停止，但下一个 yield 后退出。</summary>
        public void Cancel()
        {
            _cancelled = true;
        }

        // ─── 字符串 key 访问 ───

        public T Get<T>(string key, T fallback = default)
        {
            return _bag.TryGetValue(key, out var v) && v is T t ? t : fallback;
        }

        public void Set<T>(string key, T value)
        {
            _bag[key] = value;
        }

        public bool Has(string key) => _bag.ContainsKey(key);

        public bool TryGet<T>(string key, out T value)
        {
            if (_bag.TryGetValue(key, out var v) && v is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        public void Remove(string key) => _bag.Remove(key);

        // ─── 类型化 key 访问（推荐：避免拼写错） ───

        public T Get<T>() where T : class
        {
            return _typed.TryGetValue(typeof(T), out var v) ? v as T : null;
        }

        public void Set<T>(T value) where T : class
        {
            _typed[typeof(T)] = value;
        }

        public bool Has<T>() where T : class => _typed.ContainsKey(typeof(T));

        public void Remove<T>() where T : class => _typed.Remove(typeof(T));

        /// <summary>
        /// 内部使用：重置取消状态，便于 FlowHandle 复用同一个 ctx 跑多次（默认不复用）。
        /// </summary>
        internal void ResetCancel()
        {
            _cancelled = false;
            LastError = null;
        }
    }
}
