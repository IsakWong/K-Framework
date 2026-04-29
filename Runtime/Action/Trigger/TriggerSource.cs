// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2026/04/29

using System;

namespace KFramework.Action
{
    /// <summary>
    /// 触发源抽象：负责绑定外部事件并在事件到来时回调 handler。
    /// 多个 Source 可同时绑定到一个 KTrigger（任一源触发都会激活）。
    /// </summary>
    internal interface ITriggerSource
    {
        void Bind(System.Action handler);
        void Unbind();
    }

    /// <summary>包装无参 KSignal。</summary>
    internal sealed class SignalTriggerSource : ITriggerSource
    {
        private readonly KSignal _signal;
        private System.Action _handler;

        public SignalTriggerSource(KSignal signal) => _signal = signal;

        public void Bind(System.Action handler)
        {
            if (_signal == null) return;
            _handler = handler;
            _signal.Add(_handler);
        }

        public void Unbind()
        {
            if (_signal == null || _handler == null) return;
            _signal.Remove(_handler);
            _handler = null;
        }
    }

    /// <summary>包装单参 KSignal&lt;T&gt;。可选 filter。</summary>
    internal sealed class SignalTriggerSource<T> : ITriggerSource
    {
        private readonly KSignal<T> _signal;
        private readonly Func<T, bool> _filter;
        private Action<T> _handler;

        public SignalTriggerSource(KSignal<T> signal, Func<T, bool> filter)
        {
            _signal = signal;
            _filter = filter;
        }

        public void Bind(System.Action handler)
        {
            if (_signal == null) return;
            _handler = arg =>
            {
                if (_filter != null && !_filter(arg)) return;
                handler?.Invoke();
            };
            _signal.Add(_handler);
        }

        public void Unbind()
        {
            if (_signal == null || _handler == null) return;
            _signal.Remove(_handler);
            _handler = null;
        }
    }

    /// <summary>EventBus 上某个事件类型作为触发源。</summary>
    internal sealed class EventBusTriggerSource<T> : ITriggerSource where T : struct, IEvent
    {
        private readonly Func<T, bool> _filter;
        private Action<T> _handler;

        public EventBusTriggerSource(Func<T, bool> filter)
        {
            _filter = filter;
        }

        public void Bind(System.Action handler)
        {
            _handler = e =>
            {
                if (_filter != null && !_filter(e)) return;
                handler?.Invoke();
            };
            EventBus.Instance.Subscribe(_handler);
        }

        public void Unbind()
        {
            if (_handler == null) return;
            EventBus.Instance.Unsubscribe(_handler);
            _handler = null;
        }
    }

    /// <summary>仅手动触发：通过 KTrigger.Fire() 启动。</summary>
    internal sealed class ManualTriggerSource : ITriggerSource
    {
        public System.Action Handler;

        public void Bind(System.Action handler)
        {
            Handler = handler;
        }

        public void Unbind()
        {
            Handler = null;
        }
    }
}
