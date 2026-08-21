# -*- coding: utf-8 -*-
"""
ui-spec 可视化编辑器 · 本地服务
零第三方依赖（仅用标准库 http.server + json + re）。

目录布局（相对本脚本上两级的 UIPrefab/）:
    pages/   页面设计稿（含内嵌 ui-spec JSON，被动读写）
    assets/  共享前端资源（viewer.js/viewer.css/spec-core.js）
    editor/  编辑器前端（editor.html/editor.js/editor.css）

用法:
    python tools/ui_editor.py [--port 8765]
    浏览器打开 http://localhost:8765/ 即编辑器

端点:
    GET  /                → editor/editor.html
    GET  /api/pages       → 扫描 pages/ 下含 id="ui-spec" 的 *.html，返回页面名列表
    GET  /api/spec?page=X → 返回该页面提取出的 ui-spec JSON
    POST /api/save?page=X → body = 新的 ui-spec JSON，写回 pages/<页面>.html
    GET  /editor/* /assets/* → 编辑器与共享资源（白名单静态文件）

写回机制（安全与格式）:
    1. 校验: body 先 json.loads 验证合法性，失败返回 400（不落盘）
    2. 排版: dumps_spec(spec) —— 与现有内嵌 JSON 风格一致
             （1 空格缩进 + 小结构单行），git diff 干净
    3. 替换: 正则定位 <script ... id="ui-spec">...</script> 块，只替换其内容
    4. 落盘: 先写 <页面>.html.tmp 成功后 os.replace 原子替换；
             替换前把原文件复制为 <页面>.html.bak（单代备份，git 为最终保障）
    5. 防护: 页面名白名单校验（必须来自 /api/pages 结果），拒绝路径穿越
"""
import argparse
import json
import os
import re
import shutil
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

# 目录默认取脚本上两级（tools/ → UIPrefab/）
BASE_DIR = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
PAGES_DIR = os.path.join(BASE_DIR, "pages")
EDITOR_DIR = os.path.join(BASE_DIR, "editor")
ASSETS_DIR = os.path.join(BASE_DIR, "assets")
DEFAULT_PORT = 8765

SPEC_TAG = re.compile(
    r'(<script[^>]*id="ui-spec"[^>]*>)(.*?)(</script>)',
    re.S,
)


# ---------- 与现有内嵌 JSON 风格一致的排版器 ----------
# 规则（对照现有 10 份设计稿）：1 空格缩进；
# 小对象（≤4 键且值为标量/标量数组，如 anchors/layout）与标量数组
# （components/pivot/position/size/padding）保持单行；children 数组逐节点展开。

def _is_scalar(x):
    return not isinstance(x, (dict, list))


def _inlineable(v):
    """是否可单行渲染：标量数组，或 ≤4 键且值为标量/标量数组的对象"""
    if isinstance(v, list):
        return all(_is_scalar(x) for x in v)
    if isinstance(v, dict):
        if len(v) > 4:
            return False
        return all(
            _is_scalar(x) or (isinstance(x, list) and all(_is_scalar(i) for i in x))
            for x in v.values()
        )
    return True


def _inline(v):
    if isinstance(v, dict):
        return "{ " + ", ".join(
            json.dumps(k, ensure_ascii=False) + ": " + _inline(x) for k, x in v.items()
        ) + " }"
    return json.dumps(v, ensure_ascii=False)


def dumps_spec(v, indent=0):
    """序列化为与现有设计稿一致的排版（1 空格缩进 + 小结构单行）"""
    pad = " " * indent
    if isinstance(v, dict):
        if not v:
            return "{}"
        if _inlineable(v):
            return _inline(v)
        items = [
            " " * (indent + 1) + json.dumps(k, ensure_ascii=False) + ": " + dumps_spec(x, indent + 1)
            for k, x in v.items()
        ]
        return "{\n" + ",\n".join(items) + "\n" + pad + "}"
    if isinstance(v, list):
        if not v:
            return "[]"
        if _inlineable(v):
            return _inline(v)
        items = [" " * (indent + 1) + dumps_spec(x, indent + 1) for x in v]
        return "[\n" + ",\n".join(items) + "\n" + pad + "]"
    return json.dumps(v, ensure_ascii=False)

# 除 API 外允许直接访问的静态文件（编辑器/查看器资源）
def list_pages():
    """扫描 pages/ 下含 ui-spec 的页面。"""
    pages = []
    if not os.path.isdir(PAGES_DIR):
        return pages
    for name in sorted(os.listdir(PAGES_DIR)):
        if not name.endswith(".html"):
            continue
        path = os.path.join(PAGES_DIR, name)
        try:
            with open(path, encoding="utf-8") as f:
                head = f.read()
        except OSError:
            continue
        if 'id="ui-spec"' in head:
            pages.append(name[:-5])
    return pages


def extract_spec(page):
    """从页面 HTML 提取 ui-spec JSON 文本。"""
    path = os.path.join(PAGES_DIR, page + ".html")
    with open(path, encoding="utf-8") as f:
        html = f.read()
    m = SPEC_TAG.search(html)
    if not m:
        raise ValueError(f"{page}.html: 未找到 ui-spec 标签")
    json_text = m.group(2).strip().replace("<\\/", "</")
    return json_text


