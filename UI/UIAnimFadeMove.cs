using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public enum AnimDirection
{
    Top,
    Bottom,
    Left,
    Right
}

public class UIAnimFadeMove : UIAnimBase
{
    [FormerlySerializedAs("mMoveRoot")] public RectTransform mRootTransform;

    public float YMovement = -100;
    public AnimDirection mAnimDirection = AnimDirection.Top;

    private Vector2 mTargetAnchoredPosition;

    private float mStartLocation;

    // Start is called before the first frame update
    public bool HideWhenAwake = true;

    private void Awake()
    {
        if (mRootTransform is null)
            mRootTransform = this.GetComponent<RectTransform>();
        if (HideWhenAwake)
            mRootTransform.gameObject.SetActive(false);
        mTargetAnchoredPosition = mRootTransform.anchoredPosition3D;
        //Hide();
    }

    public void SetTargetAnchoredPosition(Vector2 target)
    {
        mTargetAnchoredPosition = target;
    }

    private Sequence _sequence;

    public override Sequence Show(float delta = 0.3f, float lifetime = -1f)
    {
        mRootCanvasGroup.interactable = true;
        gameObject.SetActive(true);
        if (_sequence != null)
        {
            _sequence.IsActive();
            _sequence.Kill();
        }

        mRootCanvasGroup.DOFade(1.0f, delta);

        var start = mTargetAnchoredPosition;
        switch (mAnimDirection)
        {
            case AnimDirection.Top:
                start.y -= YMovement;
                break;
            case AnimDirection.Bottom:
                start.y += YMovement;
                break;
            case AnimDirection.Left:
                start.x += YMovement;
                break;
            case AnimDirection.Right:
                start.x -= YMovement;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _sequence = DOTween.Sequence();
        mRootTransform.gameObject.SetActive(true);

        mRootTransform.anchoredPosition = start;
        _sequence.Append(mRootTransform.DOAnchorPos(mTargetAnchoredPosition, delta).SetEase(Ease.InOutQuad));
        if (lifetime > -1f)
        {
            _sequence.AppendInterval(lifetime);
            _sequence.Append(mRootCanvasGroup.DOFade(0.0f, delta));
        }

        _sequence.Play();
        return _sequence;
    }

    public override void Hide(float delta = 0.3f)
    {
        mRootCanvasGroup.interactable = false;
        if (_sequence != null)
        {
            _sequence.IsActive();
            _sequence.Kill();
        }

        _sequence = DOTween.Sequence();
        _sequence.Append(mRootCanvasGroup.DOFade(0.0f, delta));
        _sequence.AppendCallback(() => { mRootTransform.gameObject.SetActive(false); });
    }
}