# YIUI UI 架构分析

> 仓库：`https://github.com/LiShengYang-yiyi/YIUI`（YIUI-ET9.0 分支）
> 分析对象核心包：`Packages/cn.etetet.yiuiframework` v3.1.4（Runtime 23.3k 行 + Editor 8.4k 行 + Scripts 12.9k 行，共 493 个 .cs）
> UI 基础：Unity UGUI + Dotween + Odin Inspector
> 宿主框架：ET9.0（Entity-Component-System 风格的 C# 全栈框架）

---

## 1. 包体系：YIUI 是一套「核心 + 插件包」的生态

YIUI 不是一个单体包，而是以 `cn.etetet.yiuiframework`（核心）为地基，按职责拆成多个 ET 包：

| 编号 | 包名 | 职责 |
|------|------|------|
| 1001 | `cn.etetet.yiuiframework` | **框架核心**（管理器、绑定、生成、工具） |
| 1002 | `cn.etetet.yiui` | **框架生成的默认落地包**（只放 asmdef / 图集配置，生成的 UI 代码落这里） |
| 1201 | `cn.etetet.yiuiloopscrollrectasync` | 无限循环列表（异步） |
| 1203 | `cn.etetet.yiuiyooassets` | YooAsset 资源管理适配 |
| 1205 | `cn.etetet.yiuiinvoke` | Invoke 调用系统（跨模块异步调用） |
| 1301 | `cn.etetet.yiuigm` | GM 命令面板 |
| 1302 | `cn.etetet.yiuitips` | 提示弹窗 |
| 1303 | `cn.etetet.yiui3ddisplay` | UI 3D 模型展示 |
| 1304 | `cn.etetet.yiuireddot` | 红点 |
| 1306 | `cn.etetet.yiuieffect` | UI 特效 |

设计意图：核心框架稳定，业务能力以「可插拔包」形式叠加，用户按项目需要引入。

---

## 2. 核心分层

```
YIUIMgrComponent（全局唯一 UI 管理器，挂在 Scene 上）
  └─ YIUIRootComponent（UIRoot / Canvas / UICamera / LayerRoot）
        └─ Layer0..Layer6（对应 EPanelLayer 枚举，按 z 轴分层）
              └─ PanelInfo（每个已打开 UI 的运行时档案）
                    ├─ YIUIChild         UI 主体（ET Entity，持有 GameObject / RectTransform / 4 张绑定表）
                    ├─ YIUIPanelComponent 面板组件（层级/选项/优先级/子 View）
                    ├─ YIUIViewComponent  子界面组件
                    └─ YIUIWindowComponent 窗口组件（窗口选项 + 开关动画）
```

关键点：**Unity 侧是 GameObject/Prefab（MonoBehaviour 上的 4 张 CDE 表），逻辑侧是 ET Entity**，两者通过 `YIUIChild` 一一对应。ET 的 Entity 树负责逻辑与生命周期，Unity 的 Transform 树负责渲染，运行时由代码建立映射。

---

## 3. 核心概念

### 3.1 CDE 四表（组件/数据/事件）

`UIBindCDETable`（挂在 Prefab 上）持有并管理：

| 表 | 类型 | 作用 |
|----|------|------|
| **C**omponentTable | `UIBindComponentTable` | 引用子控件（`u_ComXXX`），支持嵌套公共组件 |
| **D**ataTable | `UIBindDataTable` | 数据变量（`UIData` / `UIDataValue`），供绑定使用 |
| **E**ventTable | `UIBindEventTable` | UI 事件（点击/拖拽/输入等），支持 P0~P5 泛型参数 |

这三张表由 Odin 在 Inspector 中可视化配置，运行时由代码生成器产出强类型字段（`u_ComGMButton`、`u_EventOpenGMView` …），业务代码直接 `self.u_ComXXX` 使用，无字符串查找、无 SendMessage。

### 3.2 Panel / View / Window / Component 四层 UI 模型

| 类型 | 说明 | 回退(Back) | 同层并存 |
|------|------|-----------|---------|
| **Panel** | 全屏面板，挂在 `EPanelLayer.Panel`，栈式管理 | ✅ 参与堆栈 | ❌ 同层只显示一个 |
| **View** | Panel 内部子界面（分块加载），挂在 Panel 下的 ViewParent | ❌ **无自动回退**（只能切换） | 可多个，`ViewWindowType` 区分窗口/弹窗 |
| **Window** | 非独立类型，是 Panel/View 的「窗口行为」组件（动画/选项） | — | — |
| **Component** | 公共/嵌套组件（如 GMCommandItem），可被多个 Panel 复用 | — | — |

Panel 与 View 都实现相同的 `IYIUIOpen*` 生命周期接口，因此打开/关闭/动画逻辑共用（`YIUIPanelComponentSystem_Open_Async.cs` 与 `YIUIViewlComponentSystem_Open.cs` 结构几乎一致）。

