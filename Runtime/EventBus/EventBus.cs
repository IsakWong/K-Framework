using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件总线 — 模块间解耦通信的核心设施
///
/// 与 KSignal 的区别:
///   KSignal — 实例级信号，调用者必须持有发布者引用（紧耦合）
///   EventBus — 全局类型路由，发布者和订阅者互不感知（松耦合）
///
/// 设计要点:
///   • 事件按 Type 路由，编译期类型安全
///   • 推荐用 struct 定义事件，避免 GC
///   • 支持优先级排序、一次性订阅、粘性事件
///   • 可与 Subscriber 集成实现批量退订
/// </summary>
/// <example>
/// // 定义事件
/// public struct EnemyKilledEvent : IEvent { public int EnemyId; public int Score; }
///
/// // 订阅
/// EventBus.Instance.Subscribe&lt;EnemyKilledEvent&gt;(OnEnemyKilled);
///
/// // 发布
/// EventBus.Instance.Publish(new EnemyKilledEvent { EnemyId = 42, Score = 100 });
///
/// // 配合 Subscriber 自动清理
/// EventBus.Instance.Subscribe&lt;EnemyKilledEvent&gt;(subscriber, OnEnemyKilled);
/// subscriber.DisconnectAll(); // 自动退订 EventBus 中的监听
/// </example>
public class EventBus : KSingleton<EventBus>, IEventBusService
{
    // ─── 内部类型 ───

    /// <summary>单个订阅记录</summary>
    private class Binding
    {
        public Delegate Handler;
        public int Priority;
        public bool Once;
    }

    /// <summary>某个事件类型的所有订阅</summary>
    private class Channel
    {
        public readonly List<Binding> Bindings = new();
        public bool Dirty; // 优先级排序标记
    }

    // ─── 存储 ───

    /// <summary>事件类型 → 订阅通道</summary>
    private readonly Dictionary<Type, Channel> _channels = new();

    /// <summary>粘性事件缓存（最后一次发布的事件实例）</summary>
    private readonly Dictionary<Type, object> _stickyEvents = new();

    protected override void OnServiceRegistered()
    {
        ServiceLocator.Register<IEventBusService>(this);
    }

    // ═══════════════════════════════════════════════════════════════
    //  订阅
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T">事件类型（推荐 struct : IEvent）</typeparam>
    /// <param name="handler">回调</param>
    /// <param name="priority">优先级，数值越小越先执行（默认 0）</param>
    public void Subscribe<T>(Action<T> handler, int priority = 0) where T : struct, IEvent
    {
        GetOrCreateChannel<T>().Bindings.Add(new Binding
        {
            Handler = handler,
            Priority = priority,
            Once = false
        });
        MarkDirty<T>();
    }

    /// <summary>
    /// 订阅事件（配合 Subscriber 自动清理）
    /// </summary>
    public void Subscribe<T>(Subscriber subscriber, Action<T> handler, int priority = 0) where T : struct, IEvent
    {
        Subscribe(handler, priority);
        subscriber.TrackEventBus(this, typeof(T), handler);
    }

    /// <summary>
    /// 一次性订阅，触发一次后自动退订
    /// </summary>
    public void SubscribeOnce<T>(Action<T> handler, int priority = 0) where T : struct, IEvent
    {
        GetOrCreateChannel<T>().Bindings.Add(new Binding
        {
            Handler = handler,
            Priority = priority,
            Once = true
        });
        MarkDirty<T>();
    }

    // ═══════════════════════════════════════════════════════════════
    //  退订
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 退订事件
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
    {
        Unsubscribe(typeof(T), handler);
    }

    /// <summary>
    /// 退订事件（内部使用，支持 Type + Delegate）
    /// </summary>
    internal void Unsubscribe(Type eventType, Delegate handler)
    {
        if (!_channels.TryGetValue(eventType, out var channel)) return;

        for (int i = channel.Bindings.Count - 1; i >= 0; i--)
        {
            if (channel.Bindings[i].Handler.Equals(handler))
            {
                channel.Bindings.RemoveAt(i);
                return;
            }
        }
    }

