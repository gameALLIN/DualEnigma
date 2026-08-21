/* ui-spec 可视化编辑器
 * 布局数据的生产与修改：拖拽/手柄/属性面板 → 本地服务直写回 HTML 内嵌 JSON。
 * 设计稿 HTML 仍是唯一数据源；预制体生成走 Editor 侧通用生成器。
 * 交互规范见《HTML可视化编辑器.md》§三；坐标数学见 §四（复用 spec-core.js）。
 */
(function () {
  "use strict";

  var Core = window.SpecCore;
  var CANVAS_W = Core.CANVAS_W, CANVAS_H = Core.CANVAS_H;

  // ---------- DOM ----------

  var pageListEl = document.getElementById("pageList");
  var treeEl = document.getElementById("tree");
  var zoomSel = document.getElementById("zoom");
  var gridChk = document.getElementById("gridOn");
  var snapChk = document.getElementById("snapOn");
  var snapStepSel = document.getElementById("snapStep");
  var showHiddenChk = document.getElementById("showHidden");
  var undoBtn = document.getElementById("undoBtn");
  var redoBtn = document.getElementById("redoBtn");
  var saveBtn = document.getElementById("saveBtn");
  var dirtyMark = document.getElementById("dirtyMark");
  var canvasWrap = document.getElementById("canvasWrap");
  var canvasEl = document.getElementById("canvas");
  var contentEl = document.getElementById("canvasContent");
  var selectionEl = document.getElementById("selection");
  var statusPath = document.getElementById("statusPath");
  var statusMsg = document.getElementById("statusMsg");
  var propBody = document.getElementById("propBody");

  // ---------- 状态 ----------

  var pages = [];
  var currentPage = null;
  var spec = null;                 // 当前页 spec 根节点
  var selected = null;             // 选中节点（spec 对象引用）
  var rectMap = new Map();         // node → { rect, parentRect, posLocked }
  var undoStack = [], redoStack = [];
  var dirty = false;
  var dragState = null;

  var ZOOM_STEPS = [0.25, 0.5, 0.75, 1, 1.5, 2];

  // ---------- 工具 ----------

  function zoom() { return parseFloat(zoomSel.value); }
  function snapStep() { return snapChk.checked ? parseInt(snapStepSel.value, 10) : 0; }
  function snap(v) { var s = snapStep(); return s > 1 ? Math.round(v / s) * s : Math.round(v); }
  function escapeHtml(s) {
    return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
  }
  function setStatus(msg) {
    statusMsg.textContent = msg || "";
    if (msg) setTimeout(function () { if (statusMsg.textContent === msg) statusMsg.textContent = ""; }, 4000);
  }
  function setDirty(d) {
    dirty = d;
    dirtyMark.classList.toggle("on", d);
  }

  // ---------- 撤销 / 重做（JSON 快照栈，上限 50 步） ----------

  function checkpoint() {
    undoStack.push(JSON.stringify(spec));
    if (undoStack.length > 50) undoStack.shift();
    redoStack.length = 0;
    setDirty(true);
  }
  function undo() {
    if (!undoStack.length) return;
    redoStack.push(JSON.stringify(spec));
    spec = JSON.parse(undoStack.pop());
    selected = null;
    renderAll();
    setDirty(true);
  }
  function redo() {
    if (!redoStack.length) return;
    undoStack.push(JSON.stringify(spec));
    spec = JSON.parse(redoStack.pop());
    selected = null;
    renderAll();
    setDirty(true);
  }

  // ---------- 页面加载 ----------

  function loadPages() {
    return fetch("/api/pages").then(function (r) { return r.json(); }).then(function (list) {
      pages = list;
      pageListEl.innerHTML = "";
      list.forEach(function (name) {
        var li = document.createElement("li");
        li.textContent = name;
        li.dataset.page = name;
        li.addEventListener("click", function () { selectPage(name); });
        pageListEl.appendChild(li);
      });
      if (list.length && !currentPage) selectPage(list[0]);
    });
  }

  function selectPage(name) {
    if (dirty && !confirm("当前页面有未保存改动，切换将丢弃。继续？")) return;
    currentPage = name;
    Array.prototype.forEach.call(pageListEl.children, function (li) {
      li.classList.toggle("active", li.dataset.page === name);
    });
    fetch("/api/spec?page=" + encodeURIComponent(name))
      .then(function (r) {
        if (!r.ok) throw new Error("加载失败: HTTP " + r.status);
        return r.json();
      })
      .then(function (s) {
        spec = s;
        selected = null;
        undoStack.length = 0; redoStack.length = 0;
        setDirty(false);
        renderAll();
      })
      .catch(function (e) { alert(e.message); });
  }

  // ---------- 画布渲染 ----------

  function isPosLocked(node) {
    var parent = Core.findParent(spec, node);
    return !!(parent && parent.layout);
  }

  function renderAll() {
    renderCanvas();
    renderTree();
    renderProps();
    updateSelectionOverlay();
    statusPath.textContent = selected ? Core.nodePath(spec, selected) : "未选择节点";
  }

  function renderCanvas() {
    var z = zoom();
    canvasEl.style.transform = "scale(" + z + ")";
    canvasEl.classList.toggle("grid", gridChk.checked);
    contentEl.innerHTML = "";
    rectMap.clear();
    if (!spec) return;
    var showHidden = showHiddenChk.checked;
    walkRender(spec, { L: 0, T: 0, w: CANVAS_W, h: CANVAS_H }, true, showHidden);
  }

  function walkRender(node, parentRect, parentVisible, showHidden) {
    var rect = Core.calcRect(node, parentRect.L, parentRect.T, parentRect.w, parentRect.h);
    var visible = parentVisible && node.active !== false;
    var posLocked = false;

    if (node !== spec) {
      posLocked = isPosLocked(node);
      rectMap.set(node, { rect: rect, parentRect: parentRect, posLocked: posLocked });
      if (visible || showHidden) contentEl.appendChild(makeEl(node, rect, visible, posLocked));
    }

    var kids = node.children || [];
    var layoutRects = node.layout ? Core.layoutChildren(node, rect.w, rect.h, showHidden) : null;
    kids.forEach(function (c, i) {
      var cr;
      if (layoutRects && layoutRects[i]) {
        var lr = layoutRects[i];
        cr = { L: rect.L + lr.L, T: rect.T + lr.T, w: lr.w, h: lr.h };
        walkRenderAt(c, cr, visible, showHidden);
      } else {
        walkRender(c, rect, visible, showHidden);
      }
    });
  }

  // LayoutGroup 子树：矩形由布局排布给定
  function walkRenderAt(node, rect, parentVisible, showHidden) {
    var visible = parentVisible && node.active !== false;
    rectMap.set(node, { rect: rect, parentRect: null, posLocked: true });
    if (visible || showHidden) contentEl.appendChild(makeEl(node, rect, visible, true));
    var kids = node.children || [];
    var layoutRects = node.layout ? Core.layoutChildren(node, rect.w, rect.h, showHidden) : null;
    kids.forEach(function (c, i) {
      if (layoutRects && layoutRects[i]) {
        var lr = layoutRects[i];
        walkRenderAt(c, { L: rect.L + lr.L, T: rect.T + lr.T, w: lr.w, h: lr.h }, visible, showHidden);
      } else {
        walkRender(c, rect, visible, showHidden);
      }
    });
  }

  function makeEl(node, rect, visible, posLocked) {
    var el = document.createElement("div");
    var comps = node.components || [];
    var isText = "text" in node;
    var isBtn = comps.indexOf("Button") >= 0;
    var isImg = comps.indexOf("Image") >= 0;
    el.className = "el" + (visible ? "" : " ghost") + (posLocked ? " lock-pos" : "") +
      (node === selected ? " selected" : "");
    el.style.left = rect.L + "px";
    el.style.top = rect.T + "px";
    el.style.width = Math.max(1, rect.w) + "px";
    el.style.height = Math.max(1, rect.h) + "px";

    if (isText) {
      el.classList.add("txt");
      var txt = node.text || "";
      var dynamic = !txt.trim();
      if (dynamic) txt = node.name;
      var fs = node.fontSize || rect.h * 0.72;
      el.style.fontSize = Math.max(6, fs) + "px";
      el.style.color = node.color || "#ECEFF1";
      el.style.fontStyle = dynamic ? "italic" : (node.fontStyle === "italic" ? "italic" : "normal");
      el.style.opacity = dynamic ? 0.6 : 1;
      var al = Core.parseAlign(node.align);
      el.style.justifyContent = ["flex-start", "center", "flex-end"][Math.round(al.h * 2)];
      el.style.alignItems = ["flex-end", "center", "flex-start"][Math.round(al.v * 2)];
      el.style.textAlign = ["left", "center", "right"][Math.round(al.h * 2)];
      el.textContent = txt;
    } else if (isBtn) {
      el.style.background = node.background || "rgba(255,255,255,0.08)";
    } else if (isImg) {
      el.style.background = node.background || "rgba(255,255,255,0.06)";
    } else {
      el.classList.add("container");
    }

    // v1.2 变换（scale/rotation）
    var s = Core.scaleOf(node), deg = Core.rotationOf(node);
    if (s[0] !== 1 || s[1] !== 1 || deg !== 0) {
      var pivot = node.pivot || [0.5, 0.5];
      el.style.transformOrigin = (pivot[0] * 100) + "% " + ((1 - pivot[1]) * 100) + "%";
      el.style.transform = "scale(" + s[0] + "," + s[1] + ") rotate(" + (-deg) + "deg)";
    }

    el._node = node;
    el.addEventListener("mousedown", onElMouseDown);
    return el;
  }

  // ---------- 选择 ----------

  function select(node) {
    selected = node;
    Array.prototype.forEach.call(contentEl.children, function (el) {
      el.classList.toggle("selected", el._node === node);
    });
    statusPath.textContent = node ? Core.nodePath(spec, node) : "未选择节点";
    renderTree();
    renderProps();
    updateSelectionOverlay();
  }

  function updateSelectionOverlay() {
    if (!selected || !rectMap.has(selected)) { selectionEl.hidden = true; return; }
    var rect = rectMap.get(selected).rect;
    selectionEl.hidden = false;
    selectionEl.style.left = rect.L + "px";
    selectionEl.style.top = rect.T + "px";
    selectionEl.style.width = Math.max(1, rect.w) + "px";
    selectionEl.style.height = Math.max(1, rect.h) + "px";
  }

  // ---------- 拖拽移动 / 手柄 resize ----------

  function onElMouseDown(e) {
    if (e.button !== 0) return;
    e.stopPropagation();
    e.preventDefault();
    var node = e.currentTarget._node;
    select(node);
    var info = rectMap.get(node);
    dragState = {
      mode: "maybe-move", node: node,
      startX: e.clientX, startY: e.clientY,
      rect0: info.rect, locked: info.posLocked,
      checkpointed: false,
    };
    window.addEventListener("mousemove", onDragMove);
    window.addEventListener("mouseup", onDragEnd);
  }

  function onHandleMouseDown(e) {
    if (e.button !== 0) return;
    e.stopPropagation();
    e.preventDefault();
    if (!selected) return;
    var info = rectMap.get(selected);
    dragState = {
      mode: "resize", dir: e.target.dataset.dir, node: selected,
      startX: e.clientX, startY: e.clientY,
      rect0: info.rect, locked: info.posLocked,
      checkpointed: false,
    };
    window.addEventListener("mousemove", onDragMove);
    window.addEventListener("mouseup", onDragEnd);
  }

  Array.prototype.forEach.call(selectionEl.querySelectorAll(".handle"), function (h) {
    h.addEventListener("mousedown", onHandleMouseDown);
  });

  function onDragMove(e) {
    if (!dragState) return;
    var z = zoom();
    var dx = (e.clientX - dragState.startX) / z;
    var dy = (e.clientY - dragState.startY) / z;
    var st = dragState;

    if (st.mode === "maybe-move") {
      if (Math.abs(dx) + Math.abs(dy) < 3) return; // 点击阈值
      if (st.locked) { st.mode = "noop"; setStatus("位置由 LayoutGroup 接管，不可拖动"); return; }
      st.mode = "move";
    }
    if (st.mode === "noop") return;

    if (!st.checkpointed) { checkpoint(); st.checkpointed = true; }

    var r0 = st.rect0;
    if (st.mode === "move") {
      var L = snap(r0.L + dx), T = snap(r0.T + dy);
      writeRect(st.node, { L: L, T: T, w: r0.w, h: r0.h }, false);
    } else if (st.mode === "resize") {
      var rect = resizeRect(r0, st.dir, dx, dy, e.shiftKey);
      writeRect(st.node, rect, true);
    }
    renderCanvas();
    updateSelectionOverlay();
  }

  function onDragEnd() {
    if (dragState && dragState.checkpointed) {
      renderAll(); // 同步属性面板
    }
    dragState = null;
    window.removeEventListener("mousemove", onDragMove);
    window.removeEventListener("mouseup", onDragEnd);
  }

  // 8 手柄 resize：对边/对角固定；Shift+角手柄等比
  function resizeRect(r0, dir, dx, dy, proportional) {
    var L = r0.L, T = r0.T, w = r0.w, h = r0.h;
    var hasW = dir.indexOf("w") >= 0, hasE = dir.indexOf("e") >= 0;
    var hasN = dir.indexOf("n") >= 0, hasS = dir.indexOf("s") >= 0;
    if (hasE) w = r0.w + dx;
    if (hasW) { w = r0.w - dx; L = r0.L + dx; }
    if (hasS) h = r0.h + dy;
    if (hasN) { h = r0.h - dy; T = r0.T + dy; }
    if (proportional && (hasW || hasE) && (hasN || hasS) && r0.w > 0 && r0.h > 0) {
      var k = Math.max(w / r0.w, h / r0.h);
      var nw = r0.w * k, nh = r0.h * k;
      if (hasW) L = r0.L + (r0.w - nw);
      if (hasN) T = r0.T + (r0.h - nh);
      w = nw; h = nh;
    }
    w = Math.max(1, w); h = Math.max(1, h);
    // 吸附作用于尺寸与左上角
    w = snap(w); h = snap(h);
    if (hasW) L = snap(L);
    if (hasN) T = snap(T);
    return { L: L, T: T, w: w, h: h };
  }

  // 逆向公式写回（§4.2）：布局矩形 → position/size
  function writeRect(node, rect, includeSize) {
    var info = rectMap.get(node);
    if (info.posLocked) {
      // LayoutGroup 子节点：位置由布局接管，仅尺寸可编辑
      if (includeSize) node.size = Core.roundPair([rect.w, rect.h]);
      return;
    }
    var back = Core.rectToSpec(node, info.parentRect, rect);
    node.position = Core.roundPair(back.position);
    if (includeSize) node.size = Core.roundPair(back.size);
  }

  // ---------- 层级树 ----------

  function renderTree() {
    treeEl.innerHTML = "";
    if (!spec) return;
    var showHidden = showHiddenChk.checked;
    (function walk(node, depth) {
      if (node !== spec && node.active === false && !showHidden) {
        // 隐藏节点不展示（可选）
      }
      var div = document.createElement("div");
      div.className = "tnode" + (node === selected ? " selected" : "") +
        (node.active === false ? " hidden-node" : "");
      div.style.paddingLeft = (depth * 14 + 4) + "px";
      var cs = (node.components || []).filter(function (c) { return c !== "RectTransform"; });
      div.innerHTML = escapeHtml(node.name) +
        (cs.length ? ' <span class="comps">[' + escapeHtml(cs.join(",")) + "]</span>" : "");
      div._node = node;
      div.addEventListener("click", function () { select(node); });
      treeEl.appendChild(div);
      (node.children || []).forEach(function (c) { walk(c, depth + 1); });
    })(spec, 0);
  }

  // ---------- 属性面板 ----------

  var ANCHOR_POINTS = [
    [[0, 1], [0.5, 1], [1, 1]],
    [[0, 0.5], [0.5, 0.5], [1, 0.5]],
    [[0, 0], [0.5, 0], [1, 0]],
  ];

  function renderProps() {
    propBody.innerHTML = "";
    if (!selected) {
      propBody.innerHTML = '<div class="prop-empty">未选择节点</div>';
      return;
    }
    var node = selected;
    var info = rectMap.get(node) || { posLocked: isPosLocked(node) };

    var nameDiv = document.createElement("div");
    nameDiv.className = "prop-name";
    nameDiv.textContent = node.name;
    propBody.appendChild(nameDiv);

    if (info.posLocked) {
      var lock = document.createElement("div");
      lock.className = "prop-lock";
      lock.textContent = "位置由 LayoutGroup 接管（仅尺寸可编辑）";
      propBody.appendChild(lock);
    }

    // 位置
    addPairRow("位置", node.position[0], node.position[1], info.posLocked, function (x, y) {
      checkpoint();
      node.position = Core.roundPair([x, y]);
      renderAll();
    });
    // 大小
    addPairRow("大小", node.size[0], node.size[1], false, function (w, h) {
      checkpoint();
      node.size = Core.roundPair([w, h]);
      renderAll();
    });
    // 缩放
    var sc = Core.scaleOf(node);
    addPairRow("缩放", sc[0], sc[1], false, function (sx, sy) {
      checkpoint();
      sx = Math.min(10, Math.max(0.1, sx));
      sy = Math.min(10, Math.max(0.1, sy));
      node.scale = Core.roundPair([sx, sy]);
      renderAll();
    }, 0.1);
    // 旋转
    addSingleRow("旋转", Core.rotationOf(node), false, function (deg) {
      checkpoint();
      node.rotation = Core.round1(deg);
      renderAll();
    }, 1);

    // 锚点 9 宫格 + 拉伸预设
    addSection("锚点（点击预设，默认补偿不跳变）");
    addAnchorGrid(node, info);

    // 轴心 9 点
    addSection("轴心");
    addPivotGrid(node, info);
  }

  function addSection(text) {
    var div = document.createElement("div");
    div.className = "prop-section";
    div.textContent = text;
    propBody.appendChild(div);
  }

  function addPairRow(label, vx, vy, disabled, onCommit, step) {
    var row = document.createElement("div");
    row.className = "prop-row";
    var lab = document.createElement("label");
    lab.textContent = label;
    var ix = document.createElement("input");
    var iy = document.createElement("input");
    ix.type = iy.type = "number";
    ix.step = iy.step = String(step || 1);
    ix.value = Core.round1(vx); iy.value = Core.round1(vy);
    ix.disabled = iy.disabled = !!disabled;
    function commit() {
      var x = parseFloat(ix.value), y = parseFloat(iy.value);
      if (isNaN(x) || isNaN(y)) return;
      onCommit(x, y);
    }
    ix.addEventListener("change", commit);
    iy.addEventListener("change", commit);
    row.appendChild(lab); row.appendChild(ix); row.appendChild(iy);
    propBody.appendChild(row);
  }

  function addSingleRow(label, v, disabled, onCommit, step) {
    var row = document.createElement("div");
    row.className = "prop-row";
    var lab = document.createElement("label");
    lab.textContent = label;
    var input = document.createElement("input");
    input.type = "number";
    input.step = String(step || 1);
    input.value = Core.round1(v);
    input.disabled = !!disabled;
    input.addEventListener("change", function () {
      var x = parseFloat(input.value);
      if (isNaN(x)) return;
      onCommit(x);
    });
    row.appendChild(lab); row.appendChild(input);
    propBody.appendChild(row);
  }

  // 补偿复选（锚点/轴心共用状态，默认开启）
  var compensate = true;

  function addAnchorGrid(node, info) {
    var wrap = document.createElement("div");
    wrap.className = "prop-row";
    var lab = document.createElement("label");
    lab.textContent = "锚点";
    wrap.appendChild(lab);
    var grid = document.createElement("div");
    grid.className = "prop-grid";
    ANCHOR_POINTS.forEach(function (rowPts) {
      rowPts.forEach(function (pt) {
        var b = document.createElement("button");
        b.title = "锚点 (" + pt[0] + ", " + pt[1] + ")";
        b.textContent = "•";
        var a = node.anchors;
        if (a.min[0] === pt[0] && a.min[1] === pt[1] && a.max[0] === pt[0] && a.max[1] === pt[1])
          b.classList.add("on");
        b.addEventListener("click", function () {
          applyAnchorChange(node, info, { min: [pt[0], pt[1]], max: [pt[0], pt[1]] });
        });
        grid.appendChild(b);
      });
    });
    wrap.appendChild(grid);
    propBody.appendChild(wrap);

    // 拉伸预设行
    var row = document.createElement("div");
    row.className = "prop-row";
    var lab2 = document.createElement("label");
    row.appendChild(lab2);
    [["↔ 横拉", "h"], ["↕ 竖拉", "v"], ["⛶ 铺满", "f"]].forEach(function (item) {
      var b = document.createElement("button");
      b.textContent = item[0];
      b.style.flex = "1";
      b.addEventListener("click", function () {
        var a = node.anchors, na;
        if (item[1] === "h") na = { min: [0, a.min[1]], max: [1, a.max[1]] };
        else if (item[1] === "v") na = { min: [a.min[0], 0], max: [a.max[0], 1] };
        else na = { min: [0, 0], max: [1, 1] };
        applyAnchorChange(node, info, na);
      });
      row.appendChild(b);
    });
    propBody.appendChild(row);

    var chk = document.createElement("label");
    chk.className = "prop-check";
    chk.innerHTML = '<input type="checkbox"' + (compensate ? " checked" : "") + "> 补偿（保持视觉矩形）";
    chk.querySelector("input").addEventListener("change", function (e) {
      compensate = e.target.checked;
    });
    propBody.appendChild(chk);
  }

  function addPivotGrid(node, info) {
    var wrap = document.createElement("div");
    wrap.className = "prop-row";
    var lab = document.createElement("label");
    lab.textContent = "轴心";
    wrap.appendChild(lab);
    var grid = document.createElement("div");
    grid.className = "prop-grid";
    ANCHOR_POINTS.forEach(function (rowPts) {
      rowPts.forEach(function (pt) {
        var b = document.createElement("button");
        b.title = "轴心 (" + pt[0] + ", " + pt[1] + ")";
        b.textContent = "•";
        var p = node.pivot || [0.5, 0.5];
        if (p[0] === pt[0] && p[1] === pt[1]) b.classList.add("on");
        b.addEventListener("click", function () { applyPivotChange(node, info, pt); });
        grid.appendChild(b);
      });
    });
    wrap.appendChild(grid);
    propBody.appendChild(wrap);
  }

  // 锚点变更（§4.3：记录旧矩形 → 写新锚点 → 逆向重算 position/size，一次撤销步）
  function applyAnchorChange(node, info, newAnchors) {
    if (!info.parentRect) { setStatus("LayoutGroup 子节点不支持改锚点"); return; }
    checkpoint();
    if (compensate) {
      Core.applyAnchorsWithCompensation(node, info.parentRect, newAnchors, null);
      node.position = Core.roundPair(node.position);
      node.size = Core.roundPair(node.size);
    } else {
      node.anchors = { min: [newAnchors.min[0], newAnchors.min[1]], max: [newAnchors.max[0], newAnchors.max[1]] };
    }
    renderAll();
  }

  // 轴心变更（同样补偿）
  function applyPivotChange(node, info, pt) {
    if (!info.parentRect) { setStatus("LayoutGroup 子节点不支持改轴心"); return; }
    checkpoint();
    if (compensate) {
      Core.applyAnchorsWithCompensation(node, info.parentRect, null, pt);
      node.position = Core.roundPair(node.position);
      node.size = Core.roundPair(node.size);
    } else {
      node.pivot = [pt[0], pt[1]];
    }
    renderAll();
  }

  // ---------- 键盘 ----------

  window.addEventListener("keydown", function (e) {
    var inInput = /^(INPUT|TEXTAREA|SELECT)$/.test(document.activeElement.tagName);

    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") {
      e.preventDefault();
      save();
      return;
    }
    if ((e.ctrlKey || e.metaKey) && !e.shiftKey && e.key.toLowerCase() === "z") {
      e.preventDefault(); undo(); return;
    }
    if ((e.ctrlKey || e.metaKey) && (e.key.toLowerCase() === "y" ||
        (e.shiftKey && e.key.toLowerCase() === "z"))) {
      e.preventDefault(); redo(); return;
    }
    if (inInput || !selected) return;

    var step = e.shiftKey ? 10 : 1;
    var dirs = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
    var d = dirs[e.key];
    if (!d) return;
    e.preventDefault();
    var info = rectMap.get(selected);
    if (!info || info.posLocked) { setStatus("位置由 LayoutGroup 接管"); return; }
    checkpoint();
    var rect = info.rect;
    writeRect(selected, { L: rect.L + d[0], T: rect.T + d[1], w: rect.w, h: rect.h }, false);
    renderAll();
  });

  // 空格 + 拖拽平移；滚轮缩放（仅视图，不动数据）
  var panState = null;
  window.addEventListener("keydown", function (e) {
    if (e.code === "Space" && !/^(INPUT|TEXTAREA)$/.test(document.activeElement.tagName)) {
      e.preventDefault();
      canvasWrap.classList.add("panning");
    }
  });
  window.addEventListener("keyup", function (e) {
    if (e.code === "Space") canvasWrap.classList.remove("panning");
  });
  canvasWrap.addEventListener("mousedown", function (e) {
    if (e.target !== canvasWrap && e.target !== canvasEl) return;
    if (e.code === "Space" || e.button === 1 || canvasWrap.classList.contains("panning")) {
      panState = { x: e.clientX, y: e.clientY, sl: canvasWrap.scrollLeft, st: canvasWrap.scrollTop };
      e.preventDefault();
    } else if (e.target === canvasEl || e.target === canvasWrap) {
      select(null); // 点击空白取消选择
    }
  });
  window.addEventListener("mousemove", function (e) {
    if (!panState) return;
    canvasWrap.scrollLeft = panState.sl - (e.clientX - panState.x);
    canvasWrap.scrollTop = panState.st - (e.clientY - panState.y);
  });
  window.addEventListener("mouseup", function () { panState = null; });
  canvasWrap.addEventListener("wheel", function (e) {
    e.preventDefault();
    var cur = zoom();
    var idx = ZOOM_STEPS.indexOf(cur);
    if (idx < 0) idx = 1;
    idx += e.deltaY < 0 ? 1 : -1;
    idx = Math.max(0, Math.min(ZOOM_STEPS.length - 1, idx));
    zoomSel.value = String(ZOOM_STEPS[idx]);
    renderCanvas();
    updateSelectionOverlay();
  }, { passive: false });

  // ---------- 工具栏事件 ----------

  zoomSel.addEventListener("change", function () { renderCanvas(); updateSelectionOverlay(); });
  gridChk.addEventListener("change", renderCanvas);
  showHiddenChk.addEventListener("change", renderAll);
  undoBtn.addEventListener("click", undo);
  redoBtn.addEventListener("click", redo);
  saveBtn.addEventListener("click", save);

  // ---------- 保存（Ctrl+S → POST /api/save） ----------

  function save() {
    if (!spec || !currentPage) return;
    saveBtn.disabled = true;
    fetch("/api/save?page=" + encodeURIComponent(currentPage), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(spec),
    }).then(function (r) {
      return r.text().then(function (text) {
        if (!r.ok) throw new Error(text || ("HTTP " + r.status));
        setDirty(false);
        setStatus("已保存 " + new Date().toLocaleTimeString());
      });
    }).catch(function (e) {
      alert("保存失败：" + e.message);
    }).finally(function () {
      saveBtn.disabled = false;
    });
  }

  // ---------- 离开保护 ----------

  window.addEventListener("beforeunload", function (e) {
    if (dirty) {
      e.preventDefault();
      e.returnValue = "";
    }
  });

  // ---------- 启动 ----------

  loadPages().catch(function (e) {
    document.body.innerHTML = '<div style="padding:40px;color:#EF5350">' +
      "无法连接编辑服务。请先启动：python tools/ui_editor.py<br><small>" + escapeHtml(e.message) + "</small></div>";
  });
})();
