# 通用 JSON 预制体生成器（ui-spec 解释器）

> **文档版本**: v1.0  
> **最后更新**: 2026-08-20  
> **文档状态**: 设计定稿（工具待实现，实施计划见 §九）  
> **用途**: 定义「HTML 设计稿 → Unity 预制体」的通用 JSON 解释器方案，替代现有 9 个手写预制体生成器，闭合 UI 制作流程的数据链路  
> **关联文档**: `TechnicalDocs/Client/UI系统.md`（MVC 架构与绑定规范）、`TechnicalDocs/Client/UIPrefab/index.html`（设计稿索引与工作流说明）、`TechnicalDocs/Client/UIPrefab/HTML可视化编辑器.md`（布局可视化编辑，上游工具）、`TechnicalDocs/Client/UIPrefab/tools/gen_ui_html.py`（反向导出工具）

---

## 一、背景与动机

### 1.1 现状

UI 制作采用「先 HTML 后代码」工作流（见 `index.html`）：

```
① 手写/迭代 <页面>.html（视觉稿 + 层级树 + 内嵌 ui-spec JSON）
② 设计定稿
③ 人工将 JSON 规格转写为手写 Editor 生成器（GenerateXXXPrefab.cs）
④ Unity 菜单执行生成器 → 预制体入库
```

当前规模（2026-08-20 盘点）：

| 项 | 数量 | 说明 |
|----|------|------|
| HTML 设计稿 | 10 份 | 9 个页面 + Common（FriendItem 通用组件），共 205 个节点 |
| 手写生成器 | 10 个 | `Generate{Panel}Prefab.cs` × 9 + `GenerateFriendItemPrefab.cs`，另有 `UIPrefabCreator.cs`（UITest 旧工具） |
| 单生成器体量 | 145~624 行 | GenerateUIFriendsPrefab.cs 471 行、GenerateUIHomePrefab.cs 624 行，合计约 4000 行 |
| 规格覆盖组件 | 26 种 | RectTransform/CanvasRenderer/Text/Image/Button/InputField/ScrollRect/Mask/Horizontal·VerticalLayoutGroup/Slider/Toggle + 15 种脚本组件（View/Ctrl/AutoBinder/行视图） |

### 1.2 问题

`index.html` 声称 ui-spec JSON 是"后续代码生成的唯一数据源"，但第③步实际是**人工转写**：

1. **双份维护**：改一次设计要同时改 HTML 和 C# 生成器，靠人肉同步（历史提交 `ffb102b`"UIFriends 预制体与 AB 引用缓存同步"即此成本的体现）。
2. **大量重复代码**：颜色常量、`CreateImage/CreateText/CreateButton/FindDeepChild/EnsureDirectory` 等辅助方法在 10 个生成器间逐份复制（源码注释自证："与 GenerateUILoginPrefab 相同约定"、"与 GenerateUILoginPrefab 保持同一套视觉规范"）。
3. **漂移风险**：HTML 与预制体之间没有一致性校验，改了一边忘掉另一边不会报错。
4. **扩展成本线性增长**：每新增一个面板都要再写约 500 行 Editor 代码。

### 1.3 目标

**写一个通用 JSON 解释器**：一个 Editor 工具直接读取任意 `<页面>.html` 内嵌的 ui-spec JSON，递归解释节点树，构建完整 UGUI 预制体。达成：

- HTML 设计稿成为**真正的唯一数据源**：改 HTML → 点菜单 → 预制体自动更新
- 10 个手写生成器退役，约 4000 行重复代码归零
- 视觉规范（颜色/字号/尺寸）只存在一份

---

## 二、JSON 解释器能做什么（能力总览）

一句话定义：**把「按规范描述 UI 的 JSON 树」解释执行成「Unity 场景中的 GameObject 树 + 组件 + 字段绑定」，并保存为预制体。**

