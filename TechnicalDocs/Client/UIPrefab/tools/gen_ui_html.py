# -*- coding: utf-8 -*-
"""
UIPrefab HTML 设计稿生成器（双向工作流）
- 方向 A（本脚本，引导期）: 从现有预制体反向导出 HTML 设计稿（预览 + 层级树 + 内嵌 JSON 规格）
- 方向 B（未来工作流）   : 新页面先在本目录手写/迭代 HTML（编辑 <script id="ui-spec"> 的 JSON），
                          确认后由后续生成工具解析 JSON 产出预制体生成器与 View/Ctrl 代码

输出: <页面名>.html × N + index.html；移除旧 README.md / *.md / _预览.html
"""
import os, re, glob, json, html as H

ASSETS = r"D:\PersonProjects\DualEnigma\Client\Assets"
UI_DIR = os.path.join(ASSETS, "AssetPackage", "Prefabs", "UI")
OUT_DIR = r"D:\PersonProjects\DualEnigma\TechnicalDocs\Client\UIPrefab"
CANVAS_W, CANVAS_H, SCALE = 1280, 720, 0.5

guid_map = {}
for meta in glob.glob(os.path.join(ASSETS, "**", "*.cs.meta"), recursive=True):
    try:
        with open(meta, encoding="utf-8") as f:
            m = re.search(r"guid: ([0-9a-f]{32})", f.read())
        if m:
            stem = os.path.splitext(os.path.basename(meta))[0]
            guid_map[m.group(1)] = stem[:-3] if stem.endswith(".cs") else stem
    except OSError:
        pass

CLASS_NAMES = {1: "GameObject", 114: "MonoBehaviour", 222: "CanvasRenderer", 224: "RectTransform"}
ALIGN = {0: "UpperLeft", 1: "UpperCenter", 2: "UpperRight", 3: "MiddleLeft",
         4: "MiddleCenter", 5: "MiddleRight", 6: "LowerLeft", 7: "LowerCenter", 8: "LowerRight"}

def mono_type(body):
    if "m_FontData" in body: return "Text"
    if "m_OnClick" in body: return "Button"
    if "m_Content" in body and "m_Horizontal" in body: return "ScrollRect"
    if "m_FillRect" in body: return "Slider"
    if "m_IsOn" in body and "m_Group" in body: return "Toggle"
    if "m_Sprite" in body or "m_Type" in body: return "Image"
    return "Mono"

def vec(s):
    m = re.match(r"\{x: ([-\d.eE]+), y: ([-\d.eE]+)\}", s or "")
    return (float(m.group(1)), float(m.group(2))) if m else (0.0, 0.0)

def parse_color(s):
    m = re.match(r"\{r: ([\d.eE+-]+), g: ([\d.eE+-]+), b: ([\d.eE+-]+), a: ([\d.eE+-]+)\}", s or "")
    if not m: return None
    r, g, b, a = (min(1.0, max(0.0, float(x))) for x in m.groups())
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
            if fm: fields[fm.group(1)] = fm.group(2)
            cm = re.match(r"^  - \{fileID: (\d+)\}\s*$", line)
            if cm: children.append(cm.group(1))
        docs[fid] = {"cls": CLASS_NAMES.get(int(cls), cls), "fields": fields,
                     "children": children,
                     "comps": re.findall(r"component: \{fileID: (\d+)\}", body), "body": body}
    return docs

