using System;

/// <summary>
/// 事件总线的事件基接口（标记接口）
/// 所有通过 EventBus 传递的事件类型都应实现此接口
/// 推荐使用 struct 以避免 GC 分配
/// </summary>
/// <example>
/// public struct PlayerDiedEvent : IEvent
/// {
///     public UnitBase Player;
///     public UnitBase Killer;
/// }
///
/// public struct ScoreChangedEvent : IEvent
/// {
///     public int OldScore;
///     public int NewScore;
/// }
/// </example>
public interface IEvent { }
