using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerModule : TModule<PlayerModule>
{

    public ControllerBase LocalPlayerController;

    public T GetLocalPlayerController<T>() where T : ControllerBase
    {
        return LocalPlayerController as T;
    }

    public List<ControllerBase> ControllerList = new();

    public PlayerInput GetLocalPlayerInput()
    {
        return LocalPlayerController.GetLocalPlayerInput();
    }

    public void RegisterController(ControllerBase value, bool enable)
    {
        if (enable)
        {
            ControllerList.Add(value);
        }
        else
        {
            ControllerList.Remove(value);
        }
    }
    
    public void SwitchInputMode(string inputMode)
    {
        LocalPlayerController.GetLocalPlayerInput().SwitchCurrentActionMap(inputMode);
    }

    public void SwitchController(ControllerBase controller)
    {
        if(LocalPlayerController)
            LocalPlayerController.OnControllerDisable();
        LocalPlayerController = controller;
        if(LocalPlayerController)
            LocalPlayerController.OnControllerEnable();
    }

    public void FixedUpdate()
    {
        foreach (var controller in ControllerList)
        {
            controller.OnLogic();
        }

    }
}