### 3.3 层级系统 `EPanelLayer`

```
Top(0) → Tips(1) → Popup(2) → Panel(3) → Scene(4) → Bottom(5) → Cache(6, 强制隐藏)
```

`InitRoot()` 动态为每个枚举值创建一个 Layer GameObject，按 `localPosition.z = i * LayerDistance` 分层，摄像机远裁剪负责前后关系。Panel 通过 `AddUI()` 按 `Priority`（大在前）插入到层内的 `List<PanelInfo>`，并同步设置 `SiblingIndex`。

---

## 4. 生命周期与事件接口（ETSystem 风格）

UI 逻辑不写在 MonoBehaviour，而是通过 **ET 的 EntitySystem 特性** 分散到静态类中：

```csharp
// 自动生成：GMPanelComponentSystemGen.cs（不可改）
[EntitySystem] private static void Awake(this GMPanelComponent self) { }
[EntitySystem] private static void YIUIBind(this GMPanelComponent self) { self.UIBind(); }

// 手写：GMPanelComponentSystem.cs（业务逻辑）
[EntitySystem] private static async ETTask<bool> YIUIOpen(this GMPanelComponent self) { ... }
[EntitySystem] private static void Destroy(this GMPanelComponent self) { }
```

框架定义了一整套生命周期接口（`Scripts/ModelView/Client/Event/SystemEvent/`）：

- `IYIUIOpen` / `IYIUIOpen<T1..T5>`（异步 `ETTask<bool>`，返回值决定是否继续）
- `IYIUIClose`、`IYIUIDisClose`（可拒绝关闭）、`IYIUIInitialize`
- `IYIUIBackOpen` / `IYIUIBackClose`（回退栈恢复/关闭时回调）
- `IYIUIBackHomeOpen` / `IYIUIBackHomeClose`（Home 操作特殊回调）
- `IYIUIOpenTween` / `IYIUICloseTween` / `...TweenEnd`（自定义动画接管）

参数化打开由 `IYIUIOpen<P1..P5>` 标记，`YIUIEventSystem.Open(entity, p1, p2)` 通过 ET 的 SystemType 反射分发到对应 System，**类型安全，无装箱字符串**。

---

## 5. 打开 / 关闭 / 回退三大流程

### 5.1 打开（`YIUIMgrComponentSystem_Open.cs`，575 行）

```
OpenPanelAsync<T>(root)
  ├─ GetPanelInfo<T>()                        按类型查 BindVo（缓存到 m_PanelCfgMap）
  ├─ CoroutineLock(UIProjectName)             同层串行，避免并发打开
  ├─ OpenPanelStartAsync
  │    ├─ Publish YIUIEventPanelOpenBefore（全局事件）
  │    ├─ YIUIFactory.CreatePanelAsync         加载/实例化 Prefab（YIUILoadComponent）
  │    │     └─ CreateByObjVo → AddChild<YIUIChild> + AddComponent(vo.ComponentType)
  │    │                       + cdeTable.CreateComponent(递归子组件) + InitOwnerUIEntity
  │    └─ AddUI(info)                          入层、排序、全屏拉伸、停止销毁倒计时
  ├─ OpenPanelBefore                           处理「先开」/ 关闭其他同级 Panel
  ├─ info.UIPanel.Open(...)                    触发 YIUIOpen 系统方法 + Window 开动画
  └─ OpenPanelAfter                            处理「后关」/ 失败回滚 / Publish After
```

### 5.2 关闭（`YIUIMgrComponentSystem_Close.cs`）

```
ClosePanelAsync(name, tween, ignoreElse, ignoreLock)
  ├─ 协程锁
  ├─ Publish CloseBefore
  ├─ DisClose 检查（IYIUIDisClose 可拒绝关闭）
  ├─ IYIUIClose 回调（可返回 false 阻止）
  ├─ WindowCloseTweenBefore 时机
  ├─ CloseAllView + CloseTween（先关 / 后关由 WindowLastClose 决定）
  ├─ RemoveUIAddElse（恢复被压栈的 Panel）
  ├─ WindowClose 回调
  └─ RemoveUI → ForeverCache/TimeCache 走 Cache 层，否则 Destroy
```

### 5.3 回退（`YIUIMgrComponentSystem_Back.cs`，439 行）

- `AddUICloseElse`：打开新 Panel 时，遍历 Panel 层其他 UI，按 `EPanelStackOption` 处理：
  - `Visible` → 直接 `SetActive(false)`（不播动画）
  - `VisibleTween` → 播关闭动画后隐藏
  - `Omit` → 真正关闭并移出堆栈
  - `None` → 叠加，不动
