using System;
using UnityEngine;

[Serializable]
public class KEventBase
{
    protected Subscriber subscriber = new Subscriber();
    public virtual void InitEvent(GameObject owner, Action trigger)
    {

    }

    public virtual void DeInitEvent()
    {
        subscriber.DisconnectAll();
    }
}