def build(path):
    docs = parse(path)
    rt_go = {}
    for fid, d in docs.items():
        if d["cls"] == "RectTransform":
            g = re.search(r"\{fileID: (\d+)\}", d["fields"].get("m_GameObject", ""))
            if g: rt_go[fid] = g.group(1)
    def comp_infos(go):
        out = []
        for c in docs[go]["comps"]:
            d = docs.get(c)
            if not d: continue
            if d["cls"] == "MonoBehaviour":
                g = re.search(r"guid: ([0-9a-f]{32})", d["fields"].get("m_Script", ""))
                name = guid_map.get(g.group(1), "") if g else ""
                out.append((("Script", name + ".cs", d)) if name else (mono_type(d["body"]), "", d))
            else:
                out.append((d["cls"], "", d))
        return out
    root_rt = next(fid for fid, d in docs.items()
                   if d["cls"] == "RectTransform" and d["fields"].get("m_Father", "").startswith("{fileID: 0}"))
    nodes = []
    def walk(rt, depth):
        go = rt_go.get(rt); d = docs[rt]
        if go not in docs: return
        infos = comp_infos(go)
        types = {t for t, _, _ in infos}
        text_comp = next((c for t, _, c in infos if t == "Text"), None)
        img_comp = next((c for t, _, c in infos if t == "Image"), None)
        txt, tcolor, fsize, align = None, None, None, None
        if text_comp:
            tm = re.search(r'm_Text: "(.*)"', text_comp["body"])
            txt = tm.group(1) if tm else ""
            if "\\u" in txt:
                try: txt = txt.encode("latin-1", errors="ignore").decode("unicode_escape", errors="ignore")
                except Exception: pass
            tcolor = parse_color(text_comp["fields"].get("m_Color"))
            sm = re.search(r"m_FontSize: (\d+)", text_comp["body"])
            am = re.search(r"m_Alignment: (\d+)", text_comp["body"])
            fsize = int(sm.group(1)) if sm else None
            align = ALIGN.get(int(am.group(1))) if am else None
        comps = [(n if t == "Script" else t) for t, n, _ in infos]
        nodes.append({
            "name": docs[go]["fields"].get("m_Name", "?"), "depth": depth,
            "active": docs[go]["fields"].get("m_IsActive", "1") != "0",
            "amin": vec(d["fields"].get("m_AnchorMin")), "amax": vec(d["fields"].get("m_AnchorMax")),
            "pos": vec(d["fields"].get("m_AnchoredPosition")), "size": vec(d["fields"].get("m_SizeDelta")),
            "pivot": vec(d["fields"].get("m_Pivot")), "comps": comps,
            "isText": text_comp is not None, "isBtn": "Button" in types, "isImg": img_comp is not None,
            "hasVisual": bool(types & {"Text", "Image", "Button"}),
            "txt": txt, "tcolor": tcolor or "#ECEFF1", "fsize": fsize, "align": align,
            "bgcolor": parse_color(img_comp["fields"].get("m_Color")) if img_comp else None,
        })
        for ch in d["children"]:
            if ch in docs: walk(ch, depth + 1)
    walk(root_rt, 0)
    kids, stack = {}, []
    for i, n in enumerate(nodes):
        while stack and nodes[stack[-1]]["depth"] >= n["depth"]: stack.pop()
        if stack: kids.setdefault(stack[-1], []).append(i)
        stack.append(i)
    return nodes, kids

def tree_text(nodes, kids):
    lines = []
    def fmt(n):
        s = n["name"]
        cs = [c for c in n["comps"] if c != "RectTransform"]
        if cs: s += "  [" + ", ".join(cs) + "]"
        if not n["active"]: s += "  ✕隐藏"
        if n["depth"] > 0:
            s += "  ·锚(%.1f,%.1f)~(%.1f,%.1f) 偏(%.0f,%.0f) 尺寸%.0f×%.0f" % (
                n["amin"][0], n["amin"][1], n["amax"][0], n["amax"][1],
                n["pos"][0], n["pos"][1], n["size"][0], n["size"][1])
        else:
            s += "  ·尺寸%.0f×%.0f" % (n["size"][0], n["size"][1])
        return s
    def emit(i, prefix, last):
        n = nodes[i]
        branch = "└─ " if last else "├─ "
        lines.append((prefix + branch if i else "") + fmt(n))
        cs = kids.get(i, [])
        for k, ci in enumerate(cs):
            ext = "   " if last else "│  "
            emit(ci, (prefix + ext) if i else "", k == len(cs) - 1)
    emit(0, "", True)
    return "\n".join(lines)

