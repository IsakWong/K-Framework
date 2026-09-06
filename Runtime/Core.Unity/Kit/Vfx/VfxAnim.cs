using UnityEngine;
using UnityEngine.Serialization;

public class VfxAnim : MonoBehaviour
{
    /// <summary>
    /// LoopDelay后进行Loop的调用
    /// </summary>
    public float mLoopDelay = 0.0f;

    public bool mAutoLoop = true;
    [FormerlySerializedAs("mDieDuration")] public float DieDuration = 0.3f;
    public float SpawnDuration = 0.2f;

    protected bool alive = true;

    /// <summary>
    /// 播放创建动画
    /// </summary>
    public virtual void Spawn()
    {
    }
    
    /// <summary>
    /// 播放死亡
    /// </summary>
    public virtual void Die()
    {
        Debug.Assert(alive);
        alive = false;
    }
}