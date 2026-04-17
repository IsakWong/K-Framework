using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerBase : MonoBehaviour
{
    protected CommandQueue controllerCmdQueue = new();
    public bool LogCommand = false;

    public void PushCommand(ICommand cmd)
    {
        if (enabled)
        {
            controllerCmdQueue.PushCmd(cmd);
        }
    }

    protected void OnEnable()
    {
        PlayerModule.Instance.RegisterController(this, true);
        controllerCmdQueue.Queue.Clear();
        //KGameCore.RequireSystem<GameplayModule>().RegisterController(this, true);
    }

    public virtual void OnControllerEnable()
    {

    }

    public virtual void OnControllerDisable()
    {

    }

    protected void OnDisable()
    {
        if (!PlayerModule.NullablInstance)
        {
            return;
        }

        PlayerModule.Instance.RegisterController(this, false);
        controllerCmdQueue.Queue.Clear();
        //KGameCore.RequireSystem<GameplayModule>().RegisterController(this, false);
    }

    public virtual void OnLogic()
    {
        controllerCmdQueue.LogCommand = LogCommand;
        controllerCmdQueue.ProcessOnce();
    }

    public virtual bool IsLocalPlayerController()
    {
        return false;
    }

    public virtual PlayerInput GetLocalPlayerInput()
    {
        if (!IsLocalPlayerController())
        {
            return null;
        }
        return GetComponent<PlayerInput>();
    }
}