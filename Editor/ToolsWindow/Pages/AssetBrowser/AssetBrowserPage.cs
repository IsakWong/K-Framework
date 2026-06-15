using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KFramework.Editor
{
    /// <summary>
    /// 资产管理页面 — 按类型自动分类，类 JasaManagerWindow 风格
    /// </summary>
    [KToolPage("资产管理", Kit = "资源", Priority = 0)]
    public class AssetBrowserPage : KToolPageBase
    {
        public override string PageName => "资产管理";
        public override int Priority => 0;

        // ===== 分类系统 =====
        private readonly List<AssetCategory> _categories = new();
        private int _categoryIndex;

        // ===== 统一数据 =====
        private readonly List<AssetEntry> _allEntries = new();
        private string _search = "";
        private Vector2 _listScroll, _detailScroll;
        private UnityEngine.Object _selectedAsset;
        private UnityEditor.Editor _selectedEditor;

        // ===== Prefab 编辑缓存 =====
        private GameObject _activePrefabForEditor;
        private readonly Dictionary<Component, UnityEditor.Editor> _prefabCompEditors = new();
        private readonly Dictionary<Component, bool> _prefabCompFoldouts = new();

        // ===== 缺失检测 =====
        private readonly List<Type> _missingConfigTypes = new();

        // ===== 内部类型 =====
        private sealed class AssetCategory
        {
            public string Name;
            public string TypeName;     // null = 显示该分组下的全部
            public bool IsPrefabCat;
            public bool ShowAll;        // "全部" 专用，不过滤 IsPrefab
            public int Count;
        }

        private sealed class AssetEntry
        {
            public string DisplayName;
            public string SubInfo;       // 子路径 或 组件标签
            public string TypeName;
            public UnityEngine.Object Asset;
            public bool IsPrefab;
            public Texture Icon;
        }

        // =====================================================================
        //  生命周期
        // =====================================================================

        public override void OnEnable()
        {
            RefreshAll();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            DestroySelectedEditor();
            ClearPrefabEditors();
        }

        // =====================================================================
        //  Toolbar — JasaManagerWindow 风格的水平分类按钮
        // =====================================================================

        public override void OnToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                // 分类按钮（可滚动）
                var toolbarRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                {
                    for (int i = 0; i < _categories.Count; i++)
                    {
                        var cat = _categories[i];
                        string label = cat.Count > 0 ? $"{cat.Name} ({cat.Count})" : cat.Name;

                        if (GUILayout.Toggle(_categoryIndex == i, label, EditorStyles.toolbarButton))
                        {
                            if (_categoryIndex != i)
                            {
                                _categoryIndex = i;
                                RepaintWindow();
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(180));

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
                    RefreshAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        // =====================================================================
        //  列表面板
        // =====================================================================

        public override void OnListGUI()
        {
            InitStyles();

            // 缺失配置提示
            if (_missingConfigTypes.Count > 0 && _categoryIndex == 0) // 仅在"全部"时显示
            {
                DrawMissingSection();
                EditorGUILayout.Space(4);
            }

            var cat = GetCurrentCategory();
            var filtered = GetFilteredEntries(cat);

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("当前分类下没有匹配的资源", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            // 全部 Tab 按类型分组；单个类型 Tab 不分组
            if (cat?.TypeName == null)
                DrawGroupedList(filtered);
            else
                DrawFlatList(filtered);

            EditorGUILayout.EndScrollView();
        }

        private void DrawMissingSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"缺失配置 ({_missingConfigTypes.Count})", HeaderStyle);
            foreach (var t in _missingConfigTypes)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {t.Name}", GUILayout.ExpandWidth(true));
                if (!string.IsNullOrEmpty(t.Namespace))
                    EditorGUILayout.LabelField(t.Namespace, EditorStyles.miniLabel, GUILayout.Width(80));
                if (GUILayout.Button("创建", GUILayout.Width(40)))
                    CreateConfigAsset(t);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupedList(List<AssetEntry> entries)
        {
            var groups = entries
                .GroupBy(e => e.TypeName)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                EditorGUILayout.LabelField($"{group.Key}  ({group.Count()})", HeaderStyle);

                foreach (var entry in group)
                {
                    DrawListItem(entry);
                }

                EditorGUILayout.Space(4);
            }
        }

        private void DrawFlatList(List<AssetEntry> entries)
        {
            foreach (var entry in entries)
            {
                DrawListItem(entry);
            }
        }

        private void DrawListItem(AssetEntry entry)
        {
            if (entry.Asset == null) return;

            bool selected = _selectedAsset == entry.Asset;
            var style = selected ? SelectedEntryStyle : EntryStyle;
            int height = entry.IsPrefab ? 48 : 32;

            EditorGUILayout.BeginHorizontal(style, GUILayout.Height(height));
            {
                // 图标
                var icon = entry.Icon ?? AssetPreview.GetMiniThumbnail(entry.Asset);
                int iconSize = entry.IsPrefab ? 44 : 24;
                if (icon != null)
                    GUILayout.Label(icon, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                else
                    GUILayout.Label("", GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                // 文字
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(entry.DisplayName, EditorStyles.boldLabel);
                string sub = entry.IsPrefab ? entry.SubInfo : entry.SubInfo;
                if (!string.IsNullOrEmpty(sub))
                    EditorGUILayout.LabelField(sub, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            var rect = GUILayoutUtility.GetLastRect();

            // 点击选择
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (entry.IsPrefab && Event.current.clickCount == 2)
                    InstantiatePrefabInScene(entry.Asset as GameObject);
                else
                    SelectAsset(entry.Asset);

                if (Event.current.clickCount == 2 && !entry.IsPrefab)
                    AssetDatabase.OpenAsset(entry.Asset);

                Event.current.Use();
            }

            // 拖拽（仅 Prefab）
            if (entry.IsPrefab && Event.current.type == EventType.MouseDrag &&
                rect.Contains(Event.current.mousePosition) && entry.Asset != null)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[] { entry.Asset };
                DragAndDrop.StartDrag(entry.DisplayName);
                Event.current.Use();
            }
        }

        // =====================================================================
        //  详情面板
        // =====================================================================

        public override void OnDetailGUI()
        {
            if (_selectedAsset == null)
            {
                EditorGUILayout.HelpBox("在左侧选择一个资源查看详情", MessageType.Info);
                return;
            }

            DrawDetailHeader(_selectedAsset, _selectedAsset.GetType().Name);

            EditorGUILayout.Space(4);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));

            if (_selectedAsset is GameObject prefab)
                DrawPrefabDetailContent(prefab);
            else if (_selectedEditor != null)
                _selectedEditor.OnInspectorGUI();

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabDetailContent(GameObject prefab)
        {
            // 大预览图
            var bigPreview = AssetPreview.GetAssetPreview(prefab);
            if (bigPreview != null)
            {
                var r = GUILayoutUtility.GetRect(180, 180, GUILayout.ExpandWidth(true));
                float size = Mathf.Min(r.width, r.height, 220);
                GUI.DrawTexture(new Rect(r.x + (r.width - size) / 2, r.y, size, size), bigPreview, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.Space(8);

            // 快捷操作
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("放入场景", GUILayout.Width(80)))
                InstantiatePrefabInScene(prefab);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 组件 Inspector
            if (_activePrefabForEditor != prefab)
                RebuildPrefabEditorCache(prefab);

            foreach (var comp in _activePrefabForEditor.GetComponents<Component>())
            {
                if (comp == null || comp is Transform) continue;
                if (!_prefabCompEditors.TryGetValue(comp, out var ed) || ed == null) continue;

                EditorGUILayout.Space(2);
                _prefabCompFoldouts[comp] = EditorGUILayout.InspectorTitlebar(_prefabCompFoldouts[comp], comp);

                if (_prefabCompFoldouts[comp])
                {
                    EditorGUI.indentLevel++;
                    ed.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(4);
                }
            }
        }

        // =====================================================================
        //  数据刷新
        // =====================================================================

        private void RefreshAll()
        {
            _allEntries.Clear();
            _categories.Clear();
            _missingConfigTypes.Clear();

            BuildSOSections();
            BuildPrefabSections();

            // 更新"全部"总数
            var allCat = _categories.Find(c => c.ShowAll);
            if (allCat != null) allCat.Count = _allEntries.Count;

            // 排序：全部 → SO类型 → 全部预制体 → Prefab类型
            _categories.Sort((a, b) =>
            {
                if (a.ShowAll) return -1;
                if (b.ShowAll) return 1;
                if (a.IsPrefabCat != b.IsPrefabCat) return a.IsPrefabCat.CompareTo(b.IsPrefabCat);
                int countCmp = b.Count.CompareTo(a.Count);
                if (countCmp != 0) return countCmp;
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            _categoryIndex = Mathf.Clamp(_categoryIndex, 0, Mathf.Max(0, _categories.Count - 1));

            if (_selectedAsset != null && !_allEntries.Any(e => e.Asset == _selectedAsset))
                SelectAsset(null);
        }

        private void BuildSOSections()
        {
            var soGuids = AssetDatabase.FindAssets("t:ScriptableObject");
            var typeEntries = new Dictionary<string, List<AssetEntry>>();

            foreach (var guid in soGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null) continue;

                string typeName = asset.GetType().Name;
                string subPath = GetAssetRelPath(path, "Assets");

                var entry = new AssetEntry
                {
                    DisplayName = asset.name,
                    SubInfo = subPath,
                    TypeName = typeName,
                    Asset = asset,
                    IsPrefab = false,
                    Icon = AssetPreview.GetMiniThumbnail(asset)
                };
                _allEntries.Add(entry);

                if (!typeEntries.TryGetValue(typeName, out var list))
                {
                    list = new List<AssetEntry>();
                    typeEntries[typeName] = list;
                }
                list.Add(entry);
            }

            foreach (var list in typeEntries.Values)
                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            // "全部" 分类 — 显示所有 SO + Prefab
            _categories.Add(new AssetCategory { Name = "全部", TypeName = null, IsPrefabCat = false, ShowAll = true, Count = -1 });

            foreach (var kv in typeEntries.OrderBy(kv => kv.Key))
            {
                _categories.Add(new AssetCategory
                {
                    Name = kv.Key,
                    TypeName = kv.Key,
                    IsPrefabCat = false,
                    Count = kv.Value.Count
                });
            }

            // 缺失 ConfigObject 检测
            DetectMissingConfigs(typeEntries.Keys.ToHashSet());
        }

        private void BuildPrefabSections()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var typeEntries = new Dictionary<string, List<AssetEntry>>();
            int totalCount = 0;

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string compLabel = DetectPrefabComponent(prefab);

                var entry = new AssetEntry
                {
                    DisplayName = prefab.name,
                    SubInfo = compLabel,
                    TypeName = $"预制体 · {compLabel}",
                    Asset = prefab,
                    IsPrefab = true,
                    Icon = AssetPreview.GetAssetPreview(prefab)
                };
                _allEntries.Add(entry);
                totalCount++;

                if (!typeEntries.TryGetValue(compLabel, out var list))
                {
                    list = new List<AssetEntry>();
                    typeEntries[compLabel] = list;
                }
                list.Add(entry);
            }

            foreach (var list in typeEntries.Values)
                list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            _categories.Add(new AssetCategory { Name = "全部预制体", TypeName = null, IsPrefabCat = true, Count = totalCount });

            foreach (var kv in typeEntries.OrderBy(kv => kv.Key))
            {
                _categories.Add(new AssetCategory
                {
                    Name = kv.Key,
                    TypeName = kv.Key,
                    IsPrefabCat = true,
                    Count = kv.Value.Count
                });
            }
        }

        private static string DetectPrefabComponent(GameObject prefab)
        {
            if (prefab == null) return "无";

            // 按优先级检测常见组件
            if (prefab.GetComponent("UnitBase") != null) return "Unit";
            if (prefab.GetComponent("UIPanel") != null) return "UI面板";
            if (prefab.GetComponent("HUDBase") != null) return "HUD";
            if (prefab.GetComponent("Vfx") != null) return "特效";
            if (prefab.GetComponent("SoundEmitter") != null) return "音效";
            if (prefab.GetComponent("VfxAnim") != null) return "特效动画";

            return "其他";
        }

        private void DetectMissingConfigs(HashSet<string> existingTypeNames)
        {
            Type configBase = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                configBase = asm.GetType("Framework.Config.ConfigObject");
                if (configBase != null) break;
            }
            if (configBase == null) return;

            var allConfigTypes = TypeCache.GetTypesDerivedFrom(configBase)
                .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
                .ToList();

            foreach (var t in allConfigTypes)
            {
                if (!existingTypeNames.Contains(t.Name))
                    _missingConfigTypes.Add(t);
            }
        }

        // =====================================================================
        //  辅助方法
        // =====================================================================

        private AssetCategory GetCurrentCategory()
        {
            if (_categoryIndex < 0 || _categoryIndex >= _categories.Count) return null;
            return _categories[_categoryIndex];
        }

        private List<AssetEntry> GetFilteredEntries(AssetCategory cat)
        {
            string search = (_search ?? "").Trim().ToLowerInvariant();

            return _allEntries
                .Where(e => e.Asset != null)
                .Where(e =>
                {
                    if (cat == null) return true;
                    if (cat.ShowAll) return true;
                    if (cat.TypeName == null) return e.IsPrefab == cat.IsPrefabCat;
                    return e.IsPrefab == cat.IsPrefabCat && (e.TypeName == cat.TypeName || e.SubInfo == cat.TypeName);
                })
                .Where(e => string.IsNullOrEmpty(search) || e.DisplayName.ToLowerInvariant().Contains(search))
                .ToList();
        }

        private void SelectAsset(UnityEngine.Object asset)
        {
            if (_selectedAsset == asset) return;

            DestroySelectedEditor();
            _selectedAsset = asset;

            if (asset != null && asset is not GameObject)
                _selectedEditor = UnityEditor.Editor.CreateEditor(asset);

            // Prefab 选择时重建编辑器缓存
            if (asset is GameObject go)
                RebuildPrefabEditorCache(go);

            RepaintWindow();
        }

        private void DestroySelectedEditor()
        {
            if (_selectedEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(_selectedEditor);
                _selectedEditor = null;
            }
        }

        private void ClearPrefabEditors()
        {
            foreach (var ed in _prefabCompEditors.Values)
            {
                if (ed != null) UnityEngine.Object.DestroyImmediate(ed);
            }
            _prefabCompEditors.Clear();
            _prefabCompFoldouts.Clear();
            _activePrefabForEditor = null;
        }

        private void RebuildPrefabEditorCache(GameObject prefab)
        {
            ClearPrefabEditors();
            _activePrefabForEditor = prefab;
            if (prefab == null) return;

            foreach (var comp in prefab.GetComponents<Component>())
            {
                if (comp == null || comp is Transform) continue;
                _prefabCompEditors[comp] = UnityEditor.Editor.CreateEditor(comp);
                _prefabCompFoldouts[comp] = true;
            }
        }

        private void CreateConfigAsset(Type configType)
        {
            const string configFolder = "Assets/Config";
            if (!AssetDatabase.IsValidFolder(configFolder))
                AssetDatabase.CreateFolder("Assets", "Config");

            var assetPath = $"{configFolder}/{configType.Name}.asset";
            if (System.IO.File.Exists(assetPath))
            {
                EditorUtility.DisplayDialog("文件已存在", $"{assetPath} 已存在", "确定");
                return;
            }

            var asset = ScriptableObject.CreateInstance(configType);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshAll();
            EditorGUIUtility.PingObject(asset);
        }

        private static void InstantiatePrefabInScene(GameObject prefab)
        {
            Transform parent = null;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                parent = prefabStage.prefabContentsRoot.transform;

            var sceneView = SceneView.lastActiveSceneView;
            Vector3 position = Vector3.zero;
            if (sceneView != null)
            {
                position = sceneView.pivot;
                position.z = 0f;
                position.x = Mathf.Round(position.x);
                position.y = Mathf.Round(position.y);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            if (parent != null)
                instance.transform.SetParent(parent, true);

            Undo.RegisterCreatedObjectUndo(instance, $"创建 {prefab.name}");
            Selection.activeGameObject = instance;

            if (sceneView != null) sceneView.Focus();
        }

        private void RepaintWindow()
        {
            EditorWindow.GetWindow<KFrameworkToolsWindow>()?.Repaint();
        }
    }
}
