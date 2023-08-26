
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Analytics;

public class EventDispatcher<EventID>
{
    public Dictionary<EventID, Delegate> mSenders = new Dictionary<EventID, Delegate>();

    public void AddListener(EventID message, Action d)
    {
        if (!mSenders.ContainsKey(message))
            mSenders.Add(message, d);
        else
            mSenders[message] = (Action)mSenders[message] + (Action)d;
    }

    public void RemoveListener(EventID message, Action d)
    {
        if (!mSenders.ContainsKey(message))
            return;
        mSenders[message] = (Action)mSenders[message] - d;
    }

    public void AddListener<T>(EventID message, Action<T> d)
    {
        if (!mSenders.ContainsKey(message))
            mSenders.Add(message, d);
        else
            mSenders[message] = (Action<T>)mSenders[message] + (Action<T>)d;
    }

    public void RemoveListener<T>(EventID message, Action<T> d)
    {
        if (!mSenders.ContainsKey(message))
            return;
        mSenders[message] = (Action<T>)mSenders[message] - d;
    }

    public void AddListener<T1, T2>(EventID message, Action<T1, T2> d)
    {
        if (!mSenders.ContainsKey(message))
            mSenders.Add(message, d);
        else
            mSenders[message] = (Action<T1, T2>)mSenders[message] + d;
    }

    public void RemoveListener<T1, T2>(EventID message, Action<T1, T2> d)
    {
        if (!mSenders.ContainsKey(message))
            return;
        mSenders[message] = (Action<T1, T2>)mSenders[message] - d;
    }

    public void AddListener<T1, T2, T3>(EventID message, Action<T1, T2, T3> d)
    {
        if (!mSenders.ContainsKey(message))
            mSenders.Add(message, d);
        else
            mSenders[message] = (Action<T1, T2, T3>)mSenders[message] + (Action<T1, T2, T3>)d;
    }
    public void RemoveListener<T1, T2, T3>(EventID message, Action<T1, T2, T3> d)
    {
        if (!mSenders.ContainsKey(message))
            return;
        mSenders[message] = (Action<T1, T2, T3>)mSenders[message] - d;
    }

    public void Emit(EventID name)
    {
        if (!mSenders.ContainsKey(name))
            return;
        Action d = (Action)mSenders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke();
    }
    public void Emit<T>(EventID name, T data)
    {
        if (!mSenders.ContainsKey(name))
            return;
        Action<T> d = (Action<T>)mSenders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke(data);
    }
    public void Emit<T1, T2>(EventID name, T1 data, T2 data2)
    {
        if (!mSenders.ContainsKey(name))
            return;
        Action<T1, T2> d = (Action<T1, T2>)mSenders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke(data, data2);
    }
}

public static class MessageManager
{

    public static Dictionary<string, Delegate> Senders = new Dictionary<string, Delegate>();

    public static void AddListener(this MonoBehaviour behaviour, string message, Action d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action)Senders[message] + (Action)d;
    }

    public static void RemoveListener(this MonoBehaviour behaviour, string message, Action d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action)Senders[message] - d;
    }

    public static void AddListener<T>(this MonoBehaviour behaviour, string message, Action<T> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T>)Senders[message] + (Action<T>)d;
    }

    public static void RemoveListener<T>(this MonoBehaviour behaviour, string message, Action<T> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T>)Senders[message] - d;
    }

    public static void AddListener<T1, T2>(this MonoBehaviour behaviour, string message, Action<T1, T2> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T1, T2>)Senders[message] + d;
    }

    public static void RemoveListener<T1, T2>(this MonoBehaviour behaviour, string message, Action<T1, T2> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T1, T2>)Senders[message] - d;
    }

    public static void AddListener<T1, T2, T3>(this MonoBehaviour behaviour, string message, Action<T1, T2, T3> d)
    {
        if (!Senders.ContainsKey(message))
            Senders.Add(message, d);
        else
            Senders[message] = (Action<T1, T2, T3>)Senders[message] + (Action<T1, T2, T3>)d;
    }
    public static void RemoveListener<T1, T2, T3>(this MonoBehaviour behaviour, string message, Action<T1, T2, T3> d)
    {
        if (!Senders.ContainsKey(message))
            return;
        Senders[message] = (Action<T1, T2, T3>)Senders[message] - d;
    }

    public static void Emit(string name)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action d = (Action)Senders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke();
    }
    public static void Emit<T>(string name, T data)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action<T> d = (Action<T>)Senders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke(data);
    }
    public static void Emit<T1, T2>(string name, T1 data, T2 data2)
    {
        if (!Senders.ContainsKey(name))
            return;
        Action<T1, T2> d = (Action<T1, T2>)Senders[name];
        EnhancedLog.Log("Emit Message: " + name);
        if (d != null)
            d.Invoke(data, data2);
    }

}