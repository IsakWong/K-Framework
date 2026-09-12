using System;

/// <summary>
/// KFramework 通用声明式 ViewModel 基础契约。
/// 纯 C# 实现，不依赖 UnityEngine.UI，遵循单向数据流原则：输入命令 -> 状态转移 -> 派发新状态快照。
/// </summary>
/// <typeparam name="TState">不可变状态快照类型</typeparam>
public interface IUIViewModel<TState>
{
    /// <summary>当前不可变状态快照</summary>
    TState CurrentState { get; }

    /// <summary>状态变更通知：(上一个状态快照, 当前最新状态快照)</summary>
    event Action<TState, TState> OnStateChanged;
}
