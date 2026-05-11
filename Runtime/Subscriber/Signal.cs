using System;
using System.Collections.Generic;

/// <summary>
/// 信号接口，定义信号的基本操作。
/// 仅用于 Subscriber 内部统一管理，推荐用户使用强类型 API。
/// </summary>
public interface ISignal
{
    /// <summary>
    /// 添加回调委托到信号
    /// </summary>
    public void Add(Delegate callback);

    /// <summary>
    /// 从信号中移除回调委托
    /// </summary>
    public void Remove(Delegate callback);
}

/// <summary>
/// 泛型信号基类，用于管理和触发委托回调。
/// 支持持久回调和一次性回调。
/// </summary>
/// <typeparam name="TDelegate">委托类型</typeparam>
public class Signal<TDelegate> : ISignal where TDelegate : Delegate
{
    /// <summary>持久委托链，每次 Invoke 时都会触发</summary>
    protected TDelegate _delegates;

    /// <summary>一次性委托链，Invoke 后自动清空</summary>
    protected TDelegate _delegatesOnce;

    /// <summary>委托句柄映射表，通过 int id 管理委托</summary>
    protected Dictionary<int, TDelegate> _handlesMap;

    /// <summary>一次性回调的句柄集合，Invoke 后批量清理</summary>
    private HashSet<int> _onceHandles;

    /// <summary>全局自增 id，保证跨实例唯一</summary>
    private static int _globalHandleId;

    private static int NextId() => ++_globalHandleId;

    // 懒初始化，避免无句柄使用时分配内存
    private Dictionary<int, TDelegate> HandlesMap =>
        _handlesMap ??= new Dictionary<int, TDelegate>();

    private HashSet<int> OnceHandles =>
        _onceHandles ??= new HashSet<int>();

    // ═══════════════════════════════════════════════════════════════
    //  ISignal 接口（仅 Subscriber 内部使用，冷路径）
    // ═══════════════════════════════════════════════════════════════

    void ISignal.Add(Delegate callback)
    {
        _delegates = (TDelegate)Delegate.Combine(_delegates, (TDelegate)callback);
    }

