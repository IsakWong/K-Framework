using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Config;
using Framework.Debug;
using K1.Gameplay;
using UnityEngine;

public class GameMode : MonoBehaviour
{
    void Awake()
    {
        OnAwake();
    }

    public virtual void OnAwake()
    {
        KGameCore.Instance.RequireModule<DebugModule>();
        KGameCore.Instance.RequireModule<ConfigModule>();


        KGameCore.Instance.RequireModule<AudioModule>();

        KGameCore.Instance.RequireModule<UIModule>();

    }

    public virtual void OnModeBegin()
    {
    }

    public virtual void OnSwitchGameMode(GameMode newMode)
    {
    }

    public virtual void OnModeEnd()
    {
    }

    private void Start()
    {
        KGameCore.Instance.SwitchGameMode(this);
    }
}