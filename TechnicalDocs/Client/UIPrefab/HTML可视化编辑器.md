# HTML 可视化编辑器（ui-spec Editor）

> **文档版本**: v1.0  
> **最后更新**: 2026-08-20  
> **文档状态**: 设计定稿（工具待实现，实施计划见 §七）  
> **用途**: 在浏览器中可视化编辑 ui-spec 设计稿的布局属性（位置/大小/缩放/锚点/轴心），经本地服务写回 HTML 内嵌 JSON，衔接通用 JSON 预制体生成器，形成「拖拽即设计稿」的完整链路  
> **关联文档**: `TechnicalDocs/Client/UIPrefab/通用JSON预制体生成器.md`（JSON→预制体）、`viewer.js`（只读预览）、`tools/gen_ui_html.py`（反向导出）  
> **已定决策**: 保存方式 = 本地服务直写；形态 = 独立编辑器页；P0 编辑范围 = 布局四件套（位置/大小/缩放/锚点）+ 轴心

---

## 一、背景与定位

### 1.1 现状与痛点

`viewer.js` 只读渲染设计稿，所有布局调整都是「手改 JSON 数字 → 刷新 → 目测」：

- **锚点/位置/尺寸联动数学反直觉**：改 anchors 后 rect 会移动，正确补偿 position/size 需要脑算 anchoredPosition 语义
- **无即时反馈**：一次布局微调平均要 3~5 轮「改数字→刷新→目测→再改」循环
- **规模在涨**：现有 10 份设计稿 205 个节点，局内 UI（HUD/天赋/蓝图等）还没进来

### 1.2 定位

```
┌────────────────────────┐   保存（本地服务直写）   ┌──────────────────────┐
│ editor.html 可视化编辑  │ ───────────────────► │ ui-spec JSON          │
│ 拖拽/手柄/属性面板       │ ◄─────────────────── │ （内嵌于 <页面>.html）  │
└────────────────────────┘      重新加载          └──────────┬───────────┘
                                                            │ 通用 JSON 预制体生成器
                                                            ▼
                                                    Unity 预制体入库
```

编辑器只负责**布局数据的生产与修改**；设计稿 HTML 仍是唯一数据源；预制体生成仍走 Editor 侧通用生成器。

---

## 二、系统组成

### 2.1 文件布局

```
TechnicalDocs/Client/UIPrefab/
├── editor.html                      # 编辑器入口（新增）
├── editor.js / editor.css           # 编辑器前端（新增）
├── spec-core.js                     # 从 viewer.js 抽出的公共数学（calcRect/layoutChildren/parseAlign）（新增）
├── viewer.js                        # 保留：静态预览页复用 spec-core
├── <页面>.html                       # 设计稿（含内嵌 ui-spec JSON，被动读写）
└── tools/
    ├── ui_editor.py                 # 本地编辑服务（新增）
    └── gen_ui_html.py               # 保留：反向导出/一致性校验
```

> **spec-core.js 抽取原因**：viewer.js 与 editor.js 必须共享同一套「spec→屏幕矩形」数学，否则预览与编辑所见不一致。把 calcRect/layoutChildren/parseAlign 抽为公共模块，viewer.js 改为引用（行为不变）。

### 2.2 本地服务 `tools/ui_editor.py`

零第三方依赖，仅用 Python 标准库（http.server + json + re）。

```bash
# 启动（默认端口 8765，目录默认取脚本上两级）
python tools/ui_editor.py [--port 8765]
# 浏览器打开 http://localhost:8765/ → 即 editor.html
```

| 端点 | 方法 | 说明 |
|------|------|------|
| `/` | GET | 返回 editor.html |
| `/api/pages` | GET | 扫描目录下含 `id="ui-spec"` 的 `*.html`，返回页面名列表（排除 editor.html/index.html） |
| `/api/spec?page=UIHome` | GET | 返回该页面提取出的 ui-spec JSON |
| `/api/save?page=UIHome` | POST | body = 新的 ui-spec JSON，写回 HTML 文件 |

**写回机制（安全与格式）**：