| # | 能力 | 说明 |
|---|------|------|
| 1 | HTML 内嵌规格提取 | 解析 `<script type="application/json" id="ui-spec">` 标签，取出 JSON 文本反序列化为节点树；无需独立 .json 文件，设计稿与数据源同体 |
| 2 | 递归节点构建 | 按节点树逐层创建 GameObject + RectTransform，解释 anchors/pivot/position/size（position→anchoredPosition，size→sizeDelta） |
| 3 | 内置组件解释 | Image（background 颜色）、Text（text/fontSize/align/color）、Button、InputField、ScrollRect、Mask、Horizontal/VerticalLayoutGroup（layout 参数）、Slider、Toggle |
| 4 | 复合组件自动接线 | ScrollRect 自动挂接子节点 Viewport（含 Mask）与 Content；InputField 自动挂接 Text 与 Placeholder；Button 自动设置 targetGraphic——**约定优于配置**，无需在 JSON 里写引用 |
| 5 | 脚本组件挂载 | components 中以 `.cs` 结尾的项（如 `UIFriendsView.cs`、`FriendItem.cs`）按类名解析类型并 AddComponent；根节点照常挂 View/Ctrl/UIAutoBinder 三件套 |
| 6 | View 字段自动绑定 | 名为 `m_Xxx`/`mi_Xxx` 的节点自动绑定到 View（及行视图）的 `[SerializeField]` 字段（SerializedObject），类型按目标字段类型取组件——沿用 UIAutoBinder 命名规范，消灭手写 `BindViewFields` |
| 7 | 嵌套预制体实例化 | 通过 `ref` 字段引用 Common 公共组件（如 `Common/FriendItem`）实例化进层级（v1.1 扩展，见 §4.4） |
| 8 | 全量重建 + GUID 稳定 | 直接 `SaveAsPrefabAsset` 原地覆盖，预制体 GUID 保持不变，场景/AB 引用不断（改进点，见 §5.7） |
| 9 | 干跑校验（Dry Run） | 只解析与验证、不写资产：命名规范、组件名合法性、必备子节点（ScrollRect 缺 Viewport 等）、绑定字段可匹配性，错误列表输出到控制台 |
| 10 | 一致性校验闭环 | 生成后可调用 `gen_ui_html.py` 从预制体反向导出 HTML，与源 HTML 的 ui-spec 做 diff——设计稿 ↔ 预制体双向可对账 |

**一个直观例子**（UILogin 用户名输入框，规格 → 产物）：

```json
{
  "name": "UsernameInput",
  "active": true,
  "components": ["RectTransform", "CanvasRenderer", "Image", "InputField"],
  "anchors": {"min": [1.0, 0.5], "max": [1.0, 0.5]},
  "pivot": [0.5, 0.5],
  "position": [-120.0, 0.0],
  "size": [240.0, 36.0],
  "background": "#37474F",
  "children": [
    {"name": "Text", "components": ["...Text"], "text": "", "fontSize": 18, "align": "MiddleLeft", "color": "#FFFFFF", ...},
    {"name": "Placeholder", "components": ["...Text"], "text": "请输入用户名", "fontSize": 18, "color": "rgba(144,164,174,0.50)", ...}
  ]
}
```

