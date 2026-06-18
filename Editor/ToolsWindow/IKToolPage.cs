using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// 工具页面接口，所有在 KFrameworkToolsWindow 中显示的页面需实现此接口
    /// </summary>
    public interface IKToolPage
    {
        string PageName { get; }
        string Kit { get; }
        int Priority { get; }
        void OnEnable();
        void OnDisable();
        void OnToolbarGUI();
        void OnListGUI();
        void OnDetailGUI();
    }
}
