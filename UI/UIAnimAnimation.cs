using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using K1.Gameplay;
using TMPro;
using UnityEngine;

public class UIAnimAnimator : UIAnimBase
{
    public AnimationClip showClip;
    public AnimationClip hideClip;

    public SimpleAnimation mAnimation;

    // Start is called before the first frame update
    private void Awake()
    {
        mAnimation = this.GetOrAddComponent<SimpleAnimation>();
        mAnimation.mPlayOnAwake = false;
    }

    private Sequence _sequence;

    public override Sequence Show(float delta = 0.3f, float lifetime = -1f)
    {
        if (showClip)
        {
            mAnimation.clipsToPlay.Clear();
            mAnimation.clipsToPlay.Add(showClip);
            mAnimation.Play();
        }

        return null;
    }

    public override void Hide(float delta = 0.3f)
    {
        if (hideClip)
        {
            mAnimation.clipsToPlay.Add(hideClip);
            mAnimation.Play();
        }
    }
}