using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataContainer
{
    bool GetData<T>(string name, out T t) where T : class;
    bool HasData<T>(string name);

    bool HasKey(string name);
    void SetData<T>(string name, T t);

}
/// <summary>
/// ����һ�������࣬��ͬ��KSystem����װж�����޷���̬ж�صģ������� Log���㲥��
/// </summary>
/// <typeparam name="T"></typeparam>
public class KSingleton<T> where T : KSingleton<T>, new()
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }
            return instance;
        }
    }

    protected KSingleton()
    {
        // Do nothing
    }
}
/// <summary>
/// ����һ������ģ�飬��ģ����Զ�̬װж������Gameplay�������System���Ծֹ�������������
/// </summary>
/// <typeparam name="T"></typeparam>
public class KSystem : MonoBehaviour
{

    protected EventDispatcher<string> _systemDispatcher = new EventDispatcher<string>();
    public EventDispatcher<string> Dispatcher
    {
        get => _systemDispatcher;
    }

    public static GameObject GetSystemPrefabAsset<T>() where T : KSystem
    {
        var asset = AssetSystem.Instance.LoadAsset<GameObject>(String.Format("Assets/Gameplay/Framework/[{0}].prefab", typeof(T).Name));
        return asset;
    }

    private Dictionary<string, Variable> mVars = new Dictionary<string, Variable>();

    protected void Awake()
    {
        DontDestroyOnLoad(this);
        KGameCore.Instance.AddSystem(this);
    }


    protected virtual void Update()
    {
    }

    protected virtual void Shutdown()
    {
    }

    public T GetData<T>(string name)
    {
        throw new System.NotImplementedException();
    }
}

public class CoreBehaviour : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        var instace = KGameCore.Instance;
    }
}