/* UIPrefab 设计稿查看器
 * 读取页面内嵌 <script id="ui-spec"> 的 JSON 规格，在浏览器端按 Unity 锚点规则渲染预览与层级树。
 * 数据源即设计稿：直接编辑 ui-spec JSON 后刷新页面即可看到效果。
 */
(function () {
  "use strict";

  var CANVAS_W = 1280, CANVAS_H = 720; // 参考分辨率
  var specEl = document.getElementById("ui-spec");
  if (!specEl) return;
  var spec = JSON.parse(specEl.textContent);

  var pageEl = document.getElementById("preview");
  var treeEl = document.getElementById("tree");
  var zoomSel = document.getElementById("zoom");
  var showHiddenChk = document.getElementById("showHidden");

  // ---------- 布局计算（Unity RectTransform → 屏幕像素，y 轴翻转） ----------

  // 水平对齐系数 / 垂直对齐系数（Unity TextAnchor / LayoutGroup ChildAlignment）
  var HALIGN = { Left: 0, Center: 0.5, Right: 1 };
  var VALIGN = { Upper: 1, Middle: 0.5, Lower: 0 };

  function parseAlign(align) {
    // "MiddleCenter" → { h: 0.5, v: 0.5 }
    var v = 0.5, h = 0.5;
    if (align) {
      for (var vk in VALIGN) if (align.indexOf(vk) === 0) v = VALIGN[vk];
      for (var hk in HALIGN) if (align.indexOf(hk) > 0 || align === hk) h = HALIGN[hk];
    }
    return { h: h, v: v };
  }

  // 计算节点在父矩形内的矩形（父矩形为 HTML 坐标系：y 向下）
  function calcRect(node, px, py, pw, ph) {
    var amin = node.anchors.min, amax = node.anchors.max;
    var pos = node.position || [0, 0];
    var size = node.size || [0, 0];
    var pivot = node.pivot || [0.5, 0.5];
    var x0 = px + amin[0] * pw, x1 = px + amax[0] * pw;
    var w, cx;
    if (Math.abs(x1 - x0) < 1e-6) { w = size[0]; cx = x0 + pos[0]; }
    else { w = (x1 - x0) + size[0]; cx = (x0 + x1) / 2 + pos[0]; }
    var L = cx - pivot[0] * w;
    var ay0 = amin[1], ay1 = amax[1];
    var h, cy;
    if (Math.abs(ay1 - ay0) < 1e-6) { h = size[1]; cy = py + (1 - ay0) * ph - pos[1]; }
    else { h = (ay1 - ay0) * ph + size[1]; cy = py + (1 - (ay0 + ay1) / 2) * ph - pos[1]; }
    var T = cy - (1 - pivot[1]) * h;
    return { L: L, T: T, w: w, h: h };
  }

  // LayoutGroup 子节点排布：返回 { childIndex: {L,T,w,h} }（父矩形坐标系）
  function layoutChildren(node, pw, ph, includeHidden) {
    var layout = node.layout;
    var kids = (node.children || []).filter(function (c) { return includeHidden || c.active !== false; });
    var pad = layout.padding || [0, 0, 0, 0]; // [左, 上, 右, 下]
    var spacing = layout.spacing || 0;
    var al = parseAlign(layout.align);
    var horizontal = layout.type === "horizontal";
    var rects = {};
    var total = 0;
    kids.forEach(function (c, i) {
      var s = c.size || [0, 0];
      total += horizontal ? s[0] : s[1];
      if (i > 0) total += spacing;
    });
    // 容器尺寸为 0 时按内容撑开（如 UIInvitePopup 的 CardContainer height=0）
    var effW = pw, effH = ph;
    if (horizontal && effW <= 0) effW = total + pad[0] + pad[2];
    if (!horizontal && effH <= 0) effH = total + pad[1] + pad[3];
    var innerW = effW - pad[0] - pad[2], innerH = effH - pad[1] - pad[3];
    var cursor;
    if (horizontal) cursor = pad[0] + (innerW - total) * al.h;
    else cursor = pad[1] + (innerH - total) * (1 - al.v); // HTML y 向下，Upper 对齐 = 靠上
    kids.forEach(function (c) {
      var idx = node.children.indexOf(c);
      var s = c.size || [0, 0];
      var w = s[0], h = s[1];
      var L, T;
      if (horizontal) {
        L = cursor;
        T = pad[1] + (innerH - h) * (1 - al.v);
        cursor += w + spacing;
      } else {
        L = pad[0] + (innerW - w) * al.h;
        T = cursor;
        cursor += h + spacing;
      }
      rects[idx] = { L: L, T: T, w: w, h: h };
    });
    return rects;
  }

  // ---------- 预览渲染 ----------

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
      pageEl.appendChild(el);
    });

    renderTree();
  }

  function findParent(root, target) {
    var found = null;
    (function walk(n) {
      (n.children || []).forEach(function (c) {
        if (c === target) found = n;
        else walk(c);
      });
    })(root);
    return found;
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
