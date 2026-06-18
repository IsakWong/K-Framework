using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// KFramework 统一工具窗口
    /// 左侧页面列表 + 右侧内容区的主从布局
    /// </summary>
    public class KFrameworkToolsWindow : EditorWindow
    {
        private const float ListPaneWidth = 320f;

        private int _pageIndex;
        private IKToolPage _currentPage;
        private Vector2 _toolbarScroll;

        [MenuItem("KFramework/Tools Window %k")]
        public static void ShowWindow()
        {
            var window = GetWindow<KFrameworkToolsWindow>("KFramework");
            window.minSize = new Vector2(800, 500);
            window.Show(false);
        }

        private void OnEnable()
        {
            _pageIndex = Mathf.Clamp(_pageIndex, 0, Mathf.Max(0, KToolPageRegistry.Pages.Count - 1));
            ActivateCurrentPage();
        }

        private void OnDisable()
        {
            _currentPage?.OnDisable();
            _currentPage = null;
        }

        private void ActivateCurrentPage()
        {
            _currentPage?.OnDisable();

            var pages = KToolPageRegistry.Pages;
            if (pages.Count == 0) return;

            var entry = pages[_pageIndex];
            _currentPage = entry.Instance;
            _currentPage?.OnEnable();
        }

        private void OnGUI()
        {
            var pages = KToolPageRegistry.Pages;
            if (pages.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "未找到任何注册的工具页面。\n\n" +
                    "创建一个实现 IKToolPage 接口的类即可自动注册。",
                    MessageType.Info);
                return;
            }

            DrawTabBar(pages);
            EditorGUILayout.Space(2);

            if (_currentPage == null) return;

            DrawPageContent();
        }

        private void DrawTabBar(System.Collections.Generic.IReadOnlyList<KToolPageRegistry.PageEntry> pages)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var kitGroups = pages.GroupBy(p => p.Kit).ToList();
                foreach (var group in kitGroups)
                {
                    foreach (var entry in group)
                    {
                        int idx = FindPageIndex(pages, entry);
                        bool selected = _pageIndex == idx;

                        if (GUILayout.Toggle(selected, entry.Name, EditorStyles.toolbarButton, GUILayout.Height(22)) != selected)
                        {
                            if (_pageIndex != idx)
                            {
                                _pageIndex = idx;
                                ActivateCurrentPage();
                                Repaint();
                            }
                        }
                    }

                    if (group != kitGroups.Last())
                        GUILayout.Space(8);
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static int FindPageIndex(System.Collections.Generic.IReadOnlyList<KToolPageRegistry.PageEntry> pages, KToolPageRegistry.PageEntry entry)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i] == entry)
                    return i;
            }
            return -1;
        }

        private void DrawPageContent()
        {
            _currentPage.OnToolbarGUI();
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            {
                // 左侧列表
                EditorGUILayout.BeginVertical(GUILayout.Width(ListPaneWidth), GUILayout.ExpandHeight(true));
                _currentPage.OnListGUI();
                EditorGUILayout.EndVertical();

                // 分隔线
                GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

                // 右侧详情
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _currentPage.OnDetailGUI();
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
