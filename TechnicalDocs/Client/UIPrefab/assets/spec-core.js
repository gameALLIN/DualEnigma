/* ui-spec 公共数学库（viewer.js 与 editor.js 共享）
 * 正向：spec → 屏幕矩形（calcRect / layoutChildren，Unity 锚点规则，HTML 坐标系 y 向下）
 * 逆向：屏幕矩形 → spec（rectToSpec，编辑器拖拽/手柄写回 position/size）
 * 共享同一套数学，保证预览与编辑所见一致。
 * 依赖：无（plain script，挂 window.SpecCore）
 */
(function () {
  "use strict";

  var CANVAS_W = 1280, CANVAS_H = 720; // 参考分辨率

  // ---------- 对齐 ----------

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

  // ---------- v1.2 变换访问（scale/rotation 缺省） ----------

  function scaleOf(node) {
    var s = node.scale;
    if (!s || s.length < 2) return [1, 1];
    return [s[0], s[1]];
  }

  function rotationOf(node) {
    return node.rotation || 0;
  }

  // ---------- 正向：spec → 屏幕矩形 ----------

  // 计算节点在父矩形内的布局矩形（父矩形为 HTML 坐标系：y 向下）
  // 返回值 { L, T, w, h } 为「布局矩形」，不含 scale/rotation（变换由 CSS 叠加）
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

  // ---------- 逆向：屏幕矩形 → spec（编辑器写回） ----------

  // 给定父矩形与新的布局矩形 {L,T,w,h}，按节点现有 anchors/pivot 反解 position/size。
  // anchoredPosition 语义 = 「枢轴点」相对「锚点（区）」的偏移（y 翻转）；
  // 点锚（min==max）：size=矩形尺寸；拉伸锚（min≠max）：size=矩形尺寸-锚区尺寸（sizeDelta 语义）
  function rectToSpec(node, parentRect, rect) {
    var amin = node.anchors.min, amax = node.anchors.max;
    var pivot = node.pivot || [0.5, 0.5];
    var px = parentRect.L, py = parentRect.T, pw = parentRect.w, ph = parentRect.h;
    // 枢轴点像素位（HTML 坐标，y 向下）
    var pivotX = rect.L + pivot[0] * rect.w;
    var pivotY = rect.T + (1 - pivot[1]) * rect.h;
    var pos, size;
    var pointX = Math.abs(amax[0] - amin[0]) < 1e-6;
    var pointY = Math.abs(amax[1] - amin[1]) < 1e-6;

    // X 轴
    if (pointX) {
      var anchorX = px + amin[0] * pw;
      pos = [pivotX - anchorX, 0];
      size = [rect.w, 0];
    } else {
      var rx0 = px + amin[0] * pw, rx1 = px + amax[0] * pw;
      pos = [pivotX - (rx0 + rx1) / 2, 0];
      size = [rect.w - (rx1 - rx0), 0];
    }
    // Y 轴（锚点像素位 y = py + (1-a.y)*ph；position.y 向上为正）
    if (pointY) {
      var anchorY = py + (1 - amin[1]) * ph;
      pos[1] = anchorY - pivotY;
      size[1] = rect.h;
    } else {
      var refCy = py + (1 - (amin[1] + amax[1]) / 2) * ph;
      pos[1] = refCy - pivotY;
      size[1] = rect.h - (amax[1] - amin[1]) * ph;
    }
    return { position: pos, size: size };
  }

  // 锚点/轴心变更补偿（§4.3）：先记录旧视觉矩形，写入新 anchors/pivot 后
  // 用逆向公式重算 position/size，保证视觉矩形不跳变。
  // node: 目标节点；parentRect: 父矩形；newAnchors/newPivot: 待写入值（null 表示不变）
  function applyAnchorsWithCompensation(node, parentRect, newAnchors, newPivot) {
    var rect = calcRect(node, parentRect.L, parentRect.T, parentRect.w, parentRect.h);
    if (newAnchors) node.anchors = { min: [newAnchors.min[0], newAnchors.min[1]], max: [newAnchors.max[0], newAnchors.max[1]] };
    if (newPivot) node.pivot = [newPivot[0], newPivot[1]];
    var back = rectToSpec(node, parentRect, rect);
    node.position = back.position;
    node.size = back.size;
  }

  // ---------- 数值精度（§4.5：写回前统一保留 1 位小数） ----------

  function round1(x) {
    return Math.round(x * 10) / 10;
  }

  function roundPair(arr) {
    return [round1(arr[0]), round1(arr[1])];
  }

  // ---------- 结构辅助 ----------

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

  // 节点路径（状态栏显示用）：Root/A/B
  function nodePath(root, target) {
    var parts = [];
    (function walk(n, trail) {
      if (n === target) { parts = trail.concat([n.name]); return; }
      (n.children || []).forEach(function (c) { walk(c, trail.concat([n.name])); });
    })(root, []);
    return parts.join(" / ");
  }

  // ---------- 导出 ----------

  window.SpecCore = {
    CANVAS_W: CANVAS_W,
    CANVAS_H: CANVAS_H,
    HALIGN: HALIGN,
    VALIGN: VALIGN,
    parseAlign: parseAlign,
    scaleOf: scaleOf,
    rotationOf: rotationOf,
    calcRect: calcRect,
    layoutChildren: layoutChildren,
    rectToSpec: rectToSpec,
    applyAnchorsWithCompensation: applyAnchorsWithCompensation,
    round1: round1,
    roundPair: roundPair,
    findParent: findParent,
    nodePath: nodePath,
  };
})();