解释器产出：GameObject(UsernameInput) + Image(#37474F) + InputField（textComponent→Text 子节点，placeholder→Placeholder 子节点），RectTransform 按锚点(1,0.5)/偏移(-120,0)/尺寸(240,36) 摆放——与 `GenerateUILoginPrefab.cs` 手写约 80 行 `CreateInputField` 等价。

---

## 三、整体架构

### 3.1 数据流

```
<页面>.html（设计稿，唯一数据源）
   │ ① UISpecExtractor —— 提取 <script id="ui-spec"> 内 JSON 文本
   ▼
UISpecNode 树（纯数据 POCO，205 节点规模）
   │ ② UISpecValidator —— 干跑校验（命名/组件/约定子节点/绑定可匹配）
   ▼
UISpecPrefabBuilder —— 递归解释
   │      ├─ 组件工厂注册表 Dictionary<string, IComponentBuilder>
   │      │    ├─ RectTransformBuilder（所有节点，处理 anchors/pivot/position/size）
   │      │    ├─ CanvasRendererBuilder / ImageBuilder / TextBuilder / ButtonBuilder
   │      │    ├─ InputFieldBuilder / ScrollRectBuilder / MaskBuilder
   │      │    ├─ LayoutGroupBuilder / SliderBuilder / ToggleBuilder
   │      │    └─ ScriptComponentBuilder（xxx.cs → 类型解析 AddComponent）
   │      └─ 复合接线（构建完成后统一二次处理引用）
   ▼
GameObject 树
   │ ③ UISpecViewBinder —— m_Xxx/mi_Xxx 节点 → View/行视图字段绑定
   ▼
④ PrefabUtility.SaveAsPrefabAsset → AssetPackage/Prefabs/UI/<页面名>/<页面名>.prefab
```

### 3.2 代码位置与类设计

统一放在 `Client/Assets/Editor/UI/UISpec/`，命名空间 `DualEnigma.UI.Editor`（与同目录 UIPrefabCreator.cs 一致）：

| 文件 | 职责 |
|------|------|
| `UISpecExtractor.cs` | 读 HTML 文件 → 正则提取 JSON → `JsonUtility.FromJson`（Schema 固定，无需第三方 JSON 库） |
| `UISpecNode.cs` | `[Serializable]` POCO：name/active/components/anchors/pivot/position/size/text/fontSize/align/color/background/layout/children 及扩展字段 |
| `IComponentBuilder.cs` | 接口：`void Build(GameObject go, UISpecNode node, BuildContext ctx)`；注册表 `Dictionary<string, IComponentBuilder>`，未知组件名 → 校验期报错 |
| `UISpecPrefabBuilder.cs` | 递归构建入口 + 复合接线（ScrollRect/InputField/Button 的子节点引用） |
| `UISpecViewBinder.cs` | 遍历 `m_Xxx`/`mi_Xxx` 节点，按 View 字段名/类型绑定（SerializedObject） |
| `UISpecGenerateWindow.cs` | EditorWindow 菜单：页面多选 / 生成 / 干跑校验；扫描 `TechnicalDocs/Client/UIPrefab/pages/*.html` 列出可选页面 |

菜单入口：`DualEnigma > UI > 从设计稿生成预制体`（EditorWindow）；同时提供 `DualEnigma > UI > 校验设计稿`（纯干跑）。

### 3.3 关键设计决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 运行时 or Editor | 纯 Editor 工具 | 预制体入库后运行时零开销；与"全程序化、可版本化"原则一致 |
| 逐面板代码生成 or 通用解释 | 通用解释器 | 新面板零 Editor 代码；消灭 4000 行重复；规格即数据可校验 |
| 反序列化库 | Unity 内置 JsonUtility | Schema 固定 POCO，够用且零依赖（Newtonsoft 未进 manifest） |
| 扩展机制 | 组件构建器注册表 | 新组件类型 = 新增一个 Builder 类 + 注册一行，不改核心 |
| 绑定策略 | 沿用 m_Xxx/mi_Xxx 规范 | 与 UIAutoBinder/UIBindingGenerator 同一套约定，View 代码零改动 |
| 保存策略 | SaveAsPrefabAsset 原地覆盖（不先删） | 保持 GUID 稳定（现状手写生成器先删后存，GUID 每次变化，仅因运行时按路径加载才未暴露问题） |

---

## 四、ui-spec 节点规范（v1.2）

### 4.1 节点公共字段（全部节点，205/205 节点均已具备）

| 字段 | 类型 | 说明 |
|------|------|------|
| `name` | string | 节点名。`m_Xxx`/`mi_Xxx` 前缀（后首字母大写）参与 View 字段自动绑定 |
| `active` | bool | false = 隐藏节点（设计稿橙色虚线 ✕），构建时 `SetActive(false)` |
| `components` | string[] | 组件类型列表；`.cs` 后缀 = 脚本组件；`RectTransform` 隐含于所有节点 |
| `anchors` | `{min:[x,y], max:[x,y]}` | 0~1 锚点，→ anchorMin/anchorMax |
| `pivot` | `[x,y]` | → pivot |
| `position` | `[x,y]` | → anchoredPosition（Unity 坐标系，y 向上；viewer.js 已做翻转，所见即所得） |
| `size` | `[w,h]` | → sizeDelta（锚点拉伸时可负数收缩，如 InputField 文本区 size [-20,-4]） |
| `children` | 节点数组 | 子节点，构建时 `SetParent(parent, false)` 保持局部坐标 |

### 4.2 组件字段与构建约定（现有 26 种组件全覆盖）

| 组件（spec 名） | 附加字段 | 构建约定 |
|----------------|---------|---------|
| `CanvasRenderer` | — | 有可见图形的节点需要；解释器在挂 Image/Text 时自动补齐，spec 里显式写出亦兼容 |
| `Image` | `background`（`#RRGGBB` / `rgba(r,g,b,a)`） | → Image.color；纯装饰图 raycastTarget=false |
| `Text` | `text` / `fontSize` / `align`（TextAnchor 名）/ `color` | 内置字体 LegacyRuntime.ttf；raycastTarget=false；horizontalOverflow=Overflow；空 text = 动态文本（运行时赋值） |
| `Button` | 复用 `background` | targetGraphic=自身 Image；文字放子节点 `Text`（spec 中即为子节点，无需特殊处理） |
| `InputField` | 复用 `background` | **约定子节点**：`Text`（空文本，输入内容）+ `Placeholder`（占位文案，斜体半透明）；解释器自动接线 textComponent/placeholder |
| `ScrollRect` | 复用 `background` | **约定子节点**：`Viewport`（Mask+Image，showMaskGraphic=false）内含 `XxxContent`（LayoutGroup 节点）；解释器自动接线 viewport/content；horizontal=false，scrollSensitivity=20 |
| `Mask` | 复用 `background` | showMaskGraphic=false，白色底 |
| `HorizontalLayoutGroup` / `VerticalLayoutGroup` | `layout: {type, spacing, padding[左,上,右,下], align}` | childControlWidth/Height=false，childForceExpandWidth/Height=false（与手写生成器一致）；容器 size 为 0 时按内容撑开（viewer 同规则） |
| `Slider` | 复用 `background` | **约定子节点**：`FillArea`（内含 Fill）+ `HandleArea`（内含 Handle），解释器补建 Fill/Handle 图形并接线 |
| `Toggle` | — | **约定子节点**：`Background`（内含 Checkmark）+ `Label`；解释器补建 Checkmark 并接线 graphic |
| `Xxx.cs` | — | 脚本组件：类名解析（见 §5.5）后 AddComponent；View 挂根节点后自动设置 UIAutoBinder.ViewTypeName |

### 4.3 颜色格式

`#RRGGBB`（不透明）与 `rgba(r,g,b,a)`（半透明，如 Placeholder 的 `rgba(144,164,174,0.50)`）；与 viewer.js 的 CSS 渲染格式保持一致。

### 4.4 v1.2 扩展字段（新增提案）

现有 10 份设计稿未用到、但通用化后需要的能力：

| 字段 | 类型 | 用途 | 示例 |
|------|------|------|------|
| `ref` | string | 嵌套预制体实例（相对 `AssetPackage/Prefabs/UI/` 的路径），实例化后保留 spec 中的锚点覆盖 | `"ref": "Common/FriendItem"` |
| `note` | string | 节点备注（仅文档用途，解释器忽略；tools/UIHome.spec.json 已有先例） | `"note": "行模板，运行时克隆"` |
| `sprite` | string | 引用程序化 Sprite 资产路径（如渐变背景），Image 用 sprite 替代纯色 | `"sprite": "Textures/UI/BgGradient"` |
| `fontStyle` | string | Text 斜体/加粗（Placeholder 需斜体，当前为隐式约定） | `"fontStyle": "italic"` |
| `scale` | `[sx, sy]` | 节点缩放 → RectTransform.localScale（默认 [1,1]；由 HTML 可视化编辑器写入） | `"scale": [1.2, 1.2]` |
| `rotation` | number | 旋转角度(deg) → localRotation = Euler(0,0,deg)（默认 0；编辑器属性面板写入） | `"rotation": 15` |

> **兼容性**：扩展字段全部可选，旧规格零改动即可被解释。viewer.js 未识别的字段自然忽略，预览不受影响。

---

## 五、构建流程详解

### 5.1 流程总览

```
输入: <页面>.html 路径
 ① 提取   —— 读文件，正则捕获 <script ... id="ui-spec"> ... </script> 的 JSON 文本
 ② 校验   —— 反序列化为 UISpecNode；命名规范/组件名/约定子节点/绑定字段匹配检查
 ③ 递归构建 —— BuildNode(node, parent):
        创建 GameObject → RectTransform（anchors/pivot/position/size）
        → 按注册表依次执行各组件 Builder
        → active=false 则 SetActive(false)
        → 递归 children
 ④ 复合接线 —— 全树完成后二次处理：ScrollRect/ InputField/ Button/ Slider/Toggle 的引用挂接
 ⑤ 脚本挂载 —— .cs 组件类型解析（已在③挂载，此处仅校验 UIAutoBinder.ViewTypeName）
 ⑥ 字段绑定 —— m_Xxx/mi_Xxx 节点 → View/行视图 [SerializeField] 字段
 ⑦ 保存   —— SaveAsPrefabAsset 原地覆盖 → SaveAssets/Refresh → Ping
```

### 5.2 递归构建伪代码

```csharp
GameObject BuildNode(UISpecNode node, Transform parent, BuildContext ctx)
{
    GameObject go = new GameObject(node.name);
    go.transform.SetParent(parent, false);

    RectTransform rt = go.AddComponent<RectTransform>();
    rt.anchorMin = node.Anchors.Min;  rt.anchorMax = node.Anchors.Max;
    rt.pivot = node.Pivot;
    rt.anchoredPosition = node.Position;
    rt.sizeDelta = node.Size;
    rt.localScale = node.Scale;                                  // v1.2，缺省 [1,1]
    rt.localRotation = Quaternion.Euler(0, 0, node.Rotation);    // v1.2，缺省 0

    foreach (string comp in node.Components)
        if (comp != "RectTransform")
            _builders[comp].Build(go, node, ctx);   // 未知组件名在②校验期已拦截

    if (node.Active == false) go.SetActive(false);

    foreach (UISpecNode child in node.Children)
        BuildNode(child, go.transform, ctx);

    ctx.Register(node, go);   // 供④复合接线与⑥绑定阶段按名查找
    return go;
}
```

### 5.3 复合接线（约定优于配置）

构建完成后按**子节点名约定**统一接线，不在 JSON 中表达引用：

| 宿主组件 | 约定子节点 | 接线动作 |
|---------|-----------|---------|
| Button | 自身 Image | `button.targetGraphic = image` |
| InputField | `Text`、`Placeholder` | `textComponent` / `placeholder` |
| ScrollRect | `Viewport`（其下第一个 LayoutGroup 节点） | `viewport` / `content`，`horizontal=false` |
| Slider | `FillArea`/`Fill`、`HandleArea`/`Handle`（Fill/Handle 由解释器补建） | `fillRect` / `handleRect` |
| Toggle | `Background`/`Checkmark`（Checkmark 由解释器补建）、`Label` | `graphic` / `label`（Toggle label 为继承字段） |

### 5.4 校验规则（干跑即报）

| 检查 | 失败示例 | 报错形式 |
|------|---------|---------|
| JSON 可解析 | script 标签缺失/JSON 语法错误 | 阻断：文件名 + 语法错误位置 |
| 组件名已知 | `"components": ["Imge"]` 拼错 | 阻断：节点路径 + 未知组件名 + 已注册列表 |
| 约定子节点 | ScrollRect 无 `Viewport` 子节点 | 阻断：节点路径 + 缺失项 |
| 绑定命名规范 | `m_startBtn`（后首字母小写） | 警告：节点不会被绑定，但仍按普通节点构建 |
| 绑定字段可匹配 | View 无 `m_StartBtn` 字段 | 警告：列出节点名与 View 类名，便于补字段 |
| 脚本类型可解析 | `XxxView.cs` 找不到类型 | 阻断：提示先用 UIPanelGenerator 生成骨架 |

### 5.5 脚本组件类型解析

`components` 中的 `Xxx.cs` → 去后缀取类名 → 在 `DualEnigma.UI`、`DualEnigma.Framework.UI`、`DualEnigma.UI.Components` 命名空间下用 `Type.GetType`（含程序集限定）解析；解析不到即校验失败。根节点 View 挂载后同步设置 `UIAutoBinder.ViewTypeName`（与现有生成器一致）。

### 5.6 View 字段绑定

- 遍历树中名为 `m_Xxx`/`mi_Xxx` 的节点（后首字母大写，`mi_` 统一映射 `m_`，与 UIAutoBinder 规范一致）
- 目标：根节点 View 组件 + 各**行视图/卡片视图**组件（如 FriendRowView、InviteCardView——绑定发生在其所在子树内最近的视图脚本上）
- 绑定方式：`SerializedObject.FindProperty("m_Xxx")` + 按字段类型取节点上的对应组件（GameObject/Transform/具体组件类型），`ApplyModifiedProperties`
- 等价于手写生成器中每个面板 60~90 行的 `BindViewFields/BindRowView` 段

### 5.7 保存与 GUID 稳定

```
输出路径: Assets/AssetPackage/Prefabs/UI/<页面名>/<页面名>.prefab
方式:     PrefabUtility.SaveAsPrefabAsset(root, path)   // 原地覆盖，GUID 不变
收尾:     DestroyImmediate(root) → AssetDatabase.SaveAssets/Refresh → Ping
```

> 与手写生成器的差异：现流程先 `DeleteExistingAsset` 再保存，GUID 每次重建都会变化；目前仅因运行时按路径加载（ResMgr/AB）未暴露问题。通用生成器改为原地覆盖后，场景内/Asset 间对预制体的 GUID 引用得以稳定。

---

## 六、与现有工具的关系

| 工具 | 现状 | 通用生成器落地后 |
|------|------|----------------|
| `Generate{Panel}Prefab.cs` × 9 + `GenerateFriendItemPrefab.cs` | 手写转写规格，约 4000 行 | **退役删除**（迁移验证通过后） |
| `UIPrefabCreator.cs`（UITest 旧工具，145 行） | 早期测试工具 | **退役删除** |
| `UIPanelGenerator.cs`（MVC 三件套骨架） | 新面板第一步 | **保留**，仍是流程起点（生成 View/Ctrl/Model 代码骨架） |
| `UIBindingGenerator.cs`（运行时绑定规范） | m_Xxx 规范来源 | **保留**，命名规范被 UISpecViewBinder 复用 |
| `UIAutoBinder.cs`（ViewTypeName 约定） | 根节点挂载 | **保留**，解释器照常挂载并设置 ViewTypeName |
| `gen_ui_html.py`（预制体→HTML 反向导出） | 引导期一次性使用 | **保留并复用**为一致性校验工具（§二 能力 10） |
| `viewer.js` / `index.html`（浏览器预览） | 设计稿渲染 | **保留**，改用抽出的 `assets/spec-core.js` 公共数学（v1.2 的 scale/rotation 预览同步支持） |
| `editor/editor.html` + `tools/ui_editor.py`（HTML 可视化编辑器） | 布局生产环节（设计定稿待实现，见《HTML可视化编辑器.md》） | **新增**：拖拽/手柄/锚点补偿编辑布局，本地服务直写回 HTML 内嵌 JSON |
| `ConfigAssetGenerator.cs` 等非 UI 生成器 | 配置资产生成 | 不受影响 |

---

## 七、新工作流闭环

```
① UIPanelGenerator 生成 MVC 三件套骨架（新面板一次性）
        ↓
② 手写 JSON 起稿（可选，只写结构不做布局）
        ↓ editor/editor.html 可视化编辑（python tools/ui_editor.py 启动）
        ↓ 拖拽/手柄/锚点补偿调整布局 → Ctrl+S 写回 HTML
③ 「校验设计稿」干跑通过 → 设计定稿
        ↓
④ 「从设计稿生成预制体」→ 预制体入库 + View 字段已自动绑定
        ↓
⑤ Ctrl/Model 逻辑实现（引用均已就绪）
        ↓
⑥ 改设计 → 编辑器调整 → 保存 → 重新生成（GUID 不变，场景/代码引用不断）
        ↓
⑦ （可选）gen_ui_html.py 反向导出 diff —— 一致性对账
```

各角色成本变化：

| 场景 | 现状 | 新流程 |
|------|------|--------|
| 新增面板 | 设计稿 + ~500 行手写生成器 + 手写绑定 | 设计稿 + 点两次菜单 |
| 改布局/颜色/文案 | 改 HTML + 改 C# 常量 + 重新生成 | 改 HTML + 一次重新生成 |
| 加一个绑定引用 | 改 HTML + 改 View + 改生成器绑定段 | 改 HTML（命名规范即绑定）+ 改 View |
| 核对设计稿与实际预制体 | 人眼对照 | 反向导出 diff |

---

## 八、边界、约束与风险

### 8.1 不做的事（明确边界）

- **不做运行时解释**：纯 Editor 工具，运行时加载的是普通预制体
- **不生成逻辑代码**：Ctrl/Model 业务逻辑仍由 UIPanelGenerator 骨架 + 人工实现
- **不管理 AB 打包**：BuildTool 职责不变
- **不处理动效**：动画/Tween 仍需在代码或 Animator 中制作（UI/UX设计文档 §八 的动效规范属于运行时逻辑）
- **不做增量合并**：每次全量重建；预制体上人工手改的内容会被覆盖（约定：所有结构变更必须走 HTML）

### 8.2 风险与对策

| 风险 | 对策 |
|------|------|
| HTML 被误改导致 JSON 解析失败 | 干跑校验模式先跑；错误信息定位到具体节点/字段 |
| `gen_ui_html.py` 会清理目录下 `*.md`（脚本注释自述"移除旧 README.md / *.md"），误删本文档 | 方向 A 引导期已完成使命；重跑前先调整该脚本的清理列表（见 §十 待定事项） |
| 全量重建覆盖人工微调（如 Inspector 里手调的参数） | 约定"规格外的东西不放预制体上"；需要的一律进 spec 扩展字段 |
| View 字段类型与节点组件不匹配 | 校验期警告；绑定失败不阻断，输出清单 |
| tools/UIHome.spec.json 与 UIHome.html 内嵌规格存在历史版本偏差（v1/v2 布局） | 以 HTML 内嵌规格为唯一数据源；tools/*.spec.json 标记为废弃，或由 gen_ui_html.py 重新导出覆盖 |

---

## 九、实施计划

| 阶段 | 内容 | 验收标准 | 预估 |
|------|------|---------|------|
| P0 核心链路 | UISpecExtractor / UISpecNode / PrefabBuilder（RectTransform+Image+Text+Button+脚本组件）/ ViewBinder / 菜单窗口 | 用 UITest、UIHome 生成预制体，与手写产物经 gen_ui_html.py 反向导出 diff，布局/颜色/文案一致 | ~1 天 |
| P1 复合组件 | InputField / ScrollRect / Mask / LayoutGroup / Slider / Toggle + 干跑校验完整规则 | UILogin（InputField）、UIFriends（ScrollRect+行模板）、UISettings（Slider/Toggle）三个最复杂页面通过 diff 验证 | ~1 天 |
| P2 迁移退役 | 9 页面 + Common 全量迁移；逐个 diff 验证；删除 10 个手写生成器 + UIPrefabCreator | 全部页面由通用生成器产出；`Client/Assets/Editor/` 下 Generate*UI*.cs 清零；工程编译 0 error | ~0.5 天 |
| P3 增强项 | 嵌套预制体 ref、sprite/渐变、批量生成窗口、一致性校验菜单化 | UIFriends 行模板改用 ref 实例化 FriendItem | ~0.5 天 |

> **验证方法**：以现有 9 个预制体为基准（手写生成器的产物），通用生成器重新产出后用 `gen_ui_html.py` 分别反向导出 HTML，比对两份 ui-spec JSON——节点树、锚点/位置/尺寸、颜色、文案逐字段一致（数值容差 ±0.5）即为通过。这本身也是对能力 10（一致性闭环）的首场实战。
>
> **编辑器侧（上游）**：实施计划 E0~E3 见《HTML可视化编辑器.md》§七（约 2.5 天）。两线可并行——本工具 P0 与编辑器 E1 完成即可串联首版全链路（编辑器拖拽 → 保存 → 生成预制体）。

---

## 十、待定事项

- [ ] `gen_ui_html.py` 清理行为调整：重跑时不再删除目录下 `*.md`（本文档及后续技术文档的存续依赖此项）→ 🔲 待实现 P3 前完成
- [ ] 全量重建 vs 增量保留人工修改 → 当前决策：全量重建（§8.1），后续如有强需求再引入白名单节点
- [ ] `tools/*.spec.json` 处置：标记废弃 or 由反向导出覆盖 → 🔲 待讨论
- [ ] 脚本组件类型解析范围：固定命名空间列表 vs 全程序集扫描 → 当前决策：固定列表（§5.5），解析失败即报错兜底
- [ ] View 绑定的类型推断细节：节点同时挂多个组件时按字段类型优先级（具体组件 > Transform > GameObject）→ 待实现时确认
- [ ] index.html 工作流描述更新（"由后续生成工具解析 JSON 产出..."改为本工具实际菜单路径）→ P2 完成后同步

---

> **附注**: 本方案不改变 MVC/UIManager/UIAutoBinder 等既有架构与命名规范，仅将「规格 → 预制体」的转写环节从人工手写 500 行/面板 变为通用解释器一次实现、全面板复用。设计稿（HTML 内嵌 ui-spec JSON）自此成为 UI 结构的唯一数据源，配合 gen_ui_html.py 反向导出形成双向可对账的闭环。
