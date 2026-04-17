using System;
using System.Collections.Generic;

/// <summary>
/// 信号接口，定义信号的基本操作
/// </summary>
public interface ISignal
{
    /// <summary>
    /// 调用信号，触发所有已注册的回调
    /// </summary>
    /// <param name="args">传递给回调的参数</param>
    public void Invoke(params object[] args);

    /// <summary>
    /// 添加回调委托到信号
    /// </summary>
    /// <param name="callback">要添加的回调委托</param>
    public void Add(Delegate callback);

    /// <summary>
    /// 从信号中移除回调委托
    /// </summary>
    /// <param name="callback">要移除的回调委托</param>
    public void Remove(Delegate callback);
}

/// <summary>
/// 泛型信号类，用于管理和触发委托回调
/// 支持持久回调和一次性回调
/// </summary>
/// <typeparam name="TDelegate">委托类型</typeparam>
public class Signal<TDelegate> : ISignal where TDelegate : Delegate
{
    /// <summary>
    /// 持久委托集合，每次 Invoke 时都会触发
    /// </summary>
    public TDelegate _delegates;
    
    /// <summary>
    /// 一次性委托集合，Invoke 后会被清空
    /// </summary>
    public TDelegate _delegatesOnce;
    
    /// <summary>
    /// 委托句柄映射表，用于通过 Guid 管理委托
    /// </summary>
    public Dictionary<Guid, TDelegate> _handlesMap = new();

    /// <summary>
    /// 调用信号，触发所有已注册的回调（包括持久回调和一次性回调）
    /// </summary>
    /// <param name="args">传递给回调的参数</param>
    public void Invoke(params object[] args)
    {
        _delegates?.DynamicInvoke(args);
        _delegatesOnce?.DynamicInvoke(args);
        _delegatesOnce = null; // 清空一次性委托
    }

    /// <summary>
    /// 添加持久回调委托
    /// </summary>
    /// <param name="callback">要添加的回调委托</param>
    public void Add(Delegate callback)
    {
        _delegates = (TDelegate)Delegate.Combine(_delegates, callback);
    }

    /// <summary>
    /// 移除持久回调委托
    /// </summary>
    /// <param name="callback">要移除的回调委托</param>
    public void Remove(Delegate callback)
    {
        _delegates = (TDelegate)Delegate.Remove(_delegates, callback);
    }

    /// <summary>
    /// 连接一个持久回调并返回句柄
    /// </summary>
    /// <param name="callback">要连接的回调委托</param>
    /// <returns>回调的唯一标识符（句柄）</returns>
    public Guid Connect(TDelegate callback)
    {
        var handle = Guid.NewGuid();
        if (callback is TDelegate castedCallback)
        {
            _delegates = (TDelegate)Delegate.Combine(_delegates, castedCallback);
        }
        _handlesMap[handle] = callback;
        return handle;
    }
    
    /// <summary>
    /// 连接一个一次性回调并返回句柄
    /// 该回调在第一次 Invoke 后会自动移除
    /// </summary>
    /// <param name="callback">要连接的回调委托</param>
    /// <returns>回调的唯一标识符（句柄）</returns>
    public Guid ConnectOnce(TDelegate callback)
    {
        var handle = Guid.NewGuid();
        if (callback is TDelegate castedCallback)
        {
            _delegatesOnce = (TDelegate)Delegate.Combine(_delegatesOnce, castedCallback);
        }
        _handlesMap[handle] = callback;
        return handle;
    }
    
    /// <summary>
    /// 通过订阅者连接回调
    /// </summary>
    /// <param name="subscriber">订阅者对象</param>
    /// <param name="callback">要连接的回调委托</param>
    /// <returns>回调的唯一标识符（句柄）</returns>
    public Guid Connect(Subscriber subscriber, TDelegate callback)
    {
        return subscriber.Subscribe(this, callback);
    }

    /// <summary>
    /// 断开指定的回调委托
    /// </summary>
    /// <param name="callback">要断开的回调委托</param>
    public void Disconnect(TDelegate callback)
    {
        if (callback is TDelegate castedCallback)
        {
            _delegates = (TDelegate)Delegate.Remove(_delegates, castedCallback);
        }
    }

    /// <summary>
    /// 通过句柄断开回调
    /// </summary>
    /// <param name="handle">回调的唯一标识符（句柄）</param>
    public void Disconnect(Guid handle)
    {
        var toRemove = _handlesMap[handle];
        _delegates = (TDelegate)Delegate.Remove(_delegates as TDelegate, toRemove);
    }

    /// <summary>
    /// 运算符重载：添加回调
    /// </summary>
    public static Signal<TDelegate> operator +(Signal<TDelegate> left, TDelegate callback)
    {
        if (left == null)
        {
            left = new Signal<TDelegate>();
        }

        left.Add(callback);
        return left;
    }

    /// <summary>
    /// 运算符重载：移除回调
    /// </summary>
    public static Signal<TDelegate> operator -(Signal<TDelegate> left, TDelegate callback)
    {
        left.Remove(callback);
        return left;
    }
}

/// <summary>
/// 无参数信号（继承自 Signal&lt;Action&gt;）
/// </summary>
public class KSignal : Signal<Action>
{
    /// <summary>
    /// 调用信号，触发所有已注册的无参数回调（强类型，避免DynamicInvoke）
    /// </summary>
    public new void Invoke()
    {
        _delegates?.Invoke();
        _delegatesOnce?.Invoke();
        _delegatesOnce = null;
    }
};

/// <summary>
/// 单参数信号（继承自 Signal&lt;Action&lt;T1&gt;&gt;）
/// </summary>
/// <typeparam name="T1">参数类型</typeparam>
public class KSignal<T1> : Signal<Action<T1>>
{
    /// <summary>
    /// 调用信号，触发所有已注册的单参数回调（强类型，避免DynamicInvoke）
    /// </summary>
    /// <param name="arg1">第一个参数</param>
    public new void Invoke(T1 arg1)
    {
        _delegates?.Invoke(arg1);
        _delegatesOnce?.Invoke(arg1);
        _delegatesOnce = null;
    }
};

/// <summary>
/// 双参数信号（继承自 Signal&lt;Action&lt;T1, T2&gt;&gt;）
/// </summary>
/// <typeparam name="T1">第一个参数类型</typeparam>
/// <typeparam name="T2">第二个参数类型</typeparam>
public class KSignal<T1, T2> : Signal<Action<T1, T2>>
{
    /// <summary>
    /// 调用信号，触发所有已注册的双参数回调（强类型，避免DynamicInvoke）
    /// </summary>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    public new void Invoke(T1 arg1, T2 arg2)
    {
        _delegates?.Invoke(arg1, arg2);
        _delegatesOnce?.Invoke(arg1, arg2);
        _delegatesOnce = null;
    }
};

/// <summary>
/// 三参数信号（继承自 Signal&lt;Action&lt;T1, T2, T3&gt;&gt;）
/// </summary>
/// <typeparam name="T1">第一个参数类型</typeparam>
/// <typeparam name="T2">第二个参数类型</typeparam>
/// <typeparam name="T3">第三个参数类型</typeparam>
public class KSignal<T1, T2, T3> : Signal<Action<T1, T2, T3>>
{
    /// <summary>
    /// 调用信号，触发所有已注册的三参数回调（强类型，避免DynamicInvoke）
    /// </summary>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    /// <param name="arg3">第三个参数</param>
    public new void Invoke(T1 arg1, T2 arg2, T3 arg3)
    {
        _delegates?.Invoke(arg1, arg2, arg3);
        _delegatesOnce?.Invoke(arg1, arg2, arg3);
        _delegatesOnce = null;
    }
};

/// <summary>
/// 四参数信号（继承自 Signal&lt;Action&lt;T1, T2, T3, T4&gt;&gt;）
/// </summary>
/// <typeparam name="T1">第一个参数类型</typeparam>
/// <typeparam name="T2">第二个参数类型</typeparam>
/// <typeparam name="T3">第三个参数类型</typeparam>
/// <typeparam name="T4">第四个参数类型</typeparam>
[Serializable]
public class KAction<T1, T2, T3, T4> : Signal<Action<T1, T2, T3, T4>>
{
    /// <summary>
    /// 调用信号，触发所有已注册的四参数回调（强类型，避免DynamicInvoke）
    /// </summary>
    /// <param name="arg1">第一个参数</param>
    /// <param name="arg2">第二个参数</param>
    /// <param name="arg3">第三个参数</param>
    /// <param name="arg4">第四个参数</param>
    public new void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        _delegates?.Invoke(arg1, arg2, arg3, arg4);
        _delegatesOnce?.Invoke(arg1, arg2, arg3, arg4);
        _delegatesOnce = null;
    }
};