1. 校验：body 先 `json.loads` 验证合法性，失败返回 400 与错误信息（不落盘）
2. 排版：`json.dumps(spec, ensure_ascii=False, indent=1)` —— 与现有内嵌 JSON 的 1 空格缩进、`"key": value` 风格一致，git diff 干净
3. 替换：正则定位 `<script type="application/json" id="ui-spec">...</script>` 块，只替换其内容，页面其余部分（标题/图例等）不动
4. 落盘：先写 `<页面>.html.tmp` 成功后 `os.replace` 原子替换；替换前把原文件复制为 `<页面>.html.bak`（单代备份，git 为最终保障）
5. 防护：页面名白名单校验（必须来自 /api/pages 结果），拒绝路径穿越

### 2.3 编辑器界面（editor.html）

```
┌──────────┬───────────────────────────────────┬──────────────────┐
│ 页面列表   │  工具栏: 缩放 50/75/100% · 网格开关  │  属性面板          │
│ ──────── │        吸附开关 · 撤销/重做 · 保存 ●  │  ─ 名称: StartBtn │
│ 层级树     │ ┌─────────────────────────────┐   │  ├ 位置  x  y    │
│ (✕隐藏    │ │      画布 1280×720            │   │  ├ 大小  w  h    │
│  节点     │ │  （选中框+8手柄+锚点指示）      │   │  ├ 缩放  sx sy   │
│  可选)    │ │                              │   │  ├ 锚点  9宫格    │
│           │ └─────────────────────────────┘   │  ├ 轴心  9点      │
│           │  状态栏: 选中节点路径 · 未保存标记    │  └ 旋转  deg      │
└──────────┴───────────────────────────────────┴──────────────────┘
```

| 区域 | 功能 |
|------|------|
| 左栏 | 页面切换（加载即切换编辑对象）；层级树与 viewer 树同构，✕ 隐藏节点可选（选中后画布高亮定位） |
| 画布 | 参考分辨率 1280×720，与 viewer 一致；渲染复用 spec-core；叠加：选中框、8 个尺寸手柄、父容器锚点参考框、网格 |
| 右栏 | 选中节点的属性数值面板（布局四件套 + 轴心 + 旋转），数值输入与画布拖拽双向同步 |

---

## 三、交互设计

| 操作 | 交互 | 数据映射 |
|------|------|---------|
| 选择 | 画布点击命中（自顶向下取最上层）；或层级树点选 | 只读状态更新属性面板 |
| 移动 | 按住元素拖拽；方向键 ±1px；Shift+方向键 ±10px | `position`（逆向公式 §4.2） |
| 调整大小 | 选中框 8 手柄拖拽（对边固定）；Shift+角手柄等比 | `size`（+必要时 `position` 联动） |
| 缩放 | 属性面板 sx/sy 数值输入（步进 0.1，范围 0.1~10） | `scale` |
| 锚点 | 9 宫格预设（左上…右下+拉伸）点击；默认**补偿开启**（§4.3） | `anchors`（+补偿重算 `position/size`） |
| 轴心 | 9 点选择，同样保持视觉矩形补偿 | `pivot`（+补偿重算 `position`） |
| 旋转 | 属性面板 deg 输入（P2 阶段支持） | `rotation` |
| 吸附 | 开关 + 步长（默认 1px，可设 2/4/8）；拖拽与手柄生效 | 舍入到步长 |
| 撤销/重做 | Ctrl+Z / Ctrl+Shift+Z；工具栏按钮 | JSON 快照栈（上限 50 步，结构级快照） |
| 保存 | Ctrl+S / 保存按钮 → POST /api/save | 成功后清除未保存标记 ● |
| 视图缩放 | 滚轮 25%~200%，空格+拖拽平移 | 仅视图，不动数据 |
| 离开保护 | 有未保存改动时 beforeunload 提示 | — |

> **锁定规则**：父节点带 `layout` 的子节点，其位置由布局排布接管——画布上显示锁定标记，**不可拖动**（拖了也会被运行时布局覆盖）；`size` 允许编辑（childControl=false 时尺寸有效）。属性面板对锁定节点给出灰色提示「位置由 LayoutGroup 接管」。

