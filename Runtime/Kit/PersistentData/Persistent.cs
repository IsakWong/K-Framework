

using MoreMountains.Tools;
using System;
using System.Linq;
using UnityEngine;

public interface IPersistentTarget
{

    /// <summary>
    /// Returns a savable string containing the object's data
    /// </summary>
    /// <returns></returns>
    string OnSave();

    /// <summary>
    /// Loads the object's data from the passed string and applies it to its properties
    /// </summary>
    /// <param name="data"></param>
    void OnLoad(string data);
}

public interface IPersistent
{
    /// <summary>
    /// Needs to return a unique Guid used to identify this object 
    /// </summary>
    /// <returns></returns>
    string GetGuid();

    /// <summary>
    /// Returns a savable string containing the object's data
    /// </summary>
    /// <returns></returns>
    string OnSave();

    /// <summary>
    /// Loads the object's data from the passed string and applies it to its properties
    /// </summary>
    /// <param name="data"></param>
    void OnLoad(string data);


    void SetSave(bool val);

    /// <summary>
    /// Whether or not this object should be saved
    /// </summary>
    /// <returns></returns>
    bool ShouldBeSaved();
}


/// <summary>
/// 帮助持久化数据的组件
/// </summary>
class Persistent : MonoBehaviour, IPersistent
{

    public virtual void SetGuid(string newGUID) => _guid = newGUID;

    [Header("ID")]
    /// an optional suffix to add to the GUID, to make it more readable
    [Tooltip("an optional suffix to add to the GUID, to make it more readable")]
    public string UniqueIDSuffix;

    /// <summary>
    /// Generates a unique ID for the object, using the scene name, the object name, and a GUID
    /// </summary>
    /// <returns></returns>
    public virtual string GenerateGuid()
    {
        string newGuid = Guid.NewGuid().ToString();

        string guid =
            this.gameObject.scene.name
            + "-"
            + this.gameObject.name
            + "-"
            + newGuid;

        if (!string.IsNullOrEmpty(UniqueIDSuffix))
        {
            guid += "-" + UniqueIDSuffix;
        }

        this.SetGuid(guid);

        return guid;
    }

    [SerializeField]
    private string _guid;
    /// <summary>
    /// On validate, we make sure the object gets a valid GUID
    /// </summary>
    protected virtual void OnValidate()
    {
        ValidateGuid();
    }

    /// <summary>
    /// Checks if the object's ID is unique or not
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public virtual bool GuidIsUnique(string guid)
    {
        return Resources.FindObjectsOfTypeAll<MMPersistentBase>().Count(x => x.GetGuid() == guid) == 1;
    }

    public virtual void ValidateGuid()
    {
        if (!this.gameObject.scene.IsValid())
        {
            _guid = string.Empty;
            return;
        }

        int maxCount = 1000;
        int i = 0;

        while ((string.IsNullOrEmpty(_guid) || !GuidIsUnique(_guid)) && (i < maxCount))
        {
            GenerateGuid();
            i++;
        }

        if (i == maxCount)
        {
            Debug.LogWarning(this.gameObject.name + " couldn't generate a unique GUID after " + maxCount + " tries, you should probably change its UniqueIDSuffix");
        }
    }
    // 持久化
    public string GetGuid()
    {
        return _guid;
    }

    public string OnSave()
    {

        var target = GetComponent<IPersistentTarget>();
        if (target != null)
            return target.OnSave();
        return "";
    }

    public void OnLoad(string data)
    {
        var target = GetComponent<IPersistentTarget>();
        if(target != null)
            target.OnLoad(data);
    }

    public void SetSave(bool val)
    {
        _saveEnable = val;
    }

    private bool _saveEnable = false;

    public bool ShouldBeSaved()
    {
        return _saveEnable;
    }
}