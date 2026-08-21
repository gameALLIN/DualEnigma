# -*- coding: utf-8 -*-
"""
UIPrefab HTML 设计稿生成器（双向工作流）
- 方向 A（反向导出/一致性对账）: 从现有预制体反向导出 HTML 设计稿
  （预览容器 + 层级树容器 + 内嵌 JSON 规格，渲染由 viewer.js 在浏览器端完成）
- 方向 B（正向起稿）: 新页面先有手写 spec JSON 时，可用 --build-page 生成页面骨架

输出: <页面名>.html × N + index.html（输出目录可用 --out 覆盖，便于导出到临时目录做 diff 对账，
      不覆盖手工维护的设计稿 —— 设计稿 HTML 内嵌 ui-spec 是唯一数据源）

用法:
    python tools/gen_ui_html.py                  # 全量反向导出（pages + index → OUT_DIR）
    python tools/gen_ui_html.py --out <目录>     # 导出到指定目录（对账用，不动设计稿）
    python tools/gen_ui_html.py --index-only     # 只重写索引页 index.html
    python tools/gen_ui_html.py --build-page <页面名> <spec.json> [预制体相对路径]

注意: 本脚本不再删除目录下任何文件（早期版本会清理 *.md/_预览.html，已移除该行为）。
"""
import argparse
import glob
import html as H
import json
import math
import os
import re
import shutil

ASSETS = r"D:\PersonProjects\DualEnigma\Client\Assets"
UI_DIR = os.path.join(ASSETS, "AssetPackage", "Prefabs", "UI")
OUT_DIR = r"D:\PersonProjects\DualEnigma\TechnicalDocs\Client\UIPrefab"
REPO_ROOT = r"D:\PersonProjects\DualEnigma"
CLIENT_ROOT = os.path.join(REPO_ROOT, "Client")

# uGUI 内置组件（guid 解析自 PackageCache 包脚本；导出时不带 .cs 后缀）
UGUI_BUILTIN = {
    "HorizontalLayoutGroup", "VerticalLayoutGroup", "InputField", "Mask",
    "Slider", "Toggle", "Button", "ScrollRect", "Text", "Image", "RawImage",
}

# ---------- GUID → 资产路径 映射（懒加载） ----------

guid_map = {}
asset_path_map = {}
_maps_ready = False


def ensure_maps():
    global _maps_ready
    if _maps_ready:
        return
    scan_roots = [ASSETS]
    # ugui 包脚本 guid（用于解析 LayoutGroup/InputField/Mask 等内置组件名）
    for hit in glob.glob(os.path.join(CLIENT_ROOT, "Library", "PackageCache", "com.unity.ugui@*")):
        scan_roots.append(hit)
    for root in scan_roots:
        for meta in glob.glob(os.path.join(root, "**", "*.meta"), recursive=True):
            try:
                with open(meta, encoding="utf-8") as f:
                    m = re.search(r"guid: ([0-9a-f]{32})", f.read())
                if not m:
                    continue
                stem = meta[:-5]  # 去掉 .meta
                asset_path_map[m.group(1)] = stem
                if stem.endswith(".cs"):
                    guid_map[m.group(1)] = os.path.splitext(os.path.basename(stem))[0]
            except OSError:
                pass
    _maps_ready = True


