// Copyright (c) 2025 NetEase Inc. All rights reserved.
// Author: isakwong
// Date: 2025/10/11

using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDBase : MonoBehaviour
{
    public Transform Target;
    public Vector3 Offset;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private RectTransform _parentRectTransform;
    private Camera _mainCamera;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = CanvasInstance.Instance.BehaviourInstance;
        Debug.Assert(_canvas);
        _parentRectTransform = transform.parent as RectTransform;
        _mainCamera = Camera.main;
        RefreshPosition();
    }

    private void RefreshPosition()
    {
        if (Target == null || _rectTransform == null || _canvas == null)
        {
            if (_rectTransform != null) _rectTransform.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (!_rectTransform.gameObject.activeSelf) _rectTransform.gameObject.SetActive(true);
        }

        Vector3 worldPos = Target.position + Offset;
        Vector3 screenPos = Vector3.zero;
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenPos = _mainCamera != null ? _mainCamera.WorldToScreenPoint(worldPos) : worldPos;
        }
        else if (_canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            screenPos = _mainCamera != null ? _mainCamera.WorldToScreenPoint(worldPos) : worldPos;
        }
        else // World Space
        {
            screenPos = worldPos;
        }
        // 屏幕坐标转UI坐标
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            screenPos,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCamera,
            out uiPos);
        _rectTransform.anchoredPosition = uiPos;
    }

    private void LateUpdate()
    {
        RefreshPosition();
    }
    
    public static HUDBase Spawn(GameObject origin, Transform target)
    {
        HUDBase hud = null;
        GameObject inst = Instantiate(origin, CanvasInstance.Instance.HUDParent);
        hud = inst.GetComponent<HUDBase>();
        if (!hud)
            inst.AddComponent<HUDBase>();
        hud.Target = target;
        return hud;
    }
}