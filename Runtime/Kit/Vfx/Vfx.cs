using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;


[DisallowMultipleComponent]
public class Vfx : MonoBehaviour, IPoolable
{
    public float mLifeTime = 1;
    public List<Transform> ParticleParents = new();
    public Action<Vfx> EventSpawn;
    public Action EventDestroy;
    public static HashSet<Vfx> AllVfx = new();
    public float UnityDestroyDelay = 2.0f;

    private float _remainTime = 0.0f;
    private bool alive = true;

    
    public void Die()
    {
        if (!alive)
        {
            return;
        }

        Debug.Assert(alive);
        alive = false;
        var anim = GetComponent<VfxAnim>();
        if (anim)
        {
            anim.Die();
            if (anim.DieDuration > UnityDestroyDelay)
            {
                Debug.Assert(false);
            }
        }

        Invoke("Release", UnityDestroyDelay);
    }

    private void Awake()
    {
        var audioClip = GetComponent<RandomAudioClip>();
    }

    public void Release()
    {
        VfxManager.Instance.Release(this);
    }

    private void FixedUpdate()
    {
        if (mLifeTime > 0 && _remainTime > mLifeTime && alive)
        {
            Die();
        }

        _remainTime += KTime.scaleDeltaTime;
    }

    protected void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Effect");
    }

    public void OnReturnToPool()
    {
        gameObject.SetActive(false);
        EventDestroy?.Invoke();
        AllVfx.Remove(this);
    }

    public void OnGetFromPool()
    {
        alive = true;
        gameObject.SetActive(true);
        _remainTime = 0.0f;
        var anim = GetComponent<VfxAnim>();
        if (anim)
        {
            anim.Spawn();
        }

        AllVfx.Add(this);
        EventSpawn?.Invoke(this);
    }

    public SerializedDictionary<string, Variant> Datas = new();
}