    /// <summary>
    /// 移除某个事件类型的所有订阅
    /// </summary>
    public void UnsubscribeAll<T>() where T : struct, IEvent
    {
        if (_channels.TryGetValue(typeof(T), out var channel))
        {
            channel.Bindings.Clear();
        }
    }

    /// <summary>
    /// 清除所有事件的所有订阅
    /// </summary>
    public void Clear()
    {
        _channels.Clear();
        _stickyEvents.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  发布
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 发布事件，立即通知所有订阅者
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="evt">事件实例</param>
    public void Publish<T>(T evt) where T : struct, IEvent
    {
        if (!_channels.TryGetValue(typeof(T), out var channel)) return;

        // 按优先级排序（懒排序，仅在新增订阅后触发）
        if (channel.Dirty)
        {
            channel.Bindings.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            channel.Dirty = false;
        }

        // 正向遍历，用 count 快照防止遍历中新增导致死循环
        var count = channel.Bindings.Count;
        for (int i = 0; i < count; i++)
        {
            var binding = channel.Bindings[i];
            try
            {
                ((Action<T>)binding.Handler).Invoke(evt);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // 移除一次性订阅（反向遍历）
        for (int i = channel.Bindings.Count - 1; i >= 0; i--)
        {
            if (channel.Bindings[i].Once)
            {
                channel.Bindings.RemoveAt(i);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  粘性事件 (Sticky)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 发布粘性事件 — 缓存事件实例，后续新订阅者可立即获取最后一次值
    /// 适用于状态类事件（如登录状态、网络状态）
    /// </summary>
    public void PublishSticky<T>(T evt) where T : struct, IEvent
    {
        _stickyEvents[typeof(T)] = evt;
        Publish(evt);
    }

    /// <summary>
    /// 订阅并立即获取粘性事件的最后一次值（如果存在）
    /// </summary>
    public void SubscribeSticky<T>(Action<T> handler, int priority = 0) where T : struct, IEvent
    {
        Subscribe(handler, priority);

        if (_stickyEvents.TryGetValue(typeof(T), out var cached))
        {
            try
            {
                handler.Invoke((T)cached);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    /// <summary>
    /// 订阅粘性事件（配合 Subscriber 自动清理）
    /// </summary>
    public void SubscribeSticky<T>(Subscriber subscriber, Action<T> handler, int priority = 0) where T : struct, IEvent
    {
        SubscribeSticky(handler, priority);
        subscriber.TrackEventBus(this, typeof(T), handler);
    }

    /// <summary>
    /// 获取粘性事件的缓存值（不订阅）
    /// </summary>
    public bool TryGetSticky<T>(out T evt) where T : struct, IEvent
    {
        if (_stickyEvents.TryGetValue(typeof(T), out var cached))
        {
            evt = (T)cached;
            return true;
        }

        evt = default;
        return false;
    }

    /// <summary>
    /// 移除粘性事件缓存
    /// </summary>
    public void RemoveSticky<T>() where T : struct, IEvent
    {
        _stickyEvents.Remove(typeof(T));
    }

    // ═══════════════════════════════════════════════════════════════
    //  查询
    // ═══════════════════════════════════════════════════════════════

    /// <summary>某事件类型的订阅者数量</summary>
    public int GetSubscriberCount<T>() where T : struct, IEvent
    {
        return _channels.TryGetValue(typeof(T), out var ch) ? ch.Bindings.Count : 0;
    }

    /// <summary>是否有任何订阅者监听该事件</summary>
    public bool HasSubscribers<T>() where T : struct, IEvent
    {
        return GetSubscriberCount<T>() > 0;
    }

    // ─── 内部工具 ───

    private Channel GetOrCreateChannel<T>()
    {
        var type = typeof(T);
        if (!_channels.TryGetValue(type, out var channel))
        {
            channel = new Channel();
            _channels[type] = channel;
        }
        return channel;
    }

    private void MarkDirty<T>()
    {
        if (_channels.TryGetValue(typeof(T), out var channel))
        {
            channel.Dirty = true;
        }
    }
}
