# AI角色3D模型生成文档

> **文档版本**: v1.0  
> **最后更新**: 2026-08-22  
> **文档状态**: 设计草案（3D风格试验分支，未立项）  
> **对应GDD版本**: v6.1  
> **前置文档**: 美术需求文档 v2.0 / AI美术提示词清单 v1.0  
> **适用工具**: 文生3D（Meshy / Tripo / Rodin / Genie）、图生3D、AI动画（Meshy Animation / Mixamo / Cascadeur）、3D效果预览（Midjourney / SDXL）  
> **说明**: 本文档为水人/火人双主角3D化的**二次创作试验文档**，刻意突破2D矢量平涂与描边约束，探索3D材质化表达。**不影响2D主线美术方向**。角色名沿用「水人 Aqua / 火人 Ignis」。

---

## 目录

- [一、文档定位与使用说明](#一文档定位与使用说明)
- [二、二次创作设计理念](#二二次创作设计理念)
- [三、角色3D重设计](#三角色3d重设计)
- [四、基础模型生成提示词](#四基础模型生成提示词)
- [五、动作动画提示词](#五动作动画提示词)
- [六、技术规格与验收](#六技术规格与验收)
- [七、待确认事项](#七待确认事项)

---

## 一、文档定位与使用说明

### 1.1 本文档性质

- **二次创作**：不是把2D提示词直译成3D，而是以2D角色的"识别锚点"为骨架重新设计材质、体积与细节。
- **试验分支**：用于验证3D风格是否值得立项，产出物不直接进入正式资源管线。
- **打破约束**：不再受"矢量平涂/粗黑描边/二分明暗/透明背景"限制，改用材质语言（半透明、次表面散射、自发光）表达元素属性。

### 1.2 推荐工作流

```
① 风格锚定   用 Midjourney/SDXL 生成角色三视图定妆图（§4.1/§4.3）
      ↓
② 生成模型   图生3D（Meshy image-to-3D / Tripo）上传三视图生成基础模型
             （或直接用文生3D提示词 §4.2/§4.4）
      ↓
③ 清理绑骨   拓扑清理 → 自动绑骨（Mixamo / AccuRIG，标准人形骨架）
      ↓
④ 生成动作   Meshy text-to-animation 输入 §5 动作提示词
             或 Mixamo 动作库近似替换 + DCC 手K精修（参考各动作关键帧描述）
      ↓
⑤ 引擎验证   导出 GLB/FBX → Unity URP 验证材质（SSS用Shader替代，自发光用Emission）
```

> **提示**: 文生3D工具普遍不识别 hex 色值，本文档文生3D提示词一律使用颜色描述词；渲染预览图提示词（Midjourney/SDXL）保留 hex 供参考。

---

## 二、二次创作设计理念

### 2.1 保留的识别锚点（角色不能丢的东西）

| 锚点 | 说明 |
|------|------|
| 元素头部 | 头部即元素本体：水滴头 / 火焰头 |
| Q版比例 | 大头小身，头约占全身75%，头身比约1:2.5 |
| 互补配色 | 水人蓝系 / 火人橙红系，冷暖互补 |
| 性格反差 | 水人温柔沉静（慢节奏曲线）/ 火人自信急躁（快节奏、爆发感） |
| 专属能力暗示 | 水人与液体融合 / 火人二段跳与点燃 |

### 2.2 打破的2D约束 → 3D替代表达

| 2D原约束 | 3D二次创作表达 |
|----------|----------------|
| 矢量平涂 + 二分明暗 | 材质三件套：半透明体积 / 次表面散射(SSS) / 自发光(Emission) |
| 粗黑描边3px | 轮廓光(Rim Light) + 强剪影造型，必要时Toon Shader补描边 |
| 径向渐变上色 | 内→外发光结构：体内发光核心 + 外层半透明介质 |
| 纸片人（无厚度） | 实体材质叙事：果冻质感 / 黑曜石壳+岩浆裂纹 |
| 静态贴图表情 | 唯一不透明部位=眼睛，作为表情焦点；其余靠发光与形变传情绪 |

### 2.3 新增的"材质故事"设计

- **水人 = 灯笼里的海**：半透明身体是一层"水壳"，内部悬浮气泡与一颗发光水核，情绪波动时气泡上浮速度变化。
- **火人 = 裂开的熔岩蛋**：哑光黑曜石外壳布满透光岩浆裂纹，裂纹亮度=技能冷却/情绪强度；头部是半透明火焰"活灯笼"。

---

## 三、角色3D重设计

### 3.1 水人 Aqua —「深海果冻」

**设计关键词**: 半透明 / 果冻 / 气泡 / 发光水核 / 温柔

**造型分解:**

| 部位 | 设计 | 材质 |
|------|------|------|
| 头部 | 大水滴形（保留标志轮廓），表面光滑，头顶一撮向上卷起的水花呆毛（心情指示器：开心时弹跳） | 半透明+清漆高光 |
| 眼睛 | 大而圆的深蓝下垂眼，高光为小气泡造型；**全身唯一不透明部位** | 贴图+法线 |
| 身体 | 小巧梨形果冻身，内含3~5颗缓慢上浮的气泡 + 胸口一颗淡蓝发光水核 | 次表面散射(SSS) |
| 四肢 | 短圆柱腿 + 圆润鳍状短臂 | 半透明果冻 |
| 底座 | 站立时脚下有一圈涟漪水洼，待机时缓慢扩散（特效层） | 半透明水 |

**配色（3D化梯度）:**

| 层 | 颜色 | 色值参考 |
|----|------|---------|
| 表面/外壳 | 浅冰蓝 | #E1F5FE |
| 介质/体内 | 湖水蓝 | #4FC3F7 |
| 核心/发光 | 深海蓝(自发光) | #0277BD |
| 眼睛 | 深蓝+白高光 | #01579B |

### 3.2 火人 Ignis —「黑曜熔核」

**设计关键词**: 黑曜石 / 岩浆裂纹 / 活灯笼 / 自信 / 爆发

**造型分解:**

| 部位 | 设计 | 材质 |
|------|------|------|
| 头部 | 火焰形态半透明发光体，像一盏"活着的灯笼"，火焰尖端全部圆角化（禁止尖刺） | 双层半透明：内焰亮黄、外焰橙红 |
| 眼睛 | 琥珀色豆豆眼，自信上扬，咧嘴笑露一颗小虎牙；**全身唯一不透明部位** | 贴图+自发光 |
| 身体 | 圆润黑曜石/火山岩蛋壳，表面布满透光岩浆裂纹（亮度随情绪/冷却变化） | 哑光岩石+自发光裂纹 |
| 背部 | 小火山口突起，激动/二段跳时喷发火星（特效层） | 岩石+粒子 |
| 四肢 | 粗短岩石臂，指节圆钝；短柱腿，跑动留下短暂余烬脚印 | 哑光岩石 |

**配色（3D化梯度）:**

| 层 | 颜色 | 色值参考 |
|----|------|---------|
| 外壳 | 哑光炭黑 | #2B2B2B |
| 裂纹/外焰 | 橙红(自发光) | #FF4500 / #FF6F00 |
| 内焰/核心 | 暖金黄(强自发光) | #FFE082 |
| 眼睛 | 琥珀 | #FFB300 |

### 3.3 共同规格

| 项 | 规范 |
|----|------|
| 头身比 | 约1:2.5（头占全身75%体积） |
| 模型高度 | 2 Unity单位（延续2格高概念，1格=1单位） |
| 剪影测试 | 纯黑色剪影下可分辨"水滴"与"火焰"即合格 |
| 骨架 | 标准人形骨架（Mixamo兼容，便于动作库复用） |
| 双角色镜像原则 | 两者轮廓、比例、骨架完全一致，仅"材质+头部形状+性格动画节奏"不同 |

---

## 四、基础模型生成提示词

### 4.1 水人 Aqua — 三视图定妆图（Midjourney / SDXL，用于图生3D输入）

**英文 Prompt (Midjourney):**
```
character design sheet, three views front side back of a cute chibi water spirit, translucent jelly drop-shaped head with a curling splash ahoge on top, big gentle round blue droopy eyes, small pear-shaped translucent body with floating bubbles inside and a glowing light-blue core in the chest, short fin-like arms and stubby legs, standing on a small ripple puddle base, soft subsurface scattering, glossy wet jelly look, pale ice blue to deep ocean blue gradient, stylized cartoon 3D render style, Pixar-like proportions, same character in all views, full body, clean white background --ar 16:9 --style raw --no realistic human, text, background scenery, multiple characters, sharp spikes
```

**关键参数:** 16:9 三视图并排 | Pixar风Q版 | 半透明果冻质感

### 4.2 水人 Aqua — 文生3D（Meshy / Tripo / Rodin）

**英文 Prompt:**
```
cute chibi water spirit character 3D model, translucent jelly drop-shaped head with a small curling splash of hair on top, big gentle round blue eyes, small pear-shaped translucent jelly body with tiny floating bubbles inside and a glowing light-blue core in the chest, short rounded fin-like arms and stubby legs, standing on a small ripple puddle base, soft subsurface scattering, glossy wet look, colors from pale ice blue surface to deep ocean blue core, stylized cartoon game figurine, clean simple design, single character, full body, A-pose, plain background
```

### 4.3 火人 Ignis — 三视图定妆图（Midjourney / SDXL）

**英文 Prompt (Midjourney):**
```
character design sheet, three views front side back of a cute chibi fire spirit, translucent glowing flame-shaped head with rounded tips like a living lantern, bright amber eyes and confident grin, small round obsidian rock body with glowing orange magma cracks, small volcano vent on the back, chunky rounded rock arms and stubby legs, matte stone shell with emissive cracks, warm gold flame glow, stylized cartoon 3D render style, Pixar-like proportions, same character in all views, full body, clean white background --ar 16:9 --style raw --no realistic human, text, background scenery, multiple characters, sharp spikes
```

**关键参数:** 16:9 三视图并排 | 哑光岩石+自发光裂纹 | 火焰头圆角化

### 4.4 火人 Ignis — 文生3D（Meshy / Tripo / Rodin）

**英文 Prompt:**
```
cute chibi fire spirit character 3D model, translucent glowing flame-shaped head with rounded flickering tips like a living lantern, bright amber eyes with confident grin, small round obsidian rock body with glowing orange magma cracks, small volcano vent on the back, short chunky rock arms with rounded knuckles and stubby legs, ember glow from within, matte stone texture with emissive cracks, colors dark charcoal rock shell with orange-red glow and warm gold flame, stylized cartoon game figurine, clean simple design, single character, full body, A-pose, plain background
```

### 4.5 通用负面提示词（文生3D）

```
realistic human, detailed anatomy, sharp spikes, complex details, multiple characters, scene, background, text, photorealistic, 2D flat, thin body, long limbs
```

> **重点**: `sharp spikes` 是火人生成命门——AI极易把火焰头生成尖锐杂刺，务必保留该负面词；`thin body, long limbs` 防止AI回归正常人体比例。

---

## 五、动作动画提示词

> **使用说明**: 
> - 动作提示词用于 Meshy text-to-animation 等AI动画工具；关键帧描述供 Mixamo 选帧参考与手K精修。
> - 命名规范: `{角色}_{动作}_{Loop|Once}`，如 `Aqua_Idle_Loop`。
> - 双角色**节奏差异化**是二次创作核心：水人曲线柔、火人节奏冲。

### 5.1 待机 Idle（循环，3~5秒）

**水人版** — 慢呼吸+气泡+呆毛：
- 关键帧：果冻呼吸(squash&stretch ±5%) → 气泡缓慢上浮 → 呆毛轻摆 → 每4秒眨眼
```
idle breathing loop, gentle jelly-like squash and stretch, body sways slightly left and right, bubbles slowly rising inside translucent belly, splash-shaped ahoge bobbing softly, occasional slow blink, calm and relaxed cute chibi water spirit
```

**火人版** — 叉腰+跺脚+火焰乱窜：
- 关键帧：叉腰站姿 → 火焰头不规则跳动 → 裂纹脉动发光 → 脚尖不耐烦点地 → 每3秒左右张望
```
confident idle stance with hands on hips, flame head flickering restlessly, magma cracks pulsing glow, impatient toe tapping, occasional head turn looking around, energetic cocky chibi fire spirit, loop
```

### 5.2 行走 Walk（循环，约1秒/周期）

**水人版** — 轻盈弹跳步：
```
light bouncy walk cycle, small hops between steps, jelly body jiggling softly with each step, fin arms swinging gently, cheerful and light, cute chibi water spirit, loop
```

**火人版** — 大步流星：
```
energetic strut walk, confident long strides, exaggerated arm swings, flame head streaming backward, leaving faint ember footprints, proud chibi fire spirit, loop
```

### 5.3 奔跑 Run（循环，约0.7秒/周期）

**水人版** — 游泳式冲刺：
- 关键帧：前倾 → 双手向后贴身如游泳 → 身体拉长流线型 → 脚后水花尾迹
```
fast forward-leaning run, arms tucked back like swimming, body stretching streamlined, small water splash trail behind feet, determined expression, cute chibi water spirit, loop
```

**火人版** — 彗星冲刺：
- 关键帧：前倾 → 火焰拉成尾焰 → 背部火山口喷火星 → 重踏脚步
```
comet-like sprint, flame head stretching into a tail behind, sparks jetting from the back vent, heavy stomping footsteps, leaning forward, chibi fire spirit, loop
```

### 5.4 跳跃 Jump（单次，非循环）

**水人版 — 单跳**（约1.2秒）：
- 关键帧：下蹲蓄力(果冻压缩) → 弹起伸展 → 空中团身 → 落地水花溅开 → 果冻回弹
```
single jump animation: squash down to charge, spring up stretching tall, tuck in mid-air, land with a water splash and jelly bounce back, cute chibi water spirit, non-loop
```

**火人版 — 二段跳（专属）**（约1.8秒）：
- 关键帧：下蹲 → 一段跳起 → 空中蜷缩下踩(脚下火焰环爆发) → 二段升空+螺旋 → 重落地+裂纹闪光冲击
```
double jump animation: crouch, first leap, mid-air tuck and stomp triggering a fire ring burst under feet, second boost upward with a spin, heavy landing with ember shockwave, chibi fire spirit, non-loop
```

### 5.5 拾取 Pickup（单次，约1秒）

**水人版** — 双手捧起溶入核心：
- 关键帧：弯腰 → 双手捧起碎片 → 碎片化作水溶入胸口 → 水核变亮一档 → 满足微表情
```
pickup animation: bend down, scoop up a crystal shard with both hands, shard dissolves into the chest core with bubble effect, core glows brighter, delighted expression, cute chibi water spirit, non-loop
```

**火人版** — 单手抛接塞入裂纹：
- 关键帧：单手抓取 → 顺手抛起接住 → 碎片没入胸口裂纹 → 裂纹闪橙光 → 得意咧嘴
```
pickup animation: snatch a shard with one hand, casually toss and catch it, shard sinks into the chest crack with an orange flash, cocky grin, chibi fire spirit, non-loop
```

### 5.6 建造 Build（单次，约1.5秒）

> 对应游戏内"建造阶段"放置砖块动作。

**水人版** — 凝水成砖：
- 关键帧：双手抬起 → 水漩涡在掌上缠绕凝聚成砖 → 双手轻放置 → 满意点头
```
build animation: raise both arms, water spiral swirling above hands forming a brick, gently place it down with both hands, satisfied nod, cute chibi water spirit, non-loop
```

**火人版** — 锤地召砖：
- 关键帧：单手锤地(地面裂开发光) → 砖块从裂缝弹出 → 单手拍入墙体 → 握拳收势
```
build animation: slam one hand to the ground, ground cracks glowing, brick pops up from the crack, slap it into place with one hand, clench fist proudly, chibi fire spirit, non-loop
```

---

## 六、技术规格与验收

### 6.1 资源规格（试验标准，立项后按平台细化）

| 项 | 规格 |
|----|------|
| 面数 | 单角色 ≤ 15,000 tris |
| 贴图 | 1× 2K Color + 1× Emissive（发光要素分离） |
| 骨骼 | 标准人形约55根（Mixamo兼容） |
| 格式 | GLB（工具预览）/ FBX（Unity导入） |
| 命名 | `Aqua_Idle_Loop` / `Ignis_Jump_Double_Once` 等 |
| Unity | 模型高度2单位；URP管线；SSS用Toon/Custom Shader替代，发光用Emission |

### 6.2 验收标准

- [ ] 纯剪影可分辨水滴头 vs 火焰头
- [ ] 火人头部无尖锐杂刺（圆角火焰）
- [ ] 双角色站在一起比例、骨架完全一致
- [ ] 6套动作全部可循环/可单次播完，无穿模
- [ ] 水人半透明与火人自发光在URP下渲染正常（无透明排序错误）

---

## 七、待确认事项

| # | 事项 | 状态 |
|---|------|------|
| 1 | 3D风格分支是否正式立项（当前GDD为纯2D） | 🔴 待试验效果评估 |
| 2 | 半透明水人 + 自发光火人在URP透明队列的排序方案 | 🟡 待技术验证 |
| 3 | AI动画生成 vs Mixamo库改造 vs 全手K 的成本/质量取舍 | 🟡 待试验 |
| 4 | 若立项，2D/3D是否双轨并行还是全面转3D | 🔴 待决策 |
