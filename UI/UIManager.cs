using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using K1.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


public class UIManager : KSingleton<UIManager>
{

    private List<UIPanel> _uiPanels = new();

    public UIModule UIModule;

    public T ShowUI<T>(GameObject prefab, bool defaultVisible = true) where T : UIPanel
    {
        var instance = GameObject.Instantiate(prefab);
        T t = instance.GetComponent<T>();
        _uiPanels.Add(t as UIPanel);
        t.transform.SetParent(UIModule.OverlayCanvas.transform, false);
        t.gameObject.SetActive(defaultVisible);
        return t;
    }

    public void AddUI<T>(T t) where T : UIPanel
    {
        _uiPanels.Add(t as UIPanel);
    }

    public T ShowUI<T>(bool defaultVisible = true) where T : UIPanel
    {
        GameObject prefab = null;
        var ui = GetUI<T>();
        if (ui != null)
            return ui;

        prefab = AssetManager.Instance.LoadAsset<GameObject>($"Assets/Resources/UI/{typeof(T).Name}.prefab");

        var instance = GameObject.Instantiate(prefab);
        T t = instance.GetComponent<T>();
        _uiPanels.Add(t as UIPanel);
        t.transform.SetParent(UIModule.OverlayCanvas.transform, false);
        t.gameObject.SetActive(defaultVisible);
        return t;
    }

    public T GetUI<T>() where T : class
    {
        foreach (var panel in _uiPanels)
        {
            T t = panel as T;
            if (t is not null)
                return t;
        }

        return null;
    }

    public void RegisterPanel(UIPanel panel)
    {
        if (!_uiPanels.Contains(panel))
            _uiPanels.Add(panel);
    }
}