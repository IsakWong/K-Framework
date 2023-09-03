using System;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCoreProxy : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        var instace = KGameCore.Instance;
    }
}