- `RemoveUIAddElse`：关闭当前 Panel 时，反向恢复上一个被压栈的 Panel（`BackOpen` 回调）
- `RemoveUIToHome` / `HomePanel`：回到指定界面，其余全部关闭，带 `BackHome*` 专用回调

> 设计原则：**View 明确没有自动回退**（源码注释反复强调），只有 Panel 参与回退栈；这避免了「关 C 自动回 B 再回 A」的链式副作用。

---

## 6. 选项系统（Flags 枚举驱动行为）

YIUI 把大量行为开关做成 `[Flags]` 枚举，配置在 Prefab 上，运行时用 `HasFlag` 判断：

- `EPanelOption`：Container / ForeverCache / TimeCache / DisClose / IgnoreBack / IgnoreClose
- `EPanelStackOption`：None / Visible / VisibleTween / Omit
- `EWindowOption`（20+ 项）：FirstOpen / LastClose / Ban*Tween / Skip*Tween / AllowOptionByTween / WindowCloseTweenBefore …
- `EViewWindowType`：View / Popup

好处：一个 Panel 的行为完全由配置决定，无需写代码；坏处：组合爆炸、可读性差（需要 `HasFlag` 辅助属性类如 `YIUIPanelComponent_HasFlag.cs` 转成语义化属性）。

---

## 7. 数据绑定系统（MVVM 风格）

`UIDataBind`（`[ExecuteInEditMode]` MonoBehaviour）是绑定基类：

```
UIData（声明变量：name + UIDataValue 类型化值）
   ↑ 由 UIBindDataTable 管理
UIDataBind 子类（UIDataBindText / Image / Active / Slider / Rect / Animation ...）
   → BindData() / UnBindData() / OnRefreshData()
```

- 编辑器下 `OnValidate` 即可预览绑定效果（`[ExecuteInEditMode]`）
- 数据变更通过 `UIDataValue` 的 Ref 引用池广播（`UIDataChangeRef`），驱动所有绑定者刷新
- 覆盖：Text / Image / Color / Rect(位置/旋转/缩放/尺寸) / Slider / Toggle / Dropdown / Active / Animation / VideoPlayer 等
- 这是 YIUI 相对多数 UI 框架的特色：**零代码 UI 状态同步**

---

## 8. 代码生成（框架的最大工作量所在）

YIUI 大量依赖生成，分两条链：

### 8.1 Editor 工具链（`Editor/YIUIAutoTool/`）

Odin 驱动的 `YIUIAutoTool` 窗口，模块化：

| 模块 | 作用 |
|------|------|
| UIBase | 一键创建/同步 UI 基类 |
| UICreate | 从 Prefab 生成 Panel/View/Component 的 Gen 代码 + 手写模板 |
| UIETCode | 生成 ET 脚本（System/Component）|
| UICheck | Prefab/脚本合法性检查（引用丢失、命名规范等）|
| UIConst | 生成常量类 |
| UIMacro | 编译宏管理（ET/Unity 两种宏）|
| UIPublish | 资源发布（图集/预制打包）|
| UISetting | 图集等设置 |
| UIUnityIcons | Unity 图标浏览 |

模板在 `Editor/YIUIAutoTool/Template/*.txt`（`UICreateBaseTemplate`、`UICreatePanelTemplate`、`UICreateComponentGenTemplate` …）。

### 8.2 Roslyn SourceGenerator（`DotNet~/SourceGenerator/`）

- `YIUIEntitySystemAnalyzer`：校验「Entity 类存在未实现的事件函数」，编译期报错（YIUI0001）
- `YIUIEntitySystemMethodNeedSystemOfAttrAnalyzerRule`：`[EntitySystem]` 必须位于 `[EntitySystemOf]` 类中（YIUI0002）
- `YIUIEntitySystemCodeFixProvider`：一键补全缺失实现

### 8.3 生成产物（以 GM 面板为例）

```
ModelView/Client/YIUIGen/GM/GMPanelComponentGen.cs   # 字段声明 + [YIUI] [ComponentOf] 特性（勿改）
ModelView/Client/YIUIComponent/GM/GMPanelComponent.cs # 手写字段（业务可改）
HotfixView/Client/YIUIGen/GM/GMPanelComponentSystemGen.cs # Awake + UIBind 自动绑定（勿改）
HotfixView/Client/YIUISystem/GM/GMPanelComponentSystem.cs # 手写 YIUIOpen/Destroy/事件回调
```

「Gen 覆盖，非 Gen 保留」的双文件约定，让重新生成不会丢业务代码。

---

## 9. 资源与缓存

- `YIUILoadComponent`：抽象加载接口，实际由 `cn.etetet.yiuiyooassets`（YooAsset）实现
- `PanelInfo` 缓存：`m_PanelCfgMap`（按类名）保存已打开 UI 的档案与实体引用
- 关闭策略：
  - `ForeverCache`：永不销毁，只隐藏（放 Cache 层）
  - `TimeCache`：关闭后 X 秒再销毁（倒计时 Token，`YIUIPanelComponent_TimeDestroy.cs`）
  - 默认：立即 `Destroy`