def sprite_gradient(sprite_ref):
    """Image 引用了 Sprite 资源时，解析其 Texture2D 像素，返回 CSS 背景（目前支持垂直渐变）。"""
    m = re.search(r"guid: ([0-9a-f]{32})", sprite_ref or "")
    if not m:
        return None
    sasset = asset_path_map.get(m.group(1))
    if not sasset or not sasset.endswith(".asset") or not os.path.exists(sasset):
        return None
    try:
        sbody = open(sasset, encoding="utf-8").read()
    except OSError:
        return None
    tm = re.search(r"texture: \{fileID: \d+, guid: ([0-9a-f]{32})", sbody)
    if not tm:
        return None
    tasset = asset_path_map.get(tm.group(1))
    if not tasset or not os.path.exists(tasset):
        return None
    try:
        tbody = open(tasset, encoding="utf-8").read()
    except OSError:
        return None
    dm = re.search(r"_typelessdata: ([0-9a-f]+)", tbody)
    wm = re.search(r"m_Width: (\d+)", tbody)
    hm = re.search(r"m_Height: (\d+)", tbody)
    if not (dm and wm and hm):
        return None
    data, w, h = dm.group(1), int(wm.group(1)), int(hm.group(1))
    if len(data) < w * h * 8:
        return None

    def px(i):
        p = data[i * 8: i * 8 + 8]
        return "#%s%s%s" % (p[0:2], p[2:4], p[4:6])

    # Unity 纹理数据从底部行开始；采样底/中/顶三行生成垂直渐变
    bottom, middle, top = px(0), px((h // 2) * w), px((h - 1) * w)
    if bottom == top:
        return bottom
    return "linear-gradient(to top, %s 0%%, %s 50%%, %s 100%%)" % (bottom, middle, top)


CLASS_NAMES = {1: "GameObject", 114: "MonoBehaviour", 222: "CanvasRenderer", 224: "RectTransform"}
ALIGN = {0: "UpperLeft", 1: "UpperCenter", 2: "UpperRight", 3: "MiddleLeft",
         4: "MiddleCenter", 5: "MiddleRight", 6: "LowerLeft", 7: "LowerCenter", 8: "LowerRight"}


def mono_type(body):
    if "m_FontData" in body:
        return "Text"
    if "m_OnClick" in body:
        return "Button"
    if "m_Content" in body and "m_Horizontal" in body:
        return "ScrollRect"
    if "m_FillRect" in body:
        return "Slider"
    if "m_IsOn" in body and "m_Group" in body:
        return "Toggle"
    if "m_TextComponent" in body:
        return "InputField"
    if "m_ShowMaskGraphic" in body:
        return "Mask"
    if "m_ChildAlignment" in body and "m_Spacing" in body:
        return "LayoutGroup"  # H/V 需经 guid 进一步区分
    if "m_Sprite" in body or "m_Type" in body:
        return "Image"
    return "Mono"


def vec(s):
    m = re.match(r"\{x: ([-\d.eE]+), y: ([-\d.eE]+)", s or "")
    return (float(m.group(1)), float(m.group(2))) if m else (0.0, 0.0)


def quat_z_deg(s):
    """m_LocalRotation 四元数 → z 轴欧拉角（度）。仅 x=y=0 的纯 z 旋转有意义。"""
    m = re.match(r"\{x: ([-\d.eE]+), y: ([-\d.eE]+), z: ([-\d.eE]+), w: ([-\d.eE]+)\}", s or "")
    if not m:
        return 0.0
    x, y, z, w = (float(g) for g in m.groups())
    if abs(x) > 1e-4 or abs(y) > 1e-4:
        return 0.0
    deg = math.degrees(2.0 * math.atan2(z, w))
    # 归一化到 (-180, 180]
    while deg > 180.0:
        deg -= 360.0
    while deg <= -180.0:
        deg += 360.0
    return round(deg, 1)


def parse_color(s):
    m = re.match(r"\{r: ([\d.eE+-]+), g: ([\d.eE+-]+), b: ([\d.eE+-]+), a: ([\d.eE+-]+)\}", s or "")
    if not m:
        return None
    r, g, b, a = (min(1.0, max(0.0, float(x))) for x in m.groups())
    if a < 0.99:
        return "rgba(%d,%d,%d,%.2f)" % (round(r * 255), round(g * 255), round(b * 255), a)
    return "#%02X%02X%02X" % (round(r * 255), round(g * 255), round(b * 255))


def parse(path):
    with open(path, encoding="utf-8") as f:
        content = f.read()
    docs = {}
    for m in re.finditer(r"^--- !u!(\d+) &(\d+)(?: stripped)?\s*\n(.*?)(?=^--- !u!|\Z)", content, re.S | re.M):
        cls, fid, body = m.group(1), m.group(2), m.group(3)
        fields, children = {}, []
        for line in body.splitlines():
            fm = re.match(r"^  ([A-Za-z0-9_]+): (.*)$", line)
            if fm:
                fields[fm.group(1)] = fm.group(2)
            cm = re.match(r"^  - \{fileID: (\d+)\}\s*$", line)
            if cm:
                children.append(cm.group(1))
        docs[fid] = {"cls": CLASS_NAMES.get(int(cls), cls), "fields": fields,
                     "children": children,
                     "comps": re.findall(r"component: \{fileID: (\d+)\}", body), "body": body}
    return docs


def build(path):
    ensure_maps()
    docs = parse(path)
    rt_go = {}
    for fid, d in docs.items():
        if d["cls"] == "RectTransform":
            g = re.search(r"\{fileID: (\d+)\}", d["fields"].get("m_GameObject", ""))
            if g:
                rt_go[fid] = g.group(1)

    def comp_infos(go):
        out = []
        for c in docs[go]["comps"]:
            d = docs.get(c)
            if not d:
                continue
            if d["cls"] == "MonoBehaviour":
                g = re.search(r"guid: ([0-9a-f]{32})", d["fields"].get("m_Script", ""))
                name = guid_map.get(g.group(1), "") if g else ""
                if name in UGUI_BUILTIN:
                    out.append((name, "", d))  # uGUI 内置组件：不带 .cs 后缀
                elif name:
                    out.append(("Script", name + ".cs", d))
                else:
                    out.append((mono_type(d["body"]), "", d))
            else:
                out.append((d["cls"], "", d))
        return out

    root_rt = next(fid for fid, d in docs.items()
                   if d["cls"] == "RectTransform" and d["fields"].get("m_Father", "").startswith("{fileID: 0}"))
    nodes = []

    def walk(rt, depth):
        go = rt_go.get(rt)
        d = docs[rt]
        if go not in docs:
            return
        infos = comp_infos(go)
        types = {t for t, _, _ in infos}
        text_comp = next((c for t, _, c in infos if t == "Text"), None)
        img_comp = next((c for t, _, c in infos if t == "Image"), None)
        txt, tcolor, fsize, align = None, None, None, None
        if text_comp:
            tm = re.search(r'm_Text: "(.*)"', text_comp["body"])
            txt = tm.group(1) if tm else ""
            if "\\u" in txt:
                try:
                    txt = txt.encode("latin-1", errors="ignore").decode("unicode_escape", errors="ignore")
                except Exception:
                    pass
            tcolor = parse_color(text_comp["fields"].get("m_Color"))
            sm = re.search(r"m_FontSize: (\d+)", text_comp["body"])
            am = re.search(r"m_Alignment: (\d+)", text_comp["body"])
            fsize = int(sm.group(1)) if sm else None
            align = ALIGN.get(int(am.group(1))) if am else None
        comps = [(n if t == "Script" else t) for t, n, _ in infos]
        # LayoutGroup 参数导出（type/spacing/padding[左,上,右,下]/align）
        layout = None
        layout_comp = next((c for t, _, c in infos
                            if t in ("HorizontalLayoutGroup", "VerticalLayoutGroup", "LayoutGroup")), None)
        if layout_comp is not None:
            lf = layout_comp["fields"]
            lbody = layout_comp["body"]

            def _num(key, default=0.0):
                m2 = re.search(key + r": ([-\d.eE]+)", lbody)
                return float(m2.group(1)) if m2 else default

            def _clean(v):
                return int(v) if abs(v - round(v)) < 1e-6 else round(v, 4)

            layout = {
                "type": "horizontal" if any(t == "HorizontalLayoutGroup" for t, _, _ in infos) else "vertical",
                "spacing": _clean(_num("m_Spacing")),
                "align": ALIGN.get(int(_num("m_ChildAlignment")), "UpperLeft"),
                "padding": [_clean(_num("m_Left")), _clean(_num("m_Top")),
                            _clean(_num("m_Right")), _clean(_num("m_Bottom"))],
            }
        bgcolor = None
        if img_comp:
            bgcolor = (sprite_gradient(img_comp["fields"].get("m_Sprite"))
                       or parse_color(img_comp["fields"].get("m_Color")))
        # v1.2 变换导出：localScale / localRotation(z)
        sx, sy = vec(d["fields"].get("m_LocalScale"))
        rot = quat_z_deg(d["fields"].get("m_LocalRotation"))
        nodes.append({
            "name": docs[go]["fields"].get("m_Name", "?"), "depth": depth,
            "active": docs[go]["fields"].get("m_IsActive", "1") != "0",
            "amin": vec(d["fields"].get("m_AnchorMin")), "amax": vec(d["fields"].get("m_AnchorMax")),
            "pos": vec(d["fields"].get("m_AnchoredPosition")), "size": vec(d["fields"].get("m_SizeDelta")),
            "pivot": vec(d["fields"].get("m_Pivot")), "comps": comps,
            "isText": text_comp is not None, "isBtn": "Button" in types, "isImg": img_comp is not None,
            "txt": txt, "tcolor": tcolor or "#ECEFF1", "fsize": fsize, "align": align,
            "bgcolor": bgcolor,
            "scale": (round(sx, 4), round(sy, 4)), "rotation": rot,
            "layout": layout,
        })
        for ch in d["children"]:
            if ch in docs:
                walk(ch, depth + 1)

    walk(root_rt, 0)
    kids, stack = {}, []
    for i, n in enumerate(nodes):
        while stack and nodes[stack[-1]]["depth"] >= n["depth"]:
            stack.pop()
        if stack:
            kids.setdefault(stack[-1], []).append(i)
        stack.append(i)
    return nodes, kids


def to_json(i, nodes, kids):
    n = nodes[i]
    d = {"name": n["name"], "active": n["active"], "components": n["comps"],
         "anchors": {"min": list(n["amin"]), "max": list(n["amax"])},
         "pivot": list(n["pivot"]), "position": list(n["pos"]), "size": list(n["size"])}
    if n["isText"]:
        d["text"] = n["txt"] or ""
        if n["fsize"]:
            d["fontSize"] = n["fsize"]
        if n["align"]:
            d["align"] = n["align"]
        d["color"] = n["tcolor"]
    if n["isImg"] or n["isBtn"]:
        d["background"] = n["bgcolor"] or "#FFFFFF"
    if n.get("layout"):
        d["layout"] = n["layout"]
    # v1.2 扩展字段：仅在非默认时导出（一致性对账不丢字段）
    sx, sy = n["scale"]
    if abs(sx - 1.0) > 0.0005 or abs(sy - 1.0) > 0.0005:
        d["scale"] = [sx, sy]
    if abs(n["rotation"]) > 0.05:
        d["rotation"] = n["rotation"]
    ch = [to_json(c, nodes, kids) for c in kids.get(i, [])]
    if ch:
        d["children"] = ch
    return d


def count_nodes(spec):
    return 1 + sum(count_nodes(c) for c in spec.get("children", []))


PAGE_TMPL = """<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>@@NAME@@ · 页面设计稿</title>
<link rel="stylesheet" href="../assets/viewer.css">
</head>
<body>
<h1>@@NAME@@ <small>@@SUB@@ · <a href="../index.html">← 索引</a></small></h1>
<div class="toolbar">
  <label>缩放 <select id="zoom"><option value="0.5">50%</option><option value="0.75" selected>75%</option><option value="1">100%</option></select></label>
  <label><input type="checkbox" id="showHidden"> 显示隐藏节点</label>
</div>
<div class="wrap">
  <div>
    <div id="preview" class="page"></div>
    <div class="legend">按 ui-spec 锚点实时渲染（参考分辨率 1280×720）· 斜体灰字 = 动态文本占位 · 勾选「显示隐藏节点」可查看 ✕隐藏 节点（橙色虚线）</div>
  </div>
  <pre id="tree" class="tree"></pre>
</div>
<script type="application/json" id="ui-spec">
@@JSON@@
</script>
<script src="../assets/spec-core.js"></script>
<script src="../assets/viewer.js"></script>
</body>
</html>
"""

# 索引页行序与说明文案（新页面追加在后，按名排序）
PAGE_ORDER = ["UILogin", "UIHome", "UIFriends", "UIRoom", "UIGameHud",
              "UIGameOver", "UISettings", "UIInvitePopup", "Common", "UITest"]
PAGE_DESCR = {
    "UILogin": "登录 / 注册（含隐藏昵称行、错误提示、加载态）",
    "UIHome": "主界面（开始游戏、功能入口、玩家卡片、邀请抽屉、标题区）",
    "UIFriends": "好友（列表 / 搜索 / 房间邀请 / 好友申请）",
    "UIRoom": "组队房间（房间码、邀请、开始、退出）",
    "UIGameHud": "对局 HUD（阶段进度、双角色状态、碎片计数）",
    "UIGameOver": "结算（胜利 / 失败、再来一局、返回主界面）",
    "UISettings": "设置（音量、性能开关、继续 / 退出）",
    "UIInvitePopup": "邀请弹窗（房间邀请卡、好友申请卡，默认隐藏）",
    "Common": "通用组件（好友列表项 FriendItem）",
    "UITest": "测试面板（计数 / 重置 / 关闭）",
}

INDEX_TMPL = """<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<title>UIPrefab · 设计稿索引</title>
<style>
  * { box-sizing: border-box; }
  body { background: #1a2226; font-family: "Microsoft YaHei", Consolas, monospace; color: #B0BEC5; margin: 24px; }
  h1 { color: #ECEFF1; }
  h2 { color: #ECEFF1; font-size: 16px; margin-top: 22px; }
  table { border-collapse: collapse; margin-top: 12px; }
  td, th { border: 1px solid #546E7A; padding: 6px 14px; text-align: left; }
  a { color: #4FC3F7; text-decoration: none; }
  a:hover { text-decoration: underline; }
  code { background: #263238; padding: 1px 5px; border-radius: 3px; }
  .flow { background: #202a30; border: 1px solid #4FC3F7; padding: 10px 14px; margin: 14px 0; max-width: 960px; font-size: 13px; line-height: 1.7; }
  .legend { margin-top: 14px; font-size: 12px; color: #78909C; max-width: 960px; line-height: 1.7; }
</style>
</head>
<body>
<h1>UI 页面设计稿索引</h1>
<div class="flow">
  📐 工作流：<b>先 HTML 后代码，设计稿（页面内嵌 ui-spec JSON）为唯一数据源</b> —<br>
  ① <code>UIPanelGenerator</code> 生成 MVC 三件套骨架（新面板一次性）
  → ② 手写 JSON 起稿（可选，只写结构不做布局）<br>
  ③ <code>python tools/ui_editor.py</code> → <a href="http://localhost:8765/">可视化编辑器</a> 调布局
  （拖拽 / 手柄 / 锚点补偿，Ctrl+S 直写回页面内嵌 ui-spec）<br>
  ④ Unity 菜单 <code>DualEnigma &gt; UI &gt; 校验设计稿</code> 干跑 →
  <code>DualEnigma &gt; UI &gt; 从设计稿生成预制体</code> 入库（原地覆盖，GUID 不变，场景/AB 引用不断）<br>
  ⑤ Ctrl/Model 逻辑实现（View 字段已按命名规范自动绑定）<br>
  ⑥ 迭代：编辑器调整 → 保存 → 重新生成
  → ⑦ （可选）<code>python tools/gen_ui_html.py --out &lt;临时目录&gt;</code> 反向导出，与源设计稿 diff 对账。
</div>
<table>
  <tr><th>页面</th><th>预制体来源</th><th>说明</th></tr>
@@ROWS@@
</table>
<h2>工具与文档</h2>
<table>
  <tr><td><a href="editor/editor.html">editor/editor.html</a></td><td>ui-spec 可视化编辑器（先启动 <code>python tools/ui_editor.py</code> 再访问；直接双击打开无法保存）</td></tr>
  <tr><td>assets/viewer.js · spec-core.js</td><td>设计稿浏览器端渲染（只读预览；spec-core 为预览/编辑器共享的锚点数学库）</td></tr>
  <tr><td><a href="通用JSON预制体生成器.md">通用JSON预制体生成器.md</a></td><td>ui-spec 解释器设计文档（组件规范 / 构建流程 / 校验规则）</td></tr>
  <tr><td><a href="HTML可视化编辑器.md">HTML可视化编辑器.md</a></td><td>可视化编辑器设计文档（交互 / 坐标数学 / 写回机制）</td></tr>
</table>
<div class="legend">
  图例：斜体灰字 = 动态文本占位（运行时由代码赋值）· 橙色虚线 = ✕隐藏节点（勾选「显示隐藏节点」后可见）·
  预览参考分辨率 1280×720，支持 50% / 75% / 100% 缩放。
</div>
</body>
</html>
"""


def page_subtitle(rel, root_comps):
    """页面 h1 副标题：预制体相对路径 · 根节点脚本列表"""
    scripts = ", ".join(c for c in root_comps if c.endswith(".cs"))
    return (rel + " · " + scripts) if scripts else rel


def _dumps(spec):
    """ui-spec JSON 排版：优先复用 ui_editor.dumps_spec（与手工设计稿逐字节一致）"""
    try:
        from ui_editor import dumps_spec
        return dumps_spec(spec)
    except ImportError:
        return json.dumps(spec, ensure_ascii=False, indent=1)


def write_page(out_dir, name, spec, rel):
    """写出页面设计稿到 out_dir/pages/<name>.html"""
    sub = page_subtitle(rel, spec.get("components", []))
    spec_json = _dumps(spec).replace("</", "<\\/")
    doc = (PAGE_TMPL.replace("@@NAME@@", name)
           .replace("@@SUB@@", H.escape(sub))
           .replace("@@JSON@@", spec_json))
    pages_dir = os.path.join(out_dir, "pages")
    os.makedirs(pages_dir, exist_ok=True)
    with open(os.path.join(pages_dir, name + ".html"), "w", encoding="utf-8", newline="\n") as f:
        f.write(doc)


def copy_assets(out_dir):
    """拷贝共享前端资源到输出目录（导出树可直接浏览器查看）"""
    src = os.path.join(OUT_DIR, "assets")
    if not os.path.isdir(src):
        return
    dst = os.path.join(out_dir, "assets")
    os.makedirs(dst, exist_ok=True)
    for name in ("viewer.css", "viewer.js", "spec-core.js"):
        p = os.path.join(src, name)
        if os.path.isfile(p):
            shutil.copy2(p, os.path.join(dst, name))


SPEC_TAG = re.compile(r'(<script[^>]*id="ui-spec"[^>]*>)(.*?)(</script>)', re.S)


def write_index(out_dir, pages):
    """pages: [(页面名, 预制体相对仓库根路径)]，按 PAGE_ORDER 排序重写 index.html"""
    def sort_key(p):
        return (PAGE_ORDER.index(p[0]) if p[0] in PAGE_ORDER else len(PAGE_ORDER), p[0])
    rows = []
    for name, rel in sorted(pages, key=sort_key):
        descr = PAGE_DESCR.get(name, "")
        rows.append('  <tr><td><a href="pages/%s.html">%s</a></td><td>%s</td><td>%s</td></tr>'
                    % (name, name, H.escape(rel), H.escape(descr)))
    doc = INDEX_TMPL.replace("@@ROWS@@", "\n".join(rows))
    with open(os.path.join(out_dir, "index.html"), "w", encoding="utf-8", newline="\n") as f:
        f.write(doc)


def scan_design_pages():
    """扫描设计稿目录 pages/（唯一数据源），返回 [(页面名, 预制体相对仓库根路径)]。
    预制体路径按约定推导：Client/Assets/AssetPackage/Prefabs/UI/<页面>/<根节点名>.prefab。
    以设计稿为准而非预制体目录 —— 预制体可能尚未生成（如 Common/FriendItem）。"""
    pages = []
    pages_dir = os.path.join(OUT_DIR, "pages")
    if not os.path.isdir(pages_dir):
        return pages
    for name in sorted(os.listdir(pages_dir)):
        if not name.endswith(".html"):
            continue
        stem = name[:-5]
        path = os.path.join(pages_dir, name)
        try:
            with open(path, encoding="utf-8") as f:
                html = f.read()
        except OSError:
            continue
        m = SPEC_TAG.search(html)
        if not m:
            continue
        try:
            spec = json.loads(m.group(2).strip().replace("<\\/", "</"))
        except json.JSONDecodeError:
            continue
        root = spec.get("name") or stem
        rel = f"Client/Assets/AssetPackage/Prefabs/UI/{stem}/{root}.prefab"
        pages.append((stem, rel))
    return pages


def export_all(out_dir):
    """方向 A：全量反向导出 pages/ + index.html + assets/（索引以设计稿目录为准）。"""
    exported = 0
    for page, rel in scan_design_pages():
        pf = os.path.join(REPO_ROOT, rel)
        if not os.path.exists(pf):
            print("skip (prefab 不存在，仅入索引):", page, "->", rel)
            continue
        nodes, kids = build(pf)
        spec = to_json(0, nodes, kids)
        write_page(out_dir, page, spec, rel)
        exported += 1
        print("exported:", page, "| nodes:", len(nodes))
    write_index(out_dir, scan_design_pages())
    copy_assets(out_dir)
    print("done ->", out_dir, "| exported:", exported)


def build_page_from_spec(name, json_path, rel, out_dir):
    """方向 B：从设计 spec JSON 生成页面骨架到 pages/（预览/层级树由 viewer.js 客户端渲染）。"""
    with open(json_path, encoding="utf-8") as f:
        spec = json.load(f)
    write_page(out_dir, name, spec, rel or "")
    print("built:", name, "| nodes:", count_nodes(spec))


def main():
    parser = argparse.ArgumentParser(description="UIPrefab HTML 设计稿生成器")
    parser.add_argument("--out", default=OUT_DIR, help="输出根目录（默认设计稿目录；对账请指向临时目录）")
    parser.add_argument("--index-only", action="store_true", help="只重写索引页 index.html")
    parser.add_argument("--build-page", nargs="+", metavar=("页面名", "spec.json"),
                        help="方向 B：从 spec JSON 生成页面骨架（可追加预制体相对路径）")
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)

    if args.build_page:
        name, json_path = args.build_page[0], args.build_page[1]
        rel = args.build_page[2] if len(args.build_page) > 2 else ""
        build_page_from_spec(name, json_path, rel, args.out)
        return

    if args.index_only:
        write_index(args.out, scan_design_pages())
        print("index rewritten ->", os.path.join(args.out, "index.html"))
        return

    export_all(args.out)


if __name__ == "__main__":
    main()
