using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace KFramework
{
/// <summary>
/// UI 管理服务接口。
///
/// 设计要点：
/// - 四层：Hud（常驻集合）/ Window（栈）/ Popup（叠层）/ Service（全局覆盖，CloseAll 不清）
/// - 层间层级由 Canvas sortingOrder 固定，层内顺序才由 Push 决定
/// - Push 入对应层并 Open；BringToFront 已在容器则切到前台（Window 触发 Suspend/Resume）
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
    UniTask<T> PushAsync<T>(UIPanel parent = null, Action<T> configure = null) where T : UIPanel;
    UniTask<UIPanel> PushAsync(UIPanel panel, UIPanel parent = null);

    /// <summary>
    /// 同时 Push host 与 companion 面板。
    /// Companion 的开关动画与 host 并行播放；host 关闭时 companion 自动并行关闭。
    /// </summary>
    UniTask PushWithCompanionsAsync(UIPanel host, params UIPanel[] companions);

    // ─── 切到前台 ───

    /// <summary>
    /// 已在容器内的 Panel 切到前台。
    /// Window：当前栈顶挂起，自己移到栈顶并 Resume/Open。
    /// Hud / Popup / Service：仅层内置顶。
    /// </summary>
    UniTask BringToFrontAsync(UIPanel panel);
    UniTask BringToFrontAsync<T>() where T : UIPanel;

    // ─── 关闭 ───

    /// <summary>关闭指定 Panel（null = 当前 Window 栈顶）。会级联关闭所有子面板。</summary>
    UniTask CloseAsync(UIPanel panel = null);

    /// <summary>关闭第一个类型匹配的 Panel。</summary>
    UniTask<T> CloseAsync<T>() where T : UIPanel;

    /// <summary>关闭 Window 栈顶（保留 Popup / Service）。</summary>
    UniTask CloseTopWindowAsync();

    /// <summary>关闭 Hud / Window / Popup（Service 层保留）。</summary>
    UniTask CloseAllAsync();

    // ─── 销毁 ───

    UniTask DestroyAsync<T>() where T : UIPanel;
    UniTask DestroyAsync(UIPanel panel);
    UniTask DestroyAllAsync();

    // ─── 只读查询（同步） ───

    T GetUI<T>() where T : class;

    /// <summary>Panel 已在容器内（任一层）且 Visible。</summary>
    bool IsOpen<T>() where T : UIPanel;
    bool IsOpen(UIPanel panel);

    /// <summary>Panel 已实例化（在 _uiPanels 列表中），不要求在容器内。</summary>
    bool IsLoaded<T>() where T : UIPanel;
    bool IsLoaded(UIPanel panel);

    int GetVisiblePanelCount();
    bool HasAnyPanelVisible();

    UIPanel GetTopWindow();
    /// <summary>按层优先级取最顶层可见面板：Service &gt; Popup &gt; Window &gt; Hud。</summary>
    UIPanel GetTopPanel();
    UIPanel GetBottomPanel();

    IReadOnlyCollection<UIPanel> GetWindowStack();
    IReadOnlyList<UIPanel> GetLayerPanels(UILayer layer);

    List<UIPanel> GetAllPanels();
    List<T> GetAllPanels<T>() where T : UIPanel;

    Canvas OverlayCanvas { get; }
}
}
