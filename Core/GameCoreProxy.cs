using System;
using AYellowpaper.SerializedCollections;
using K1;
using UnityEngine;

public class GameCoreProxy : MonoBehaviour
{
    public SerializedDictionary<string, GameObject> ModulePrefab = new SerializedDictionary<string, GameObject>();
    //public SerializeType<KModule> et;

    void Awake()
    {
        var instace = KGameCore.Instance;
        if (instace.proxy is not null)
            Destroy(instace.proxy.gameObject);
        instace.SetProxy(this);
        DontDestroyOnLoad(this);
    }

    void Start()
    {
    }

    private void FixedUpdate()
    {
        KGameCore.Instance.OnLogic();
    }

    private void OnDrawGizmos()
    {
        KGizmos.Instance.OnDrawGizmos();
    }
}