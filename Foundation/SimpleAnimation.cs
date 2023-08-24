using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class SimpleAnimation : MonoBehaviour
{
    public bool mPlayOnAwake = true;
    public List<AnimationClip> clipsToPlay = new();
    public bool mLoop = false;
    private PlayableGraph playableGraph;

    void Start()
    {
    }

    void OnDisable()
    {
    }
}