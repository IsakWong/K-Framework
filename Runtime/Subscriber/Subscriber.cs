using System;
using System.Collections.Generic;

/// <summary>
/// 订阅者类，用于统一管理信号订阅和 EventBus 订阅。
/// 实现了 IDisposable 接口，支持自动清理订阅。
/// </summary>
public class Subscriber : IDisposable
{
    /// <summary>
    /// 信号绑定记录（值类型 struct，避免堆分配）
    /// </summary>
    private struct SignalBinding
    {
        public ISignal Signal;
        public Delegate Callback;
    }

    /// <summary>
    /// 信号订阅记录：句柄 → (信号, 回调)
    /// </summary>
    private readonly Dictionary<int, SignalBinding> _signalBindings = new();

    /// <summary>
    /// EventBus 订阅记录，用于 DisconnectAll 时自动退订
    /// </summary>
    private readonly List<(EventBus Bus, Type EventType, Delegate Handler)> _eventBusBindings = new();

    /// <summary>
    /// 订阅一个信号并存储订阅关系
    /// </summary>
    public int Subscribe<TDelegate>(Signal<TDelegate> eventSource, TDelegate callback)
        where TDelegate : Delegate
    {
        var handle = eventSource.Connect(callback);
        _signalBindings[handle] = new SignalBinding
        {
            Signal = eventSource,
            Callback = callback
        };
        return handle;
    }

    /// <summary>
    /// 通过句柄取消单个信号订阅
    /// </summary>
    public bool Unsubscribe(int handle)
    {
        if (!_signalBindings.TryGetValue(handle, out var binding))
            return false;

        binding.Signal.Remove(binding.Callback);
        _signalBindings.Remove(handle);
        return true;
    }

    /// <summary>
    /// 记录 EventBus 订阅（由 EventBus.Subscribe 内部调用）
    /// </summary>
    internal void TrackEventBus(EventBus bus, Type eventType, Delegate handler)
    {
        _eventBusBindings.Add((bus, eventType, handler));
    }

    /// <summary>
    /// 清除所有订阅（包括 Signal 和 EventBus）
    /// </summary>
    public void DisconnectAll()
    {
        // 清理 Signal 订阅
        foreach (var kv in _signalBindings)
        {
            kv.Value.Signal.Remove(kv.Value.Callback);
        }
        _signalBindings.Clear();

        // 清理 EventBus 订阅
        foreach (var (bus, eventType, handler) in _eventBusBindings)
        {
            bus.Unsubscribe(eventType, handler);
        }
        _eventBusBindings.Clear();
    }

    /// <summary>
    /// 释放资源，清除所有订阅
    /// </summary>
    public void Dispose()
    {
        DisconnectAll();
    }
}