def save_spec(page, body_bytes):
    """把新的 ui-spec JSON 写回页面 HTML（校验 → 排版 → 替换 → 原子落盘 + .bak）。"""
    # 1. 校验 JSON 合法性（失败不落盘）
    try:
        spec = json.loads(body_bytes.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as e:
        raise ValueError(f"JSON 不合法: {e}")

    path = os.path.join(PAGES_DIR, page + ".html")
    if not os.path.exists(path):
        raise ValueError(f"{page}.html: 文件不存在")

    with open(path, encoding="utf-8") as f:
        html = f.read()
    m = SPEC_TAG.search(html)
    if not m:
        raise ValueError(f"{page}.html: 未找到 ui-spec 标签")

    # 2. 排版：与现有内嵌 JSON 风格一致（1 空格缩进 + 小结构单行）；
    #    "</" 转义防止 script 提前闭合
    json_text = dumps_spec(spec).replace("</", "<\\/")

    # 3. 只替换 script 块内容，页面其余部分不动
    new_html = html[: m.start(2)] + "\n" + json_text + "\n" + html[m.end(2):]

    # 4. 备份 + 原子落盘
    bak = path + ".bak"
    tmp = path + ".tmp"
    shutil.copy2(path, bak)
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        f.write(new_html)
    os.replace(tmp, path)


class EditorHandler(BaseHTTPRequestHandler):
    server_version = "UISpecEditor/1.0"
    protocol_version = "HTTP/1.1"

    # 安静模式：不打访问日志到 stderr（保留错误日志）
    def log_message(self, fmt, *args):
        pass

    # ---------- 工具 ----------

    def _send(self, code, body, content_type="text/plain; charset=utf-8"):
        data = body.encode("utf-8") if isinstance(body, str) else body
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(data)

    def _send_json(self, code, obj):
        self._send(code, json.dumps(obj, ensure_ascii=False),
                   "application/json; charset=utf-8")

    def _page_param(self):
        qs = parse_qs(urlparse(self.path).query)
        page = (qs.get("page") or [""])[0]
        # 白名单校验：必须来自 /api/pages 结果，拒绝路径穿越
        if page and page in list_pages():
            return page
        return None

    # ---------- GET ----------

    def do_GET(self):
        path = urlparse(self.path).path

        if path == "/api/pages":
            self._send_json(200, list_pages())
            return

        if path == "/api/spec":
            page = self._page_param()
            if not page:
                self._send(400, "未知页面（须为 /api/pages 返回值）")
                return
            try:
                self._send(200, extract_spec(page), "application/json; charset=utf-8")
            except (OSError, ValueError) as e:
                self._send(500, str(e))
            return

        # "/" 重定向到编辑器真实路径（相对引用 editor.css/editor.js 需按 /editor/ 解析）
        if path == "/":
            self.send_response(302)
            self.send_header("Location", "/editor/editor.html")
            self.send_header("Content-Length", "0")
            self.end_headers()
            return
        name = path.lstrip("/")
        if not name.startswith(("editor/", "assets/")):
            self._send(403, "禁止访问")
            return
        # 规范化后必须仍落在对应目录内（拒绝 .. 路径穿越）
        base = BASE_DIR
        fpath = os.path.normpath(os.path.join(base, name))
        if not fpath.startswith(os.path.join(base, "editor") + os.sep) and \
           not fpath.startswith(os.path.join(base, "assets") + os.sep):
            self._send(403, "禁止访问")
            return
        if not os.path.isfile(fpath):
            self._send(404, "文件不存在")
            return
        ctype = ("text/html" if name.endswith(".html")
                 else "text/javascript" if name.endswith(".js")
                 else "text/css" if name.endswith(".css") else "text/plain")
        try:
            with open(fpath, "rb") as f:
                self._send(200, f.read(), ctype + "; charset=utf-8")
        except OSError as e:
            self._send(500, str(e))

    # ---------- POST ----------

    def do_POST(self):
        path = urlparse(self.path).path
        if path != "/api/save":
            self._send(404, "未知端点")
            return

        page = self._page_param()
        if not page:
            self._send(400, "未知页面（须为 /api/pages 返回值）")
            return

        try:
            length = int(self.headers.get("Content-Length") or 0)
        except ValueError:
            length = 0
        if length <= 0:
            self._send(400, "请求体为空")
            return
        body = self.rfile.read(length)

        try:
            save_spec(page, body)
        except ValueError as e:
            self._send(400, str(e))
            return
        except OSError as e:
            self._send(500, f"写盘失败: {e}")
            return

        self._send_json(200, {"ok": True, "page": page})
        print(f"[ui_editor] saved: {page}.html")


def main():
    parser = argparse.ArgumentParser(description="ui-spec 可视化编辑器本地服务")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    args = parser.parse_args()

    # 仅本机使用：绑定 127.0.0.1，不做鉴权
    server = ThreadingHTTPServer(("127.0.0.1", args.port), EditorHandler)
    print(f"[ui_editor] 目录: {BASE_DIR}")
    print(f"[ui_editor] 服务已启动: http://localhost:{args.port}/ （Ctrl+C 停止）")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[ui_editor] 已停止")


if __name__ == "__main__":
    main()
