using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(GameCoreProxy.GameModeOrder - 10)]
public class InstanceBehaviour<T> : MonoBehaviour where T : Component
{
    public static T Instance;
    // Start is called before the first frame update
    protected virtual void OnReplace()
    {
        Instance.transform.position = transform.position;
        Instance.transform.rotation = transform.rotation;
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child != null)
            {
                child.SetParent(Instance.transform);
            }
        }
        gameObject.SetActive(false);
    }

    protected void Awake()
    {
        if (Instance == null)
        {
            Instance = GetComponent<T>();
            DontDestroyOnLoad(gameObject);
            transform.SetParent(null);
        }
        else
        {
            OnReplace();
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }
}