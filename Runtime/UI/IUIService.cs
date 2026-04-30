using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UI 管理服务接口。
///
/// 设计要点：
/// - 栈式 Fullscreen + 共存 Overlay
/// - Push 入容器并 Open；BringToFront 已在容器则切到前台（Fullscreen 触发 Suspend/Resume）
/// - Close 退出容器并触发 Close 生命周期；级联子面板
/// - 所有"打开/关闭/置前"操作通过此接口，UIPanel 自身不暴露这些 API
/// </summary>
public interface IUIService
{
    // ─── 加载（独立、便于预热） ───

    UniTask<T> RequireAsync<T>(string path = null) where T : UIPanel;
    UniTask<UIPanel> RequireAsync(Type type, string path = null);

    // ─── 入容器 + Open ───

    /// <summary>加载 + 入容器 + Open。若已在容器内，转交给 BringToFrontAsync。</summary>
    UniTask<T> PushAsync<T>(UIPanel parent = null) where T : UIPanel;
    UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null);

    // ─── 切到前台 ───

    /// <summary>
    /// 已在容器内的 Panel 切到前台。
    /// Fullscreen：当前栈顶按 KeepAliveOnSuspend 走 Suspend 或 Close，自己移到栈顶并 Resume/Open。
    /// Overlay：仅 SetAsLastSibling。
    /// </summary>
    UniTask BringToFrontAsync(UIPanel panel);
    UniTask BringToFrontAsync<T>() where T : UIPanel;

    // ─── 关闭 ───

    /// <summary>关闭指定 Panel（null = 当前栈顶 Fullscreen）。会级联关闭所有子面板。</summary>
    UniTask CloseAsync(UIPanel panel = null);

    /// <summary>关闭第一个类型匹配的 Panel。</summary>
    UniTask<T> CloseAsync<T>() where T : UIPanel;

    /// <summary>关闭栈顶 Fullscreen（保留 Overlay）。</summary>
    UniTask CloseTopFullscreenAsync();

    /// <summary>关闭所有 Panel（包括 Overlay）。</summary>
    UniTask CloseAllAsync();

    // ─── 销毁 ───

    UniTask DestroyAsync<T>() where T : UIPanel;
    UniTask DestroyAsync(UIPanel panel);
    UniTask DestroyAllAsync();

    // ─── 只读查询（同步） ───

    T GetUI<T>() where T : class;

    /// <summary>Panel 已在容器内（栈或 Overlay 列表）且 Visible。</summary>
    bool IsOpen<T>() where T : UIPanel;
    bool IsOpen(UIPanel panel);

    /// <summary>Panel 已实例化（在 _uiPanels 列表中），不要求在容器内。</summary>
    bool IsLoaded<T>() where T : UIPanel;
    bool IsLoaded(UIPanel panel);

    int GetVisiblePanelCount();
    bool HasAnyPanelVisible();

    UIPanel GetTopFullscreen();
    UIPanel GetTopPanel();
    UIPanel GetBottomPanel();

    IReadOnlyCollection<UIPanel> GetFullscreenStack();
    IReadOnlyList<UIPanel> GetOverlays();

    List<UIPanel> GetAllPanels();
    List<T> GetAllPanels<T>() where T : UIPanel;

    Canvas OverlayCanvas { get; }
}
