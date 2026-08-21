/* UIPrefab 设计稿查看器
 * 读取页面内嵌 <script id="ui-spec"> 的 JSON 规格，在浏览器端按 Unity 锚点规则渲染预览与层级树。
 * 数据源即设计稿：直接编辑 ui-spec JSON 后刷新页面即可看到效果。
 * 布局数学来自 spec-core.js（与 editor.js 共享，保证预览与编辑所见一致）。
 * 需要在 viewer.js 之前引入 spec-core.js。
 */
(function () {
  "use strict";

  var Core = window.SpecCore;
  if (!Core) return;
  var CANVAS_W = Core.CANVAS_W, CANVAS_H = Core.CANVAS_H;

  var specEl = document.getElementById("ui-spec");
  if (!specEl) return;
  var spec = JSON.parse(specEl.textContent);

  var pageEl = document.getElementById("preview");
  var treeEl = document.getElementById("tree");
  var zoomSel = document.getElementById("zoom");
  var showHiddenChk = document.getElementById("showHidden");

  var calcRect = Core.calcRect;
  var layoutChildren = Core.layoutChildren;
  var parseAlign = Core.parseAlign;
  var findParent = Core.findParent;

  // ---------- 预览渲染 ----------

  // 叠加 v1.2 变换（scale/rotation）到 CSS；transform-origin 对应 pivot
  function applyTransform(el, node) {
    var s = Core.scaleOf(node);
    var deg = Core.rotationOf(node);
    if (s[0] === 1 && s[1] === 1 && deg === 0) return;
    var pivot = node.pivot || [0.5, 0.5];
    el.style.transformOrigin = (pivot[0] * 100) + "% " + ((1 - pivot[1]) * 100) + "%";
    // Unity 旋转逆时针为正；CSS 顺时针为正 → 取负保持视觉一致
    el.style.transform = "scale(" + s[0] + "," + s[1] + ") rotate(" + (-deg) + "deg)";
  }

  function hasPlaceholderSibling(node, siblings) {
    return siblings.some(function (s) { return s !== node && s.name === "Placeholder"; });
  }

  function renderNode(node, px, py, pw, ph, parentVisible, out, showHidden) {
    var visible = parentVisible && node.active !== false;
    var rect = calcRect(node, px, py, pw, ph);
    var comps = node.components || [];
    var isText = "text" in node;
    var isBtn = comps.indexOf("Button") >= 0;
    var isImg = comps.indexOf("Image") >= 0;
    var hasVisual = isText || isBtn || isImg;

    if (node !== spec && hasVisual && rect.w > 1 && rect.h > 1) {
      out.push({ node: node, rect: rect, visible: visible, isText: isText, isBtn: isBtn, isImg: isImg });
    }

    var kids = node.children || [];
    var layoutRects = node.layout ? layoutChildren(node, rect.w, rect.h, showHidden) : null;
    kids.forEach(function (c, i) {
      var cr;
      if (layoutRects && layoutRects[i]) {
        var lr = layoutRects[i];
        cr = { L: rect.L + lr.L, T: rect.T + lr.T, w: lr.w, h: lr.h };
        renderNodeAt(c, cr, visible, out, showHidden);
      } else {
        renderNode(c, rect.L, rect.T, rect.w, rect.h, visible, out, showHidden);
      }
    });
  }

  // 用给定矩形渲染子树（LayoutGroup 子节点专用）
  function renderNodeAt(node, rect, parentVisible, out, showHidden) {
    var visible = parentVisible && node.active !== false;
    var comps = node.components || [];
    var isText = "text" in node;
    var isBtn = comps.indexOf("Button") >= 0;
    var isImg = comps.indexOf("Image") >= 0;
    if (node !== spec && (isText || isBtn || isImg) && rect.w > 1 && rect.h > 1) {
      out.push({ node: node, rect: rect, visible: visible, isText: isText, isBtn: isBtn, isImg: isImg });
    }
    var kids = node.children || [];
    var layoutRects = node.layout ? layoutChildren(node, rect.w, rect.h, showHidden) : null;
    kids.forEach(function (c, i) {
      if (layoutRects && layoutRects[i]) {
        var lr = layoutRects[i];
        renderNodeAt(c, { L: rect.L + lr.L, T: rect.T + lr.T, w: lr.w, h: lr.h }, visible, out, showHidden);
      } else {
        renderNode(c, rect.L, rect.T, rect.w, rect.h, visible, out, showHidden);
      }
    });
  }

  function render() {
    var scale = parseFloat(zoomSel ? zoomSel.value : "0.75");
    var showHidden = !!(showHiddenChk && showHiddenChk.checked);
    pageEl.style.width = CANVAS_W * scale + "px";
    pageEl.style.height = CANVAS_H * scale + "px";
    pageEl.innerHTML = "";

    var items = [];
    renderNode(spec, 0, 0, CANVAS_W, CANVAS_H, true, items, showHidden);

    // 有可见文本子节点的按钮不再重复显示节点名
    items.forEach(function (it) {
      var n = it.node, r = it.rect;
      if (!it.visible && !showHidden) return;
      var el = document.createElement("div");
      el.style.left = r.L * scale + "px";
      el.style.top = r.T * scale + "px";
      el.style.width = r.w * scale + "px";
      el.style.height = r.h * scale + "px";
      el.className = "el" + (it.visible ? "" : " ghost");

      if (it.isText) {
        var txt = n.text || "";
        var siblings = (findParent(spec, n) || { children: [] }).children || [];
        if (!txt.trim() && hasPlaceholderSibling(n, siblings)) return; // 输入框空文本：只显示 Placeholder
        var dynamic = !txt.trim();
        el.classList.add("txt");
        if (dynamic) {
          el.classList.add("dyn");
          txt = n.name; // 动态文本：显示节点名占位
        }
        var fs = n.fontSize ? n.fontSize * scale : r.h * scale * 0.72;
        fs = Math.max(8, Math.min(fs, r.h * scale));
        el.style.fontSize = fs + "px";
        el.style.color = n.color || "#ECEFF1";
        var al = parseAlign(n.align);
        el.style.justifyContent = ["flex-start", "center", "flex-end"][Math.round(al.h * 2)];
        el.style.alignItems = ["flex-end", "center", "flex-start"][Math.round(al.v * 2)];
        el.style.textAlign = ["left", "center", "right"][Math.round(al.h * 2)];
        el.textContent = txt;
      } else if (it.isBtn) {
        el.classList.add("btn");
        el.style.background = n.background || "rgba(255,255,255,0.08)";
        var hasTextChild = (n.children || []).some(function (c) { return "text" in c; });
        if (!hasTextChild) {
          el.style.display = "flex";
          el.style.alignItems = "center";
          el.style.justifyContent = "center";
          el.style.color = "#ECEFF1";
          el.style.fontSize = Math.max(8, 12 * scale) + "px";
          el.textContent = n.name;
        }
      } else {
        el.classList.add("box");
        el.style.background = n.background || "rgba(255,255,255,0.06)";
      }
      applyTransform(el, n);
      pageEl.appendChild(el);
    });

    renderTree();
  }

  // ---------- 层级树渲染 ----------

  function renderTree() {
    var lines = [];
    function fmt(n, isRoot) {
      var s = n.name;
      var cs = (n.components || []).filter(function (c) { return c !== "RectTransform"; });
      if (cs.length) s += '  <span class="comps">[' + escapeHtml(cs.join(", ")) + "]</span>";
      if (n.layout) s += '  <span class="comps">{' + n.layout.type + "}</span>";
      var g;
      if (isRoot) {
        g = "  ·尺寸" + n.size[0] + "×" + n.size[1];
      } else {
        g = "  ·锚(" + n.anchors.min.map(f1).join(",") + ")~(" + n.anchors.max.map(f1).join(",") + ")" +
            " 偏(" + f0(n.position[0]) + "," + f0(n.position[1]) + ")" +
            " 尺寸" + f0(n.size[0]) + "×" + f0(n.size[1]);
      }
      s += '<span class="geo">' + g + "</span>";
      return s;
    }
    function f0(x) { return Math.round(x); }
    function f1(x) { return x.toFixed(1); }
    function emit(n, prefix, last, isRoot) {
      var cls = n.active === false ? ' class="hidden-node"' : "";
      var branch = isRoot ? "" : (last ? "└─ " : "├─ ");
      lines.push("<span" + cls + ">" + escapeHtml(prefix + branch) + fmt(n, isRoot) + "</span>");
      var kids = n.children || [];
      kids.forEach(function (c, i) {
        var ext = isRoot ? "" : (last ? "   " : "│  ");
        emit(c, isRoot ? "" : prefix + ext, i === kids.length - 1, false);
      });
    }
    emit(spec, "", true, true);
    treeEl.innerHTML = lines.join("\n");
  }

  function escapeHtml(s) {
    return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  }

  if (zoomSel) zoomSel.addEventListener("change", render);
  if (showHiddenChk) showHiddenChk.addEventListener("change", render);
  render();
})();
