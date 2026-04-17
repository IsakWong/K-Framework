using System;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CanvasInstance : InstanceBehaviour<CanvasInstance>
{
    public Canvas BehaviourInstance => GetComponent<Canvas>();

    public RectTransform HUDParent;

    protected void Awake()
    {
        if (Instance is null)
        {
            var hudParentGameObject = new GameObject("[HUDParent]", typeof(RectTransform));
            HUDParent = hudParentGameObject.GetComponent<RectTransform>();
            HUDParent.sizeDelta = new Vector2(0, 0);
            HUDParent.anchorMin = Vector2.zero;
            HUDParent.anchorMax = Vector2.one;
            HUDParent.localScale = Vector3.one;
        }
        base.Awake();
    }
    protected override void OnReplace()
    {
        var prevInstance = Instance;
        prevInstance.BehaviourInstance.worldCamera  = BehaviourInstance.worldCamera;
        prevInstance.BehaviourInstance.renderMode = BehaviourInstance.renderMode;
        base.OnReplace();
    }
}