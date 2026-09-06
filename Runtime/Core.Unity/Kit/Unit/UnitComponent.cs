using System;
using UnityEngine;
using UnityEngine.UI;

interface IUnitComponent
{
    public void Begin();

    public void Logic(float delta);

    public void End();

}


public class UnitComponent : MonoBehaviour, IUnitComponent
{
    [NonSerialized]
    public UnitBase Owner;

    /// <summary>
    /// 信号订阅管理器，在 End() 时自动清理
    /// </summary>
    protected Subscriber subscriber = new();

    protected void Awake()
    {
    }

    protected void Start()
    {
        Debug.Assert(Owner != null);
        Begin();
    }

    public virtual void Begin()
    {
        Debug.Assert(Owner);
    }

    public virtual void End()
    {
        Debug.Assert(Owner);
        subscriber.DisconnectAll();
    }

    public virtual void Logic(float delta)
    {
    }
    
    /// <summary>
    /// 当 Owner 单位 Spawn 时调用
    /// </summary>
    public virtual void OnOwnerSpawn()
    {
    }
    
    /// <summary>
    /// 当 Owner 单位 Die 时调用
    /// </summary>
    public virtual void OnOwnerDie()
    {
    }
}