def compute_rects(nodes, kids):
    out = []
    def calc(i, px, py, pw, ph):
        n = nodes[i]
        # X 轴：Unity 与 HTML 同向（左→右）
        x0, x1 = px + n["amin"][0] * pw, px + n["amax"][0] * pw
        if abs(x1 - x0) < 1e-6: w = n["size"][0]; cx = x0 + n["pos"][0]
        else: w = (x1 - x0) + n["size"][0]; cx = (x0 + x1) / 2 + n["pos"][0]
        L = cx - n["pivot"][0] * w
        # Y 轴：Unity y 向上(0=底) → HTML y 向下(0=顶)，锚点需反转
        ay0, ay1 = n["amin"][1], n["amax"][1]
        if abs(ay1 - ay0) < 1e-6:
            h = n["size"][1]
            cy = py + (1 - ay0) * ph - n["pos"][1]  # pos.y 正=向上=HTML y 减小
        else:
            h = (ay1 - ay0) * ph + n["size"][1]
            cy = py + (1 - (ay0 + ay1) / 2) * ph - n["pos"][1]
        T = cy - (1 - n["pivot"][1]) * h
        return L, T, w, h
    def walk(i, px, py, pw, ph):
        n = nodes[i]
        L, T, w, h = calc(i, px, py, pw, ph)
        if i > 0 and n["active"] and n["hasVisual"] and w > 3 and h > 3:
            out.append((n, L, T, w, h))
        for c in kids.get(i, []): walk(c, L, T, w, h)
    walk(0, 0, 0, CANVAS_W, CANVAS_H)
    return out

def to_json(i, nodes, kids):
    n = nodes[i]
    d = {"name": n["name"], "active": n["active"], "components": n["comps"],
         "anchors": {"min": list(n["amin"]), "max": list(n["amax"])},
         "pivot": list(n["pivot"]), "position": list(n["pos"]), "size": list(n["size"])}
    if n["isText"]:
        d["text"] = n["txt"] or ""
        if n["fsize"]: d["fontSize"] = n["fsize"]
        if n["align"]: d["align"] = n["align"]
        d["color"] = n["tcolor"]
    if n["isImg"] or n["isBtn"]:
        d["background"] = n["bgcolor"] or "#FFFFFF"
    ch = [to_json(c, nodes, kids) for c in kids.get(i, [])]
    if ch: d["children"] = ch
    return d

PAGE_TMPL = """<!DOCTYPE html><html><head><meta charset="utf-8"><title>@@NAME@@ · 页面设计稿</title><style>
body{background:#1a2226;font-family:"Microsoft YaHei",Consolas,monospace;color:#B0BEC5;margin:20px}
h1{color:#ECEFF1;font-size:20px}h1 small{color:#78909C;font-size:12px;font-weight:normal;margin-left:12px}
a{color:#4FC3F7}
.wrap{display:flex;gap:24px;align-items:flex-start}
.page{position:relative;width:640px;height:360px;background:#263238;border:1px solid #546E7A;overflow:hidden;flex:none}
.box{position:absolute;border:1px solid rgba(120,144,156,.45);box-sizing:border-box}
.btn{position:absolute;border:1.5px solid rgba(120,144,156,.8);box-sizing:border-box;display:flex;align-items:center;justify-content:center;color:#ECEFF1;font-size:12px;overflow:hidden;white-space:nowrap}
.t{position:absolute;display:flex;align-items:center;justify-content:center;overflow:hidden;white-space:nowrap;text-align:center;font-weight:600}
.tree{background:#202a30;border:1px solid #546E7A;padding:12px;font-size:12px;line-height:1.5;max-height:720px;overflow:auto;flex:1}
.legend{margin-top:8px;font-size:12px;color:#78909C}
</style></head><body>
<h1>@@NAME@@ <small>@@SUB@@ · <a href="index.html">← 索引</a></small></h1>
<div class="wrap"><div><div class="page">@@PREVIEW@@</div>
<div class="legend">按预制体锚点精确换算（比例 0.5）· 文字/颜色/内容取自设计稿 · ✕隐藏节点不渲染</div></div>
<pre class="tree">@@TREE@@</pre></div>
<script type="application/json" id="ui-spec">@@JSON@@</script>
</body></html>"""

