using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 管理服务接口
/// 提供 UI 面板的创建、入栈/出栈、显示/隐藏、销毁等管理能力
/// </summary>
public interface IUIService
{
    // ─── 面板生命周期 ───

    T RequireUI<T>(bool defaultActive = true) where T : UIPanel;
    UIPanel RequireUI(Type type);
    T PushUI<T>(bool clearStack = false) where T : UIPanel;
    UIPanel PushUI(UIPanel panel, bool clearStack = false);
    void PopUI(UIPanel panel = null, bool clearStack = false);
    T PopUI<T>(bool clearStack = false) where T : UIPanel;
    void PopAll();
    void PopFullScreenUI(bool clearStack);
    T PopFullScreenUI<T>(bool clearStack = false) where T : UIPanel;

    // ─── 关闭 / 销毁 ───

    void CloseUI<T>() where T : UIPanel;
    void CloseUI(UIPanel panel);
    void DestroyUI<T>() where T : UIPanel;
    void DestroyUI(UIPanel panel);
    void DestroyAllUI();

    // ─── 显示 / 隐藏 ───

    void HideAll();
    void ShowAll();

    // ─── 查询 ───

    T GetUI<T>() where T : class;
    bool IsUIVisible<T>() where T : UIPanel;
    bool IsUIVisible(UIPanel panel);
    int GetVisiblePanelCount();
    bool HasAnyPanelVisible();
    UIPanel GetTopmostFullscreenPanel();
    UIPanel GetTopmostPanel();
    UIPanel GetBottomPanel();
    List<UIPanel> GetAllPanels();
    List<T> GetAllPanels<T>() where T : UIPanel;
    LinkedList<UIPanel> VisibleStack { get; }
    Canvas OverlayCanvas { get; }

    // ─── 层级调整 ───

    void BringToFront(UIPanel panel);
    void SendToBack(UIPanel panel);
}