    void ISignal.Remove(Delegate callback)
    {
        _delegates = (TDelegate)Delegate.Remove(_delegates, (TDelegate)callback);
        // 也从 once 链中移除（如果存在）
        if (_delegatesOnce != null)
            _delegatesOnce = (TDelegate)Delegate.Remove(_delegatesOnce, (TDelegate)callback);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Connect
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 连接一个持久回调并返回句柄
    /// </summary>
    public int Connect(TDelegate callback)
    {
        var handle = NextId();
        _delegates = (TDelegate)Delegate.Combine(_delegates, callback);
        HandlesMap[handle] = callback;
        return handle;
    }

    /// <summary>
    /// 连接一个一次性回调并返回句柄。
    /// 该回调在第一次 Invoke 后自动移除。
    /// </summary>
    public int ConnectOnce(TDelegate callback)
    {
        var handle = NextId();
        _delegatesOnce = (TDelegate)Delegate.Combine(_delegatesOnce, callback);
        HandlesMap[handle] = callback;
        OnceHandles.Add(handle);
        return handle;
    }

    /// <summary>
    /// 通过订阅者连接回调（Subscriber 自动管理生命周期）
    /// </summary>
    public int Connect(Subscriber subscriber, TDelegate callback)
    {
        return subscriber.Subscribe(this, callback);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Disconnect
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 通过回调引用断开
    /// </summary>
    public void Disconnect(TDelegate callback)
    {
        _delegates = (TDelegate)Delegate.Remove(_delegates, callback);
        if (_delegatesOnce != null)
            _delegatesOnce = (TDelegate)Delegate.Remove(_delegatesOnce, callback);
        // 从 handlesMap 中清理该回调的所有条目
        if (_handlesMap != null)
        {
            var toRemove = ListPool<int>.Get();
            foreach (var kv in _handlesMap)
            {
                if (ReferenceEquals(kv.Value, callback))
                    toRemove.Add(kv.Key);
            }
            foreach (var key in toRemove)
            {
                _handlesMap.Remove(key);
                _onceHandles?.Remove(key);
            }
            ListPool<int>.Release(toRemove);
        }
    }

    /// <summary>
    /// 通过句柄断开
    /// </summary>
    public bool Disconnect(int handle)
    {
        if (_handlesMap == null || !_handlesMap.TryGetValue(handle, out var callback))
            return false;

        _delegates = (TDelegate)Delegate.Remove(_delegates, callback);
        if (_delegatesOnce != null)
            _delegatesOnce = (TDelegate)Delegate.Remove(_delegatesOnce, callback);

        _handlesMap.Remove(handle);
        _onceHandles?.Remove(handle);
        return true;
    }

    /// <summary>
    /// 断开所有回调（包括持久和一次性）
    /// </summary>
    public void DisconnectAll()
    {
        _delegates = null;
        _delegatesOnce = null;
        _handlesMap?.Clear();
        _onceHandles?.Clear();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Invoke
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 调用一次性回调并清理其句柄映射。
    /// 子类在强类型 Invoke 中调用此方法。
    /// </summary>
    protected void InvokeOnceAndCleanup()
    {
        if (_onceHandles != null && _onceHandles.Count > 0)
        {
            foreach (var h in _onceHandles)
                _handlesMap?.Remove(h);
            _onceHandles.Clear();
        }
        _delegatesOnce = null;
    }

    // ═══════════════════════════════════════════════════════════════
    //  运算符重载
    // ═══════════════════════════════════════════════════════════════

    public static Signal<TDelegate> operator +(Signal<TDelegate> left, TDelegate callback)
    {
        left ??= new Signal<TDelegate>();
        left._delegates = (TDelegate)Delegate.Combine(left._delegates, callback);
        return left;
    }

    public static Signal<TDelegate> operator -(Signal<TDelegate> left, TDelegate callback)
    {
        if (left != null)
            left._delegates = (TDelegate)Delegate.Remove(left._delegates, callback);
        return left;
    }
}

/// <summary>
/// 无参数信号
/// </summary>
public class KSignal : Signal<Action>
{
    /// <summary>
    /// 调用信号，触发所有已注册的回调（强类型，零装箱）
    /// </summary>
    public new void Invoke()
    {
        _delegates?.Invoke();
        _delegatesOnce?.Invoke();
        InvokeOnceAndCleanup();
    }
}

/// <summary>
/// 单参数信号
/// </summary>
public class KSignal<T1> : Signal<Action<T1>>
{
    /// <summary>
    /// 调用信号（强类型，零装箱）
    /// </summary>
    public new void Invoke(T1 arg1)
    {
        _delegates?.Invoke(arg1);
        _delegatesOnce?.Invoke(arg1);
        InvokeOnceAndCleanup();
    }
}

/// <summary>
/// 双参数信号
/// </summary>
public class KSignal<T1, T2> : Signal<Action<T1, T2>>
{
    /// <summary>
    /// 调用信号（强类型，零装箱）
    /// </summary>
    public new void Invoke(T1 arg1, T2 arg2)
    {
        _delegates?.Invoke(arg1, arg2);
        _delegatesOnce?.Invoke(arg1, arg2);
        InvokeOnceAndCleanup();
    }
}

/// <summary>
/// 三参数信号
/// </summary>
public class KSignal<T1, T2, T3> : Signal<Action<T1, T2, T3>>
{
    /// <summary>
    /// 调用信号（强类型，零装箱）
    /// </summary>
    public new void Invoke(T1 arg1, T2 arg2, T3 arg3)
    {
        _delegates?.Invoke(arg1, arg2, arg3);
        _delegatesOnce?.Invoke(arg1, arg2, arg3);
        InvokeOnceAndCleanup();
    }
}

/// <summary>
/// 四参数信号
/// </summary>
[Serializable]
public class KSignal<T1, T2, T3, T4> : Signal<Action<T1, T2, T3, T4>>
{
    /// <summary>
    /// 调用信号（强类型，零装箱）
    /// </summary>
    public new void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        _delegates?.Invoke(arg1, arg2, arg3, arg4);
        _delegatesOnce?.Invoke(arg1, arg2, arg3, arg4);
        InvokeOnceAndCleanup();
    }
}

/// <summary>
/// 向后兼容的别名（已废弃，请使用 KSignal&lt;T1,T2,T3,T4&gt;）
/// </summary>
[Obsolete("Use KSignal<T1,T2,T3,T4> instead")]
public class KAction<T1, T2, T3, T4> : KSignal<T1, T2, T3, T4> { }