- 内部对象池：`Runtime/Core/YIUIBase/Cache/` 提供 List/Dictionary/HashSet/StringBuilder/RefPool 等池

---

## 10. 其它值得一提的机制

- **协程锁**：`CoroutineLockComponent.Wait(CoroutineLockType.YIUIPanel, code)` —— Panel 层用项目名 hash 作锁（整层串行），其它层用界面名 hash（各自串行），彻底解决异步打开/关闭的竞态。
- **UI 屏蔽层** `YIUIMgrComponent_Block`：所有层级之上放一个 `m_LayerBlock`，播放动画时自动屏蔽操作（引用计数 `m_AllForeverBlockCode`），也可手动 API 屏蔽。
- **安全区** `YIUIMgrComponent_SafeArea`：刘海屏适配。
- **窗口动画** `YIUIWindowComponentSystem`：默认 Dotween 淡入淡出，可被 `IYIUIOpenTween/CloseTween` 接管；`BanAwaitOpenTween` 控制是否等待。
- **Widget 组件**：`UIBlock` / `UIBlockPolygon`（不规则点击）、`UICircle`、`ResolutionAdapter`、`YIUIClickEffect`、高斯模糊等。
- **Invoke 系统**（`cn.etetet.yiuiinvoke`）：`YIUIInvokeEntity_*` 结构体 + `EventSystem.YIUIInvokeEntityAsync<TReq,TRes>`，跨模块异步解耦。

---

## 11. 优点

1. **工业级完整度**：打开/关闭/堆栈/回退/动画/缓存/遮罩/安全区/红点/GM/多语言/无限列表全都有，且是实战项目沉淀。
2. **配置驱动**：Flags 选项让界面行为可视化配置，策划/美术也能调。
3. **强类型代码生成**：控件引用、事件、绑定全部生成强类型字段，杜绝字符串魔法，重构安全。
4. **数据绑定（MVVM）**：UI 状态与逻辑分离，减少手写刷新代码。
5. **异步一致性**：全链路 `ETTask` + 协程锁，异步打开/关闭不会错乱。
6. **事件系统完备**：`IYIUIBack*`、`Tween*` 等细粒度生命周期钩子覆盖绝大多数需求。

## 12. 缺点 / 风险

1. **复杂度极高**：493 个文件、4 万多行；Panel/View/Window/Component + 20+ Flags 选项，学习曲线陡峭（README 自己都说「不推荐刚入门的新人学习」）。
2. **强耦合 ET 框架**：依赖 `Entity`/`EntityRef`/`ETTask`/`EventSystem`/`CoroutineLock`/`AddChild` 等，无法脱离 ET 使用。
3. **强耦合 Odin Inspector**：核心绑定与编辑器都依赖 Odin（付费插件），且大量 `#if UNITY_EDITOR` 混在 Runtime 里。
4. **IL2CPP 依赖**：SourceGenerator/AOT 相关，构建环境要求高。
5. **Flags 组合爆炸**：`EWindowOption` 20+ 位，组合语义难以推理，靠辅助属性类勉强可读。
6. **反射/生成成本**：`GetBindVoByType`、ET System 分发、SourceGenerator 都带来启动与编译开销。
7. **命名空间不统一**：`YIUIFramework` / `ET.Client` / 全局混用（与 K-Framework 处于类似的过渡态）。

## 13. 与 K-Framework 的对照

| 维度 | YIUI | K-Framework |
|------|------|-------------|
| 宿主 | ET9（ECS 风格，Entity 树） | 自研 KGameCore + TModule |
| UI 逻辑载体 | ET Entity + EntitySystem 静态方法 | MonoBehaviour `UIPanel` + UniTask |
| 打开方式 | `OpenPanelAsync<T>()` + IYIUIOpen<> 接口 | `UIService.PushAsync<T>()` |
| 数据绑定 | 完整 MVVM（CDE 表 + UIDataBind） | 无（信号/事件驱动） |
| 代码生成 | 重度（编辑器工具 + Roslyn SG） | 无 |
| 栈管理 | Panel 栈 + View 切换 + Home 回退 | Fullscreen 栈 + Overlay |
| 配置驱动 | Flags 枚举 + Odin | 面板属性 + 抽象基类 |
| 学习成本 | 很高 | 中 |

YIUI 值得 K-Framework 借鉴的点：**强类型代码生成替代字符串查找**、**协程锁保证异步一致性**、**Panel 堆栈选项化（Visible/Omit/None）**、**回退钩子（BackOpen/BackClose）**、**绑定数据编辑器预览**。
