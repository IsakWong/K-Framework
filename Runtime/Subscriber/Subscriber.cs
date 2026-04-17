using System;
using System.Collections.Generic;

/// <summary>
/// 订阅者类，用于统一管理信号订阅和 EventBus 订阅
/// 实现了 IDisposable 接口，支持自动清理订阅
/// </summary>
public class Subscriber : IDisposable
{
    /// <summary>
    /// 委托句柄字典，存储订阅的信号和回调的映射关系
    /// Key: 订阅句柄 (Guid)
    /// Value: 元组 (信号对象, 回调委托)
    /// </summary>
    private readonly Dictionary<Guid, Tuple<ISignal, Delegate>> _delegateHandles = new();
    
    /// <summary>
    /// 委托映射字典，用于存储委托之间的关系
    /// </summary>
    private Dictionary<Delegate, Delegate> _delegates = new();

    /// <summary>
    /// EventBus 订阅记录，用于 DisconnectAll 时自动退订
    /// </summary>
    private readonly List<(EventBus Bus, Type EventType, Delegate Handler)> _eventBusBindings = new();

    /// <summary>
    /// 订阅一个信号并存储订阅关系
    /// </summary>
    /// <typeparam name="TDelegate">委托类型</typeparam>
    /// <param name="eventSource">要订阅的信号源</param>
    /// <param name="callback">回调函数</param>
    /// <returns>订阅句柄，可用于取消订阅</returns>
    public Guid Subscribe<TDelegate>(Signal<TDelegate> eventSource, TDelegate callback) where TDelegate : Delegate
    {
        var handle = eventSource.Connect(callback);
        _delegateHandles[handle] = new Tuple<ISignal, Delegate>(eventSource, callback);
        return handle;
    }

    /// <summary>
    /// 使用句柄取消订阅
    /// </summary>
    /// <typeparam name="TDelegate">委托类型</typeparam>
    /// <param name="action">要取消订阅的委托（引用传递）</param>
    /// <param name="handle">订阅时返回的句柄</param>
    public void Unsubscribe<TDelegate>(ref TDelegate action, Guid handle) where TDelegate : Delegate
    {
        var delegateHandles = _delegateHandles[handle];
        delegateHandles.Item1.Remove(delegateHandles.Item2);
    }

    /// <summary>
    /// 记录 EventBus 订阅（由 EventBus.Subscribe 内部调用）
    /// </summary>
    internal void TrackEventBus(EventBus bus, Type eventType, Delegate handler)
    {
        _eventBusBindings.Add((bus, eventType, handler));
    }

    /// <summary>
    /// 清除所有订阅并移除所有监听器（包括 Signal 和 EventBus）
    /// </summary>
    public void DisconnectAll()
    {
        // 清理 Signal 订阅
        foreach (var it in _delegateHandles)
        {
            it.Value.Item1.Remove(it.Value.Item2);
        }
        _delegates.Clear();
        _delegateHandles.Clear();

        // 清理 EventBus 订阅
        foreach (var (bus, eventType, handler) in _eventBusBindings)
        {
            bus.Unsubscribe(eventType, handler);
        }
        _eventBusBindings.Clear();
    }

    /// <summary>
    /// 释放资源，清除所有订阅
    /// 实现 IDisposable 接口，支持 using 语句自动释放
    /// </summary>
    public void Dispose()
    {
        DisconnectAll();
    }
}