---

## 四、坐标数学（核心）

### 4.1 正向：spec → 屏幕矩形

复用 `spec-core.calcRect`（现 viewer.js 已实现）：按 anchors/pivot/position/size 在父矩形内求出视觉矩形（HTML 坐标系，y 向下）。scale/rotation 扩展后叠加 CSS `transform: scale(sx,sy) rotate(deg)`，`transform-origin` 对应 pivot。

### 4.2 逆向：屏幕矩形 → spec（编辑器新增）

拖拽/手柄得到新的**视觉矩形**后反解写回字段。设父矩形 `(px,py,pw,ph)`（屏幕坐标，y 向下）：

**点锚（min == max == a）**：

```
锚点像素位 anchorPt = (px + a.x*pw, py + (1-a.y)*ph)
矩形中心  center    = (L + w/2, T + h/2)
position = (center.x - anchorPt.x, anchorPt.y - center.y)   // y 翻转
size     = (w, h)
```

**拉伸锚（min ≠ max）**：

```
参考区域 refRect = 锚点min/max 之间的矩形（像素）
size     = (w - refRect.w, h - refRect.h)                   // sizeDelta 语义
position = (center.x - refRect.cx, refRect.cy - center.y)   // y 翻转
```

### 4.3 锚点/轴心变更补偿

改 `anchors`（或 `pivot`）时若直接替换字段，视觉矩形会跳变。补偿算法（默认开启，属性面板提供「不补偿」复选）：

```
① 记录旧锚点下的视觉矩形 rect（正向 calcRect）
② 写入新 anchors / pivot
③ 用 rect 走逆向公式重算 position / size
④ 三字段原子提交（一次撤销步）
```

### 4.4 scale 的处理

- **渲染**：布局矩形经 `transform: scale()` 得视觉矩形——视觉尺寸 = size × scale
- **手柄**：resize 手柄基于**布局矩形**（先把视觉矩形除以 scale 还原），写回的 size 不含 scale，避免两级缩放混叠
- **生成器侧**：`scale` → `RectTransform.localScale`（见生成器文档 §4.4 v1.2 扩展）

### 4.5 数值精度

写回前 position/size/anchors/scale 统一**保留 1 位小数**（0.05 四舍五入），避免浮点噪声污染 git diff。

---

## 五、ui-spec schema v1.2 扩展

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `scale` | `[sx, sy]` | `[1, 1]` | 可选；→ RectTransform.localScale |
| `rotation` | `deg` | `0` | 可选；→ Quaternion.Euler(0,0,deg) |

**配套改动**：

| 组件 | 改动 |
|------|------|
| viewer.js | 渲染叠加 CSS transform（scale/rotation），预览与编辑器一致 |
| 通用生成器 | RectTransform 构建解释 localScale / localRotation |
| gen_ui_html.py | 反向导出时读取 localScale/localRotation 写入这两个字段（一致性对账不丢字段） |

**已有特殊值的处理**：`background` 支持 `linear-gradient(...)` 字符串（UILogin 背景已在用）。编辑器 P0 对渐变背景**只读保留**（不提供编辑，原样写回），渐变编辑列为后续扩展。

---

## 六、更新后的完整工作流

```
① UIPanelGenerator 生成 MVC 三件套骨架（新面板一次性）
② 手写 JSON 起稿（可选，只写结构不做布局）
③ python tools/ui_editor.py → editor.html 可视化编辑布局
        拖拽/手柄/锚点补偿/属性面板 → Ctrl+S 写回 HTML
④ 「校验设计稿」干跑 → 「从设计稿生成预制体」
⑤ Ctrl/Model 逻辑实现
⑥ 迭代：编辑器改 → 保存 → 重新生成（GUID 不变）
⑦ （可选）gen_ui_html.py 反向导出 diff 对账
```

与现有工具关系：

