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
    }

    public virtual void Logic(float delta)
    {
    }
}