using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KGameCore
{
    private static KGameCore mCore;

    public static KGameCore Instance
    {
        get
        {
            if (mCore == null)
            {
                mCore = new KGameCore();
                mCore.Init();
            }
            return mCore;
        }
    }

    protected KCommandQueue mCmdQueue = new KCommandQueue();

    public static T SystemAt<T>() where T : KSystem
    {
        return mCore.mSystem[typeof(T).Name] as T;
    }
    
    public T ImmeExecute<T>(T cmd) where T : KCommand
    {
        cmd.OnExecute();
        return cmd;
    }
    
    public KSystem AddSystem(KSystem system)
    {
        var name = system.GetType().Name;
        mSystem[name] = system;
        return system;
    }

    public KSystem GetSystem(string name)
    {
        return mSystem[name];
    }

    public T RequireSystem<T>(string name = null) where T : KSystem
    {
        if (name == null)
            name = typeof(T).Name;
        if (mSystem.ContainsKey(name))
            return mSystem[name] as T;
        else
        {
            var asset = KSystem.GetSystemPrefabAsset<T>();
            var inst = GameObject.Instantiate(asset);
        }
        return mSystem[name] as T;
    }

    public T GetSystem<T>() where T : KSystem
    {
        return mSystem[typeof(T).Name] as T;
    }

    public KGameCore()
    {
    }

    void Init()
    {
        DOTween.Init();
        mSystem = new Dictionary<string, KSystem>();
    }

    private Dictionary<string, KSystem> mSystem;
}