INDEX_TMPL = """<!DOCTYPE html><html><head><meta charset="utf-8"><title>UIPrefab · 设计稿索引</title><style>
body{background:#1a2226;font-family:"Microsoft YaHei",Consolas,monospace;color:#B0BEC5;margin:24px}
h1{color:#ECEFF1}table{border-collapse:collapse;margin-top:12px}
td,th{border:1px solid #546E7A;padding:6px 14px;text-align:left}a{color:#4FC3F7;text-decoration:none}
.flow{background:#202a30;border:1px solid #4FC3F7;padding:10px 14px;margin:14px 0;max-width:860px;font-size:13px}
</style></head><body><h1>UI 页面设计稿索引</h1>
<div class="flow">📐 工作流：<b>先 HTML 后代码</b> — ① 在本目录新建/迭代 &lt;页面&gt;.html（视觉稿 + 层级树 + 内嵌
&lt;script id="ui-spec"&gt; 的 JSON 规格） → ② 设计定稿 → ③ 由生成工具解析 ui-spec JSON 产出
预制体生成器与 View/Ctrl 代码 → ④ Unity 菜单生成预制体入库。<br>
每个页面 HTML 内嵌完整节点规格（名称/组件/锚点/尺寸/颜色/文案），即后续代码生成的唯一数据源。</div>
<table><tr><th>页面</th><th>预制体来源</th><th>节点数</th></tr>@@ROWS@@</table></body></html>"""

def render_elements(nodes, kids):
    els = []
    for n, L, T, w, h in compute_rects(nodes, kids):
        x, y, W, Hh = L * SCALE, T * SCALE, w * SCALE, h * SCALE
        if n["isText"]:
            fs = max(8, min(16, Hh * 0.72))
            els.append('<div class="t" style="left:%.0fpx;top:%.0fpx;width:%.0fpx;height:%.0fpx;'
                       'font-size:%.0fpx;color:%s">%s</div>'
                       % (x, y, W, Hh, fs, n["tcolor"], H.escape(n["txt"] or n["name"])))
        elif n["isBtn"]:
            els.append('<div class="btn" style="left:%.0fpx;top:%.0fpx;width:%.0fpx;height:%.0fpx;'
                       'background:%s"><span>%s</span></div>'
                       % (x, y, W, Hh, n["bgcolor"] or "rgba(255,255,255,0.08)",
                          H.escape(n["txt"] or n["name"])))
        else:
            els.append('<div class="box" style="left:%.0fpx;top:%.0fpx;width:%.0fpx;height:%.0fpx;'
                       'background:%s"></div>'
                       % (x, y, W, Hh, n["bgcolor"] or "rgba(255,255,255,0.06)"))
    return els

def flatten_spec(spec):
    """方向 B：把 ui-spec JSON 展平为 nodes/kids（与预制体解析同构）"""
    nodes, kids = [], {}
    def walk(d, depth, parent):
        idx = len(nodes)
        comps = d.get("components", ["RectTransform"])
        nodes.append({
            "name": d.get("name", "?"), "depth": depth,
            "active": d.get("active", True),
            "amin": tuple(d["anchors"]["min"]), "amax": tuple(d["anchors"]["max"]),
            "pos": tuple(d.get("position", [0, 0])), "size": tuple(d.get("size", [0, 0])),
            "pivot": tuple(d.get("pivot", [0.5, 0.5])), "comps": comps,
            "isText": "text" in d, "isBtn": "Button" in comps, "isImg": "Image" in comps,
            "hasVisual": "text" in d or "Button" in comps or "Image" in comps,
            "txt": d.get("text"), "tcolor": d.get("color", "#ECEFF1"),
            "fsize": d.get("fontSize"), "align": d.get("align"),
            "bgcolor": d.get("background"),
        })
        if parent is not None: kids.setdefault(parent, []).append(idx)
        for c in d.get("children", []): walk(c, depth + 1, idx)
    walk(spec, 0, None)
    return nodes, kids

