using System;

/// <summary>
/// 事件总线服务接口
/// 提供类型路由的全局发布/订阅通信能力
/// </summary>
public interface IEventBusService
{
    // ─── 订阅 ───

    void Subscribe<T>(Action<T> handler, int priority = 0) where T : struct, IEvent;
    void Subscribe<T>(Subscriber subscriber, Action<T> handler, int priority = 0) where T : struct, IEvent;
    void SubscribeOnce<T>(Action<T> handler, int priority = 0) where T : struct, IEvent;

    // ─── 退订 ───

    void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent;
    void UnsubscribeAll<T>() where T : struct, IEvent;
    void Clear();

    // ─── 发布 ───

    void Publish<T>(T evt) where T : struct, IEvent;

    // ─── 粘性事件 ───

    void PublishSticky<T>(T evt) where T : struct, IEvent;
    void SubscribeSticky<T>(Action<T> handler, int priority = 0) where T : struct, IEvent;
    void SubscribeSticky<T>(Subscriber subscriber, Action<T> handler, int priority = 0) where T : struct, IEvent;
    bool TryGetSticky<T>(out T evt) where T : struct, IEvent;
    void RemoveSticky<T>() where T : struct, IEvent;

    // ─── 查询 ───

    int GetSubscriberCount<T>() where T : struct, IEvent;
    bool HasSubscribers<T>() where T : struct, IEvent;
}