| 工具 | 关系 |
|------|------|
| viewer.js | 保留为静态预览（改用 spec-core）；只读场景仍然有用（给别人看稿） |
| gen_ui_html.py | 保留为反向导出/对账；P3 前补充 scale/rotation 字段导出 |
| 通用生成器 | 下游不变，新增 scale/rotation 解释 |
| 手写 JSON 起稿 | 仍可选——结构（增删节点）编辑器 P0 不做，新增节点仍手写 JSON 后进编辑器调布局 |

---

## 七、实施计划

| 阶段 | 内容 | 验收标准 | 预估 |
|------|------|---------|------|
| E0 服务与框架 | ui_editor.py（静态托管/pages/spec/save 四端点 + 原子写 + .bak）；editor.html 三栏框架；spec-core.js 抽取（viewer.js 回归不变）；页面列表 + 层级树 + 选中态 | 启动服务能打开/切换页面、选节点、保存往返后 HTML diff 仅在 JSON 块内且格式一致 | ~0.5 天 |
| E1 核心编辑 | 拖动、8 手柄 resize、属性面板数值输入、逆向公式、撤销/重做、未保存标记、beforeunload | 对 UIHome 连续 10 次随机拖拽/resize 后保存，重新生成预制体布局与编辑器所见一致 | ~1 天 |
| E2 锚点与变换 | 9 宫格锚点 + 补偿算法、轴心 9 点、scale/rotation 字段 + viewer/生成器联动 | 改锚点视觉不跳变；scale 后生成预制体 localScale 正确 | ~0.5 天 |
| E3 体验打磨 | 吸附（步长可配）、网格、锁定节点标记、键盘微调、状态栏节点路径 | UIFriends（滚动列表+行模板）完整走查一遍无阻断 | ~0.5 天 |

> **端到端验收**（E3 后）：UIHome + UIFriends 各做一轮「编辑 → 保存 → 生成预制体 → gen_ui_html.py 反向导出 diff」——除有意修改的字段外 diff 为空。

---

## 八、边界与风险

| 项 | 说明 / 对策 |
|----|------------|
| 字体度量差异 | CSS 与 UGUI 字体渲染不同，文字视觉宽度为近似——布局数值为准，最终以 Unity 为准（viewer 时代已知现状） |
| LayoutGroup 子节点 | 位置锁定不可拖（§三）；size 可编辑；面板显示接管提示 |
| 并发写 | 单人开发不做文件锁；.bak 单代备份 + git 兜底；保存失败（文件被占用等）明确报错不清标记 |
| 浏览器兼容 | 仅用 fetch/DOM 标准能力，现代 Chromium/Firefox/Edge 均可（无 File System Access 依赖） |
| 服务安全 | 仅本机使用；页面名白名单；不做鉴权（localhost 绑定 127.0.0.1） |
| 数字精度 | §4.5 统一 1 位小数舍入 |
| `*.md` 清理风险 | 与生成器文档 §八 同一条：gen_ui_html.py 重跑会清目录下 *.md，调整前列为阻塞项 |

---

## 九、待定事项

- [ ] 外观属性编辑（颜色/文字/字号/对齐）→ P0 范围外，编辑器属性面板预留分页结构，后续按需加
- [ ] 结构操作（增删节点/复制/重命名/调层级）→ 后续版本（P0 阶段新增节点仍手写 JSON）
- [ ] 多选 / 对齐分布工具 → 后续
- [ ] 渐变背景（linear-gradient）可视化编辑 → 后续
- [ ] editor 保存后一键触发 Unity 批处理生成预制体（保存→生成全自动）→ 🔲 待讨论（收益 vs 复杂度）
- [ ] 吸附默认步长（1px or 8px 网格）→ E3 实测定
- [ ] index.html 工作流描述同步编辑器入口 → 实现完成后更新

---

> **附注**: 编辑器补全了「先 HTML 后代码」流程中缺失的**布局生产环节**：设计稿从"手写 JSON + 目测"升级为"拖拽即所得"。与通用 JSON 预制体生成器（文档另见）首尾相接——编辑器管 spec 的生产与修改，生成器管 spec 的解释与落地，设计稿 HTML 始终是唯一数据源。