def build_page_from_spec(name, json_path, rel):
    """方向 B：从设计 JSON 生成页面 HTML（预览 + 树 + 内嵌规格）"""
    with open(json_path, encoding="utf-8") as f:
        spec = json.load(f)
    nodes, kids = flatten_spec(spec)
    scripts = ", ".join(c for c in nodes[0]["comps"] if c.endswith(".cs"))
    sub = ((rel + " · ") if rel else "") + str(len(nodes)) + " 节点 · " + scripts
    doc = (PAGE_TMPL.replace("@@NAME@@", name)
           .replace("@@SUB@@", H.escape(sub))
           .replace("@@PREVIEW@@", "".join(render_elements(nodes, kids)))
           .replace("@@TREE@@", H.escape(tree_text(nodes, kids)))
           .replace("@@JSON@@", json.dumps(spec, ensure_ascii=False, indent=1).replace("</", "<\\/")))
    with open(os.path.join(OUT_DIR, name + ".html"), "w", encoding="utf-8", newline="\n") as f:
        f.write(doc)
    print("built:", name, "| nodes:", len(nodes))

if __name__ == "__main__":
    import sys
    if len(sys.argv) >= 4 and sys.argv[1] == "--build-page":
        build_page_from_spec(sys.argv[2], sys.argv[3],
                             sys.argv[4] if len(sys.argv) > 4 else "")
        sys.exit(0)

pages = []
for folder in sorted(os.listdir(UI_DIR)):
    pdir = os.path.join(UI_DIR, folder)
    if not os.path.isdir(pdir): continue
    pf = glob.glob(os.path.join(pdir, "*.prefab"))
    if not pf: continue
    nodes, kids = build(pf[0])
    rel = os.path.relpath(pf[0], r"D:\PersonProjects\DualEnigma").replace("\\", "/")
    els = render_elements(nodes, kids)

    spec = json.dumps(to_json(0, nodes, kids), ensure_ascii=False, indent=1).replace("</", "<\\/")
    scripts = ", ".join(c for c in nodes[0]["comps"] if c.endswith(".cs"))
    doc = (PAGE_TMPL.replace("@@NAME@@", folder)
           .replace("@@SUB@@", H.escape(rel + " · " + str(len(nodes)) + " 节点 · " + scripts))
           .replace("@@PREVIEW@@", "".join(els))
           .replace("@@TREE@@", H.escape(tree_text(nodes, kids)))
           .replace("@@JSON@@", spec))
    with open(os.path.join(OUT_DIR, folder + ".html"), "w", encoding="utf-8", newline="\n") as f:
        f.write(doc)
    pages.append((folder, rel, len(nodes)))

rows = "\n".join('<tr><td><a href="%s.html">%s</a></td><td>%s</td><td>%d</td></tr>'
                 % (n, n, r, c) for n, r, c in pages)
with open(os.path.join(OUT_DIR, "index.html"), "w", encoding="utf-8", newline="\n") as f:
    f.write(INDEX_TMPL.replace("@@ROWS@@", rows))

removed = []
for old in ["README.md", "_预览.html"] + [n + ".md" for n, _, _ in pages]:
    p = os.path.join(OUT_DIR, old)
    if os.path.exists(p):
        os.remove(p); removed.append(old)
print("pages:", len(pages), "| removed legacy:", len(removed))
print("done ->", OUT_DIR)
