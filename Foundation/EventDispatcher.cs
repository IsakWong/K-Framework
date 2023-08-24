using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Analytics;

public class EventDispatcher<EventID>
{
    public Dictionary<EventID, Delegate> Senders = new Dictionary<EventID, Delegate>();

    public void AddListener(EventID message, Action d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action)Senders[message] + (Action)d;
    }

    public void RemoveListener(EventID message, Action d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action)Senders[message] - d;
    }

    public void AddListener<T>(EventID message, Action<T> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T>)Senders[message] + (Action<T>)d;
    }

    public void RemoveListener<T>(EventID message, Action<T> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T>)Senders[message] - d;
    }

    public void AddListener<T1, T2>(EventID message, Action<T1, T2> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T1, T2>)Senders[message] + d;
    }

    public void RemoveListener<T1, T2>(EventID message, Action<T1, T2> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T1, T2>)Senders[message] - d;
    }

    public void AddListener<T1, T2, T3>(EventID message, Action<T1, T2, T3> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T1, T2, T3>)Senders[message] + (Action<T1, T2, T3>)d;
    }

    public void RemoveListener<T1, T2, T3>(EventID message, Action<T1, T2, T3> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T1, T2, T3>)Senders[message] - d;
    }

    public void Emit(EventID name)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action d = (Action)Senders[name];
        if (d != null)
            d.Invoke();
    }

    public void Emit<T>(EventID name, T data)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action<T> d = (Action<T>)Senders[name];
        if (d != null)
            d.Invoke(data);
    }

    public void Emit<T1, T2>(EventID name, T1 data, T2 data2)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action<T1, T2> d = (Action<T1, T2>)Senders[name];
        if (d != null)
            d.Invoke(data, data2);
    }
}