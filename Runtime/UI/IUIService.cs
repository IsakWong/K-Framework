using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UI 管理服务接口
/// 栈式 Fullscreen + 共存 Overlay + 父子级联 + UniTask 异步
/// </summary>
public interface IUIService
{
    // ─── 资源加载（独立，便于预热） ───

    UniTask<T> RequireUIAsync<T>() where T : UIPanel;
    UniTask<UIPanel> RequireUIAsync(Type type);

    // ─── 入栈 / 出栈 ───

    UniTask<T> PushUIAsync<T>(UIPanel parent = null) where T : UIPanel;
    UniTask<UIPanel> PushUIAsync(UIPanel panel, UIPanel parent = null);

    /// <summary>Pop 指定 Panel（null = 当前栈顶 Fullscreen）。会级联 Pop 所有子面板。</summary>
    UniTask PopUIAsync(UIPanel panel = null);

    /// <summary>Pop 第一个类型匹配的 Panel。</summary>
    UniTask<T> PopUIAsync<T>() where T : UIPanel;

    /// <summary>Pop 栈顶 Fullscreen（保留 Overlay）。</summary>
    UniTask PopFullscreenAsync();

    /// <summary>Pop 所有 Panel（包括 Overlay）。</summary>
    UniTask PopAllAsync();

    /// <summary>等价于 PopUIAsync(panel)。</summary>
    UniTask CloseUIAsync(UIPanel panel);

    // ─── 销毁 ───

    UniTask DestroyUIAsync<T>() where T : UIPanel;
    UniTask DestroyUIAsync(UIPanel panel);
    UniTask DestroyAllUIAsync();

    // ─── 只读查询（同步） ───

    T GetUI<T>() where T : class;
    bool IsUIVisible<T>() where T : UIPanel;
    bool IsUIVisible(UIPanel panel);
    int GetVisiblePanelCount();
    bool HasAnyPanelVisible();

    UIPanel GetTopmostFullscreen();
    UIPanel GetTopmostPanel();
    UIPanel GetBottomPanel();

    IReadOnlyCollection<UIPanel> GetFullscreenStack();
    IReadOnlyList<UIPanel> GetOverlays();

    List<UIPanel> GetAllPanels();
    List<T> GetAllPanels<T>() where T : UIPanel;

    Canvas OverlayCanvas { get; }
}
