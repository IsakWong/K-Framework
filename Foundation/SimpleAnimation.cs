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
        // 使用AnimationPlayableUtilities中的方法简化代码
        if (mPlayOnAwake)
            Play();
    }


    public void Play()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();

        playableGraph = PlayableGraph.Create();

        var playQueuePlayable = ScriptPlayable<PlayQueuePlayable>.Create(playableGraph);

        var playQueue = playQueuePlayable.GetBehaviour();
        playQueue.mLoop = mLoop;
        playQueue.Initialize(clipsToPlay, playQueuePlayable, playableGraph);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", GetComponent<Animator>());

        playableOutput.SetSourcePlayable(playQueuePlayable);
        playableOutput.SetSourceInputPort(0);

        playableGraph.Play();
    }

    void OnDisable()
    {
        if (playableGraph.IsValid())
        {
            // 销毁所有的Playables和PlayableOutputs
            playableGraph.Destroy();
        }
    }
}