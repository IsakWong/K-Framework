using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class UIAnimBase : MonoBehaviour
{
    public CanvasGroup mRootCanvasGroup;

    public virtual Sequence Show(float delta = 0.3f, float lifetime = -1f)
    {
        return null;
    }

    public virtual void Hide(float delta = 0.3f)
    {
    }
}