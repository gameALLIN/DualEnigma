# AI美术提示词清单

> **文档版本**: v1.0  
> **最后更新**: 2026-08-13  
> **文档状态**: 初稿  
> **对应GDD版本**: v6.1  
> **对应美术需求**: v2.0  
> **适用工具**: Midjourney v6 / Stable Diffusion XL (SDXL)  
> **说明**: 本文档为所有游戏美术资源提供可直接使用的AI生成提示词。用户已决定从程序化生成转向外部AI美术工具生成。

---

## 目录

- [一、全局风格总则](#一全局风格总则)
- [二、Phase 1 — 核心资源](#二phase-1--核心资源)
- [三、Phase 2 — 灾害特效](#三phase-2--灾害特效)
- [四、Phase 3 — UI资源](#四phase-3--ui资源)
- [五、Phase 4 — 环境背景](#五phase-4--环境背景)
- [六、Phase 5 — 细节资源](#六phase-5--细节资源)
- [附录A：色值速查表](#附录a色值速查表)
- [附录B：资源统计总览](#附录b资源统计总览)

---

## 一、全局风格总则

### 1.1 核心风格定义

| 维度 | 规范 | 来源 |
|------|------|------|
| 整体风格 | 矢量几何风格 + 迷雾废墟氛围 | CODELY.md |
| 灾难风格 | 元素灾难卡通风格（卡通化自然灾害，平衡紧张感与亲和力） | 美术需求v2.0 |
| 角色风格 | 森林冰火人风格 — Q版大头小身（头占75%），头部即元素本体 | CODELY.md |
| 描边规范 | 粗黑外轮廓3px，描边色#050505；主次线条区分，大型物体粗描边，细节细内线 | CODELY.md + 美术需求v2.0 |
| 上色方式 | 平涂固有色 + 二分明暗（亮面+单层暗部阴影），极少柔和渐变；角色使用径向渐变 | CODELY.md + 美术需求v2.0 |
| 光影逻辑 | 固定左上主光源，阴影统一向右下方投射 | 美术需求v2.0 |
| 边角处理 | 物体造型卡通概括，边角柔和圆角处理 | 美术需求v2.0 |
| PPU | 32（1格 = 32px = 1 Unity单位） | CODELY.md |
| 背景基色 | 深青灰 #263238 | CODELY.md |
| 天空渐变 | #1A237E → #283593 | CODELY.md |

### 1.2 全局风格关键词（所有提示词共用）

以下关键词应附加到**每个**英文Prompt末尾，确保风格统一：

**Midjourney 风格后缀:**
```
vector art style, geometric shapes, flat colors, cel-shaded, 2-tone shading, thick black outline 3px, cartoon disaster aesthetic, misty ruins atmosphere, game asset, transparent background, clean edges, upper-left lighting, soft rounded corners, 2D side-scroller game, pixel-perfect at 32 PPU
```

**Stable Diffusion 风格后缀:**
```
vector art, geometric style, flat colors, cel shading, 2-tone shading, thick black outlines, cartoon aesthetic, game asset sprite, transparent background, clean edges, upper-left light source, rounded corners, 2D platformer game art, high quality, detailed
```

### 1.3 全局负面提示词

**Midjourney Negative (--no):**
```
--no realistic, photorealistic, 3D render, photograph, human skin, text, watermark, signature, shadow on background, gradient mesh, noise, blur, multiple objects, messy background, complex details, anatomy, real person
```

**Stable Diffusion Negative Prompt:**
```
realistic, photorealistic, 3D render, photograph, text, watermark, signature, background shadow, gradient mesh, noise, blur, messy, multiple objects, complex details, human anatomy, real person, nsfw, low quality, jpeg artifacts, deformed, ugly, bad proportions
```

### 1.4 尺寸比例速查

| 资源类型 | 比例 | Midjourney参数 | SDXL推荐尺寸 |
|----------|------|---------------|-------------|
| 角色（站立） | 1:2 | `--ar 1:2` | 512×1024 |
| 角色（动画帧） | 1:1 | `--ar 1:1` | 1024×1024 |
| 碎片/材料/砖块 | 1:1 | `--ar 1:1` | 512×512 |
| 建筑（竖直类） | 1:2 | `--ar 1:2` | 512×1024 |
| 建筑（水平类） | 2:1 | `--ar 2:1` | 1024×512 |
| 建筑（金字塔/封闭） | 1:1 | `--ar 1:1` | 1024×1024 |
| 灾害特效 | 16:9 | `--ar 16:9` | 1024×576 |
| 环境背景 | 2:1 | `--ar 2:1` | 1280×640 |
| UI图标 | 1:1 | `--ar 1:1` | 512×512 |
| UI面板 | 4:1或3:1 | `--ar 4:1` | 1024×256 |
| 天赋卡片 | 3:4 | `--ar 3:4` | 768×1024 |
| 主菜单背景 | 16:9 | `--ar 16:9` | 1920×1080 |

### 1.5 配色规范总表

| 元素 | 配色 | 色值 |
|------|------|------|
| 水人中心 | 浅水蓝 | #E1F5FE |
| 水人中段 | 湖水蓝 | #4FC3F7 |
| 水人边缘 | 深海蓝 | #0277BD |
| 火人中心 | 浅金黄 | #FFE082 |
| 火人中段 | 深橙 | #FF6F00 |
| 火人边缘 | 暗红橙 | #BF360C |
| 角色描边 | 纯黑 | #050505 |
| 背景基色 | 深青灰 | #263238 |
| 天空上 | 深靛蓝 | #1A237E |
| 天空下 | 靛蓝 | #283593 |
| 安全区高亮 | 亮绿 | rgba(50,255,100,0.2) |
| 安全区边界 | 绿色光柱 | rgba(50,255,100,0.4) |
| HP条 | 红色 | #E53935 |
| 能量条(水人) | 蓝色 | #4FC3F7 |
| 能量条(火人) | 橙色 | #FF9800 |
| 蓝图高亮 | 半透明绿 | rgba(50,255,100,0.2) |
| UI面板背景 | 深半透明 | rgba(20,20,30,0.7) |
| 稀有度-普通 | 白色边框 | #FFFFFF |
| 稀有度-稀有 | 蓝色边框 | #4FC3F7 |
| 稀有度-史诗 | 紫色边框 | #9370DB |

---

## 二、Phase 1 — 核心资源

### 1.1 角色

---

#### 1.1.1 水人（Aqua）— 站立姿态

**中文描述**: 水元素Q版角色，森林冰火人风格。头部占身体75%，头部即水元素本体（水滴形态），身体小巧。头部为流线型水滴造型，表面有流动水纹质感，表情柔和亲和。径向渐变上色：中心浅水蓝#E1F5FE，中段湖水蓝#4FC3F7，边缘深海蓝#0277BD。粗黑描边3px，描边色#050505。整体2格高（64px等效），1格宽（32px等效）。左上光源，右下阴影。透明背景。

**英文 Prompt (Midjourney):**
```
cute chibi water element character, forest fireboy watergirl style, big head 75% of body, head is a water droplet shape, small body, flowing water texture on surface, soft friendly expression, radial gradient coloring center light blue #E1F5FE mid blue #4FC3F7 edge dark blue #0277BD, thick black outline 3px color #050505, 2D vector art, standing pose, front view, transparent background --ar 1:2 --style raw --no realistic, 3D, photograph, text, background
```

**英文 Prompt (SDXL):**
```
cute chibi water element character, forest fireboy watergirl art style, oversized head 75% of body, head shaped like water droplet, small stubby body, flowing water texture on surface, soft friendly facial expression, radial gradient from light blue E1F5FE to mid blue 4FC3F7 to dark blue 0277BD, thick black outline, 2D vector art style, standing pose front view, transparent background, clean edges, game character sprite, upper-left lighting, cel-shaded, flat colors with 2-tone shading
```

**关键参数:**
- 比例: 1:2 (高瘦形，2格高×1格宽)
- 风格: 矢量几何、Q版大头小身、径向渐变
- 配色: #E1F5FE → #4FC3F7 → #0277BD，描边#050505
- PPU: 32（最终导出64×32px）

**负面提示词:**
```
realistic, 3D render, photograph, human proportions, small head, detailed anatomy, text, watermark, background scenery, gradient mesh, noise, multiple characters
```

---

#### 1.1.2 火人（Ignis）— 站立姿态

**中文描述**: 火元素Q版角色，森林冰火人风格。头部占身体75%，头部即火元素本体（火焰形态），身体小巧。头部为火焰跳动造型，边缘有火焰跳动效果，表情活泼充满活力。径向渐变上色：中心浅金黄#FFE082，中段深橙#FF6F00，边缘暗红橙#BF360C。粗黑描边3px，描边色#050505。整体2格高（64px等效），1格宽（32px等效）。左上光源，右下阴影。透明背景。

**英文 Prompt (Midjourney):**
```
cute chibi fire element character, forest fireboy watergirl style, big head 75% of body, head is a flame shape with flickering edges, small body, fire texture on surface, lively energetic expression, radial gradient coloring center light gold #FFE082 mid deep orange #FF6F00 edge dark red-orange #BF360C, thick black outline 3px color #050505, 2D vector art, standing pose, front view, transparent background --ar 1:2 --style raw --no realistic, 3D, photograph, text, background
```

**英文 Prompt (SDXL):**
```
cute chibi fire element character, forest fireboy watergirl art style, oversized head 75% of body, head shaped like flame with flickering edges, small stubby body, fire texture on surface, lively energetic facial expression, radial gradient from light gold FFE082 to deep orange FF6F00 to dark red-orange BF360C, thick black outline, 2D vector art style, standing pose front view, transparent background, clean edges, game character sprite, upper-left lighting, cel-shaded, flat colors with 2-tone shading
```

**关键参数:**
- 比例: 1:2
- 风格: 矢量几何、Q版大头小身、径向渐变
- 配色: #FFE082 → #FF6F00 → #BF360C，描边#050505
- PPU: 32（最终导出64×32px）

**负面提示词:**
```
realistic, 3D render, photograph, human proportions, small head, detailed anatomy, text, watermark, background scenery, gradient mesh, noise, multiple characters, smoke, ashes
```

---

#### 1.1.3 水人 — 行走动画帧（4帧）

**中文描述**: 水人行走动画，4帧循环。保持Q版大头小身风格，头部水滴形态不变，身体做行走摆动。腿部位移、手臂微摆。配色和描边与站立姿态一致。每帧1:2比例，透明背景，帧间间距均等。

**英文 Prompt (Midjourney):**
```
cute chibi water element character walk cycle spritesheet, 4 frames horizontal strip, forest fireboy watergirl style, big head 75% of body, water droplet head, small body walking animation, radial gradient center #E1F5FE mid #4FC3F7 edge #0277BD, thick black outline 3px, 2D vector art, transparent background, game animation spritesheet --ar 4:1 --style raw --no realistic, 3D, text, background
```

**关键参数:**
- 比例: 4:1（4帧并排）
- 风格: 同站立姿态
- 配色: 同水人
- PPU: 32（每帧64×32px，总256×32px）

**负面提示词:**
```
realistic, 3D, text, background, inconsistent style, different character, multiple rows
```

---

#### 1.1.4 火人 — 行走动画帧（4帧）

**中文描述**: 火人行走动画，4帧循环。保持Q版大头小身风格，头部火焰形态不变（火焰可微跳动），身体做行走摆动。配色和描边与站立姿态一致。

**英文 Prompt (Midjourney):**
```
cute chibi fire element character walk cycle spritesheet, 4 frames horizontal strip, forest fireboy watergirl style, big head 75% of body, flame head with slight flicker, small body walking animation, radial gradient center #FFE082 mid #FF6F00 edge #BF360C, thick black outline 3px, 2D vector art, transparent background, game animation spritesheet --ar 4:1 --style raw --no realistic, 3D, text, background
```

**关键参数:**
- 比例: 4:1
- 风格: 同火人站立
- 配色: 同火人
- PPU: 32

**负面提示词:** 同1.1.3

---

#### 1.1.5 水人 — 跳跃动画帧（3帧）

**中文描述**: 水人跳跃动画，3帧：蓄力下蹲→起跳上升→最高点。头部水滴形态不变，身体做跳跃压缩和伸展。

**英文 Prompt (Midjourney):**
```
cute chibi water element character jump animation spritesheet, 3 frames horizontal strip, crouch jump apex poses, forest fireboy watergirl style, big head 75% of body, water droplet head, radial gradient center #E1F5FE mid #4FC3F7 edge #0277BD, thick black outline 3px, 2D vector art, transparent background --ar 3:1 --style raw --no realistic, 3D, text, background
```

**关键参数:** 比例3:1，配色同水人

**负面提示词:** 同1.1.3

---

#### 1.1.6 火人 — 跳跃动画帧（4帧，含二段跳）

**中文描述**: 火人跳跃动画，4帧：蓄力下蹲→一段跳→二段跳蓄力→二段跳最高点。头部火焰形态不变，身体做跳跃压缩和伸展。

**英文 Prompt (Midjourney):**
```
cute chibi fire element character double jump animation spritesheet, 4 frames horizontal strip, crouch first-jump second-jump-crouch second-jump-apex, forest fireboy watergirl style, big head 75% of body, flame head, radial gradient center #FFE082 mid #FF6F00 edge #BF360C, thick black outline 3px, 2D vector art, transparent background --ar 4:1 --style raw --no realistic, 3D, text, background
```

**关键参数:** 比例4:1，配色同火人

**负面提示词:** 同1.1.3

---

#### 1.1.7 水人 — 头像图标

**中文描述**: 水人头像图标，用于HUD面板。仅头部水滴形态，表情柔和。32×32px等效大小。蓝色径向渐变，粗黑描边。

**英文 Prompt (Midjourney):**
```
cute chibi water droplet character icon, head only, friendly expression, radial gradient center #E1F5FE mid #4FC3F7 edge #0277BD, thick black outline 3px, 2D vector art, game UI icon, transparent background, simple clean design --ar 1:1 --style raw --no realistic, text, background, body, full character
```

**关键参数:**
- 比例: 1:1
- 尺寸: 32×32px（HUD头像）
- 配色: 同水人

**负面提示词:**
```
realistic, text, background, full body, complex details, multiple objects
```

---

#### 1.1.8 火人 — 头像图标

**中文描述**: 火人头像图标，用于HUD面板。仅头部火焰形态，表情活泼。32×32px等效大小。橙红径向渐变，粗黑描边。

**英文 Prompt (Midjourney):**
```
cute chibi flame character icon, head only, lively expression, radial gradient center #FFE082 mid #FF6F00 edge #BF360C, thick black outline 3px, 2D vector art, game UI icon, transparent background, simple clean design --ar 1:1 --style raw --no realistic, text, background, body, full character
```

**关键参数:** 同1.1.7，配色改火人

**负面提示词:** 同1.1.7

---

### 1.2 碎片

---

#### 1.2.1 冰晶碎片

**中文描述**: 蓝白色棱形碎片，半透明冰晶质感，从天空掉落的碎片。棱角分明的水晶造型，内部有冰花纹路，边缘微微发光。配色：冰蓝#B0E0E6为主，白色#F0F8FF高光，深蓝#0277BD暗部。粗黑描边，平涂二分明暗。1:1比例，约1格大小(32px)。

**英文 Prompt (Midjourney):**
```
blue-white prismatic ice crystal shard, translucent ice texture, diamond shape, internal ice flower patterns, glowing edges, flat colors ice blue #B0E0E6 highlight white #F0F8FF shadow dark blue #0277BD, thick black outline, 2-tone cel shading, 2D vector art, game item sprite, transparent background, upper-left light --ar 1:1 --style raw --no realistic, 3D, photograph, text, background
```

**关键参数:**
- 比例: 1:1
- 尺寸: ~32×32px (1格)
- 风格: 半透明冰晶质感、发光边缘
- 配色: #B0E0E6 / #F0F8FF / #0277BD

**负面提示词:**
```
realistic, 3D render, photograph, text, background, fire, orange, red, large object, complex scene
```

---

#### 1.2.2 熔岩碎片

**中文描述**: 橙红色不规则形碎片，炽热岩浆质感，从天空掉落。不规则多边形造型，表面有岩浆流动纹路，边缘有火焰跳动效果。配色：橙#FF4500为主，亮黄#FFE082高光，暗红#8B0000暗部。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
orange-red irregular lava shard, molten rock texture, jagged irregular shape, lava flow patterns on surface, flickering flame edges, flat colors orange #FF4500 highlight yellow #FFE082 shadow dark red #8B0000, thick black outline, 2-tone cel shading, 2D vector art, game item sprite, transparent background, upper-left light --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:**
- 比例: 1:1
- 尺寸: ~32×32px
- 风格: 炽热岩浆质感、火焰边缘
- 配色: #FF4500 / #FFE082 / #8B0000

**负面提示词:**
```
realistic, 3D, photograph, text, background, ice, blue, water, large object
```

---

#### 1.2.3 岩石碎片

**中文描述**: 灰色方形碎片，粗糙岩石质感，从天空掉落。方形略带不规则边角的石块造型，表面有岩石纹理和裂纹。配色：灰#808080为主，浅灰#D3D3D3高光，暗灰#404040暗部。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
gray square rock shard, rough stone texture, blocky shape with irregular edges, rock surface cracks and grain, flat colors gray #808080 highlight light gray #D3D3D3 shadow dark gray #404040, thick black outline, 2-tone cel shading, 2D vector art, game item sprite, transparent background, upper-left light --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, ice
```

**关键参数:**
- 比例: 1:1
- 尺寸: ~32×32px
- 风格: 粗糙岩石质感
- 配色: #808080 / #D3D3D3 / #404040

**负面提示词:**
```
realistic, 3D, photograph, text, background, fire, ice, colorful, glowing
```

---

### 1.3 材料（5种砖）

> **设计说明**: 每种砖为1格×1格(32×32px)的方形建筑材料。风格统一为矢量几何+平涂二分明暗+粗黑描边。放置在建筑网格上使用。

---

#### 1.3.1 水砖

**中文描述**: 蓝色冰晶质感的方形砖块，水元素的凝聚态。表面有水波纹和冰晶反光，略微透明感。配色：主色#4FC3F7，高光#E1F5FE，暗部#0277BD。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
blue ice crystal brick, square building block, water element condensed, water ripple texture on surface, ice crystal reflections, slightly translucent, flat colors main blue #4FC3F7 highlight #E1F5FE shadow #0277BD, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:**
- 比例: 1:1 | 尺寸: 32×32px | 配色: #4FC3F7/#E1F5FE/#0277BD

**负面提示词:**
```
realistic, 3D, photograph, text, background, fire, orange, red, irregular shape, large
```

---

#### 1.3.2 火砖

**中文描述**: 红色炽热质感的方形砖块，火元素的凝聚态。表面有火焰纹路和炽热发光效果。配色：主色#FF6F00，高光#FFE082，暗部#BF360C。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
red hot fiery brick, square building block, fire element condensed, flame patterns on surface, glowing hot effect, flat colors main deep orange #FF6F00 highlight light gold #FFE082 shadow dark red-orange #BF360C, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 比例1:1 | 32×32px | #FF6F00/#FFE082/#BF360C

**负面提示词:**
```
realistic, 3D, photograph, text, background, ice, blue, water, irregular shape
```

---

#### 1.3.3 冰砖

**中文描述**: 白色冰霜质感的方形砖块，水元素的高级凝固态。表面有霜花纹理和冰晶结构，较厚实。配色：主色#E0F7FA，高光#FFFFFF，暗部#80DEEA。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
white frost ice brick, square building block, advanced water element frozen solid, frost flower texture on surface, ice crystal structure, thick and solid, flat colors main ice white #E0F7FA highlight pure white #FFFFFF shadow light cyan #80DEEA, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 比例1:1 | 32×32px | #E0F7FA/#FFFFFF/#80DEEA

**负面提示词:**
```
realistic, 3D, photograph, text, background, fire, orange, red, melting, liquid
```

---

#### 1.3.4 岩浆砖

**中文描述**: 橙红色流动质感的方形砖块，火元素的高级凝固态。表面有岩浆流动纹路和裂纹，边缘有炽热发光。配色：主色#FF4500，高光#FFD54F，暗部#8B0000。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
orange-red molten lava brick, square building block, advanced fire element solidified, lava flow patterns on surface, glowing cracks, flat colors main orange-red #FF4500 highlight warm yellow #FFD54F shadow dark red #8B0000, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 比例1:1 | 32×32px | #FF4500/#FFD54F/#8B0000

**负面提示词:**
```
realistic, 3D, photograph, text, background, ice, blue, water, solid rock gray
```

---

#### 1.3.5 石砖

**中文描述**: 灰色厚重质感的方形砖块，混合元素的稳定态。表面有岩石纹理和裂纹，厚实稳重。配色：主色#757575，高光#BDBDBD，暗部#424242。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
gray heavy stone brick, square building block, mixed element stable form, rock texture with cracks on surface, thick and sturdy, flat colors main gray #757575 highlight light gray #BDBDBD shadow dark gray #424242, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, ice, colorful
```

**关键参数:** 比例1:1 | 32×32px | #757575/#BDBDBD/#424242

**负面提示词:**
```
realistic, 3D, photograph, text, background, colorful, glowing, fire, ice
```

---

#### 1.3.6 温砖（特殊材料）

**中文描述**: 冰火融合的方形砖块，表面左半为冰晶蓝(#4FC3F7)右半为火焰橙(#FF6F00)，中间有融合渐变线。代表冰火合作的最高奖励。粗黑描边3px，圆角处理。

**英文 Prompt (Midjourney):**
```
special fusion brick, half ice half fire, left side ice blue #4FC3F7 right side fire orange #FF6F00, fusion gradient line in center, ice crystal texture on left flame texture on right, square building block, thick black outline 3px, rounded corners, 2D vector art, game building material sprite, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background
```

**关键参数:** 比例1:1 | 32×32px | #4FC3F7 + #FF6F00

**负面提示词:**
```
realistic, 3D, photograph, text, background, uniform color, monochrome
```

---

### 1.4 建筑（5种类型）

> **设计说明**: 建筑由砖块堆叠组成。每种建筑有不同形状。PPU=32，每格32px。

---

#### 1.4.1 防火墙（竖直冰晶墙体）

**中文描述**: 竖直冰晶墙体建筑，由冰砖堆叠成的竖直墙壁。蓝色光芒表示降温效果。墙面有冰晶反光和霜花纹理。配色：主色#B0E0E6，光芒#4FC3F7。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
vertical ice crystal wall, stacked ice bricks, building structure for fire defense, blue glow effect indicating cooling, ice crystal reflections and frost patterns on surface, flat colors main ice blue #B0E0E6 glow blue #4FC3F7 shadow #0277BD, thick black outline, 2-tone cel shading, 2D vector art, game building sprite, transparent background, tall vertical structure --ar 1:2 --style raw --no realistic, 3D, photograph, text, background, fire, horizontal
```

**关键参数:**
- 比例: 1:2（竖直高墙）
- 尺寸: 32×64px（1×2格）
- 风格: 冰晶质感、蓝色光芒
- 配色: #B0E0E6 / #4FC3F7 / #0277BD

**负面提示词:**
```
realistic, 3D, photograph, text, background, fire, orange, horizontal, wide
```

---

#### 1.4.2 防洪堤（水平炽热屏障）

**中文描述**: 水平炽热屏障建筑，由火砖/岩浆砖堆叠成的水平堤坝。红色光芒表示蒸发效果。表面有热浪纹路。配色：主色#FF6F00，光芒#FF4500。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
horizontal fiery barrier, stacked fire bricks, building structure for flood defense, red glow effect indicating evaporation, heat wave patterns on surface, flat colors main deep orange #FF6F00 glow orange-red #FF4500 shadow #BF360C, thick black outline, 2-tone cel shading, 2D vector art, game building sprite, transparent background, wide horizontal structure --ar 2:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue, vertical, tall
```

**关键参数:**
- 比例: 2:1（水平宽堤）
- 尺寸: 64×32px（2×1格）
- 风格: 炽热质感、红色光芒
- 配色: #FF6F00 / #FF4500 / #BF360C

**负面提示词:**
```
realistic, 3D, photograph, text, background, ice, blue, vertical, tall
```

---

#### 1.4.3 加固塔（金字塔石结构）

**中文描述**: 金字塔形石结构建筑，由石砖堆叠成阶梯金字塔。表示抗震稳定性。灰色厚重质感，表面有岩石纹理。配色：主色#757575。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
pyramid shaped stone tower, stepped pyramid structure, stacked stone bricks, building structure for earthquake resistance, gray heavy stone texture, rock patterns on surface, flat colors main gray #757575 highlight #BDBDBD shadow #424242, thick black outline, 2-tone cel shading, 2D vector art, game building sprite, transparent background, pyramid silhouette --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, ice, colorful
```

**关键参数:**
- 比例: 1:1（正方形金字塔）
- 尺寸: 64×64px（2×2格）
- 风格: 石质厚重、金字塔结构
- 配色: #757575 / #BDBDBD / #424242

**负面提示词:**
```
realistic, 3D, photograph, text, background, fire, ice, colorful, thin, narrow
```

---

#### 1.4.4 避难所（封闭空间有顶）

**中文描述**: 封闭空间建筑，有屋顶的方形庇护所。由石砖/冰砖搭建的封闭结构，有入口洞口。保护玩家免受风雪和陨石伤害。配色：主色#757575（石砖）或#E0F7FA（冰砖）。粗黑描边。

**英文 Prompt (Midjourney):**
```
enclosed shelter building, square structure with roof, stone brick walls, small entrance opening, building structure for storm and meteor defense, protective dome shape, flat colors main gray #757575 highlight #BDBDBD shadow #424242, thick black outline, 2-tone cel shading, 2D vector art, game building sprite, transparent background, solid enclosed structure --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, open structure, no roof
```

**关键参数:**
- 比例: 1:1
- 尺寸: 64×64px（2×2格）
- 风格: 封闭、有顶、有入口
- 配色: #757575 / #BDBDBD / #424242

**负面提示词:**
```
realistic, 3D, photograph, text, background, open top, no walls, destroyed, ruins
```

---

#### 1.4.5 导流板（倾斜石板）

**中文描述**: 倾斜石板建筑，斜向放置的石板结构，用于偏转陨石轨迹。石板表面有岩石纹理。配色：主色#757575。粗黑描边，平涂二分明暗。

**英文 Prompt (Midjourney):**
```
angled stone deflector plate, tilted stone slab structure, building structure for meteor deflection, rock texture on surface, diagonal placement, flat colors main gray #757575 highlight #BDBDBD shadow #424242, thick black outline, 2-tone cel shading, 2D vector art, game building sprite, transparent background, diagonal angled structure --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, vertical, horizontal, flat
```

**关键参数:**
- 比例: 1:1
- 尺寸: 64×64px（2×2格）
- 风格: 倾斜石板、对角线
- 配色: #757575 / #BDBDBD / #424242

**负面提示词:**
```
realistic, 3D, photograph, text, background, perfectly vertical, perfectly horizontal, flat
```

---

### 1.5 基础背景

---

#### 1.5.1 基础天空背景（通用）

**中文描述**: 游戏基础天空背景，深青灰色调。天空从深靛蓝#1A237E渐变到靛蓝#283593。底部深青灰#263238。迷雾废墟氛围，远处有模糊的废墟轮廓。2D横板游戏背景，无前景角色。

**英文 Prompt (Midjourney):**
```
2D side-scroller game sky background, dark teal-gray atmosphere, gradient sky from deep indigo #1A237E at top to indigo #283593 at middle, dark teal-gray #263238 ground, misty ruins silhouette in distance, foggy atmosphere, vector art style, flat colors, no characters, game background layer, atmospheric depth --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, foreground objects, UI
```

**关键参数:**
- 比例: 2:1（宽背景）
- 尺寸: 1280×640px（40×20格）
- 风格: 迷雾废墟氛围、渐变天空
- 配色: #1A237E → #283593 → #263238

**负面提示词:**
```
realistic, 3D render, photograph, text, characters, foreground objects, UI elements, bright colors, sunny, cheerful
```

---

#### 1.5.2 基础地面

**中文描述**: 游戏基础地面层，深青灰色#263238的平坦地面。表面有轻微的石质纹理，无明显障碍物。左上光源，右下微阴影。2D横板游戏地面层。

**英文 Prompt (Midjourney):**
```
2D game ground tile, flat dark teal-gray #263238, subtle stone texture, no obstacles, seamless tiling, upper-left lighting with subtle shadow, vector art style, flat colors, game ground layer, top surface visible --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, obstacles, plants, decorations
```

**关键参数:**
- 比例: 2:1
- 尺寸: 1280×64px（40×2格，地面高度2格）
- 配色: #263238

**负面提示词:**
```
realistic, 3D, photograph, text, characters, obstacles, decorations, bright colors, grass, dirt
```

---

## 三、Phase 2 — 灾害特效

> **设计说明**: 35种灾难按6大类分组。每种灾难为2D粒子特效风格，保持卡通化自然灾害美学。16:9宽幅画面，适合横板游戏。灾难特效以纯视觉表现为主，不包含角色。所有灾难特效背景为半透明或透明，便于叠加到游戏画面上。

### 2.1 元素类灾难（8种）

---

#### E1 — 熔岩潮（火山环境）

**中文描述**: 地面裂缝涌出岩浆，缓慢蔓延的灾害特效。地面有多条裂缝，橙红色岩浆从裂缝中涌出并蔓延。火星飘散，空气灼热。配色：暗红#8B0000、橙#FF4500、焦黑#1A1A1A。卡通风格粒子特效。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, lava surging from ground cracks, molten lava spreading slowly across ground, multiple ground fissures with glowing orange-red lava, sparks floating, scorching atmosphere, flat colors dark red #8B0000 orange #FF4500 charred black #1A1A1A, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI
```

**关键参数:**
- 比例: 16:9 | 配色: #8B0000/#FF4500/#1A1A1A
- 风格: 地面裂缝+岩浆蔓延+火星

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, ice, blue, water, snow
```

---

#### E2 — 冰封领域（暴风雪环境）

**中文描述**: 区域快速结冰，地面变滑的灾害特效。冰面从中心向外蔓延扩散，地面覆盖冰层，霜花绽放。冰蓝色调，寒气逼人。配色：冰蓝#B0E0E6、白#F0F8FF、深蓝#191970。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, area rapidly freezing, ice spreading from center outward, ground covered with ice layer, frost flowers blooming, cold blue atmosphere, flat colors ice blue #B0E0E6 white #F0F8FF deep blue #191970, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, orange, red
```

**关键参数:** 比例16:9 | #B0E0E6/#F0F8FF/#191970 | 冰面蔓延+霜花

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, orange, red, lava`

---

#### E3 — 元素风暴（洪水环境）

**中文描述**: 水火元素在空中碰撞，蒸汽爆炸的灾害特效。蓝色水元素和橙红火元素在空中碰撞产生白色蒸汽爆炸，元素碎片飞舞。混乱、剧烈。配色：紫#9370DB、蓝#4169E1、红#DC143C。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, water and fire elements colliding in mid-air, steam explosions, blue water element clashing with orange-red fire element, white steam burst, element fragments flying, chaotic violent atmosphere, flat colors purple #9370DB blue #4169E1 red #DC143C, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI
```

**关键参数:** 比例16:9 | #9370DB/#4169E1/#DC143C | 水火碰撞+蒸汽爆炸

**负面提示词:** `realistic, 3D, photograph, text, characters, calm, peaceful`

---

#### E4 — 蒸汽领域（火山环境）

**中文描述**: 大量蒸汽弥漫，能见度降低的灾害特效。白色蒸汽从地图中央向四周扩散，雾气浓密。配色：白#F5F5F5、灰#9E9E9E、淡橙#FFCC80。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, massive steam spreading from center, thick white vapor clouds expanding outward, visibility reducing fog, hazy atmosphere, flat colors white #F5F5F5 gray #9E9E9E light orange #FFCC80, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, lava
```

**关键参数:** 比例16:9 | #F5F5F5/#9E9E9E/#FFCC80 | 蒸汽扩散

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, lava, ice`

---

#### E5 — 沸腾领域（火山环境）

**中文描述**: 地面水坑沸腾，蒸汽柱弹飞碎片的灾害特效。地面水坑冒泡沸腾，蒸汽柱向上喷射。配色：暗红#8B0000、橙#FF4500、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, ground puddles boiling, bubbling water pools, steam geysers shooting upward, splashing bubbles, scorching ground, flat colors dark red #8B0000 orange #FF4500 white steam #F5F5F5, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, ice, snow
```

**关键参数:** 比例16:9 | #8B0000/#FF4500/#F5F5F5 | 沸水+蒸汽柱

**负面提示词:** `realistic, 3D, photograph, text, characters, ice, snow, blue`

---

#### E6 — 火焰龙卷（火山环境）

**中文描述**: 旋转火柱随机移动，路径上碎片被点燃的灾害特效。垂直旋转的火焰龙卷风，火焰螺旋上升。配色：橙#FF4500、红#8B0000、焦黑#1A1A1A。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, rotating fire tornado, vertical spinning flame pillar, spiraling flames rising upward, fire vortex moving across ground, embers spiraling, flat colors orange #FF4500 dark red #8B0000 charred black #1A1A1A, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, water, ice
```

**关键参数:** 比例16:9 | #FF4500/#8B0000/#1A1A1A | 火焰龙卷旋涡

**负面提示词:** `realistic, 3D, photograph, text, characters, water, ice, blue`

---

#### E7 — 水晶风暴（暴风雪环境）

**中文描述**: 冰晶从四面八方飞来，击中减速的灾害特效。大量冰晶碎片如飞刀般从各方向飞来，冰晶闪光。配色：冰蓝#B0E0E6、白#F0F8FF、深蓝#191970。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, ice crystals flying from all directions, sharp crystal shards flying like daggers, crystal sparkles, freezing wind, flat colors ice blue #B0E0E6 white #F0F8FF deep blue #191970, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, orange
```

**关键参数:** 比例16:9 | #B0E0E6/#F0F8FF/#191970 | 冰晶飞行物

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, orange, red`

---

#### E8 — 元素漩涡（火山环境）

**中文描述**: 水火元素形成漩涡，吸入碎片和角色的灾害特效。蓝色水元素和橙红火元素形成双色螺旋漩涡，中心有吸入效果。配色：蓝#4FC3F7、橙#FF6F00、紫#9370DB。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, water-fire elemental vortex, dual-color spiral vortex blue water and orange fire swirling, suction effect at center, element fragments being pulled in, flat colors blue #4FC3F7 orange #FF6F00 purple #9370DB, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI
```

**关键参数:** 比例16:9 | #4FC3F7/#FF6F00/#9370DB | 双色螺旋漩涡

**负面提示词:** `realistic, 3D, photograph, text, characters, calm, static`

---

### 2.2 环境类灾难（6种）

---

#### V1 — 酸雨腐蚀（洪水环境）

**中文描述**: 酸雨降落，持续腐蚀建筑耐久的灾害特效。绿色酸雨从天空降落，雨滴落地后有腐蚀冒烟效果。配色：暗绿#2F4F4F、灰#708090、黄绿#9ACD32。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, acid rain falling from sky, green corrosive raindrops, smoke rising from ground impact, corroding atmosphere, flat colors dark green #2F4F4F gray #708090 yellow-green #9ACD32, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, ice
```

**关键参数:** 比例16:9 | #2F4F4F/#708090/#9ACD32 | 酸雨+腐蚀冒烟

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, ice, blue rain`

---

#### V2 — 沙尘暴（暴风雪环境）

**中文描述**: 黄沙弥漫，能见度极低的灾害特效。大量黄沙从边缘席卷，沙尘遮蔽视线。配色：沙黄#DAA520、暗棕#5D4037、灰#808080。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, sandstorm sweeping in, thick yellow sand swirling, dust obscuring visibility, sandy wind blowing, flat colors sand yellow #DAA520 dark brown #5D4037 gray #808080, thick black outlines, cel-shaded particles, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, water, ice, fire
```

**关键参数:** 比例16:9 | #DAA520/#5D4037/#808080 | 黄沙席卷

**负面提示词:** `realistic, 3D, photograph, text, characters, water, ice, fire, blue`

---

#### V3 — 植物疯长（洪水环境）

**中文描述**: 藤蔓从地面生长，缠绕角色和建筑的灾害特效。绿色藤蔓从地面快速向上生长蔓延，叶片绽放。配色：绿#228B22、暗绿#2F4F4F、棕#5D4037。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, vines rapidly growing from ground, green tendrils spreading upward, leaves blooming, plants engulfing area, flat colors green #228B22 dark green #2F4F4F brown #5D4037, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, ice
```

**关键参数:** 比例16:9 | #228B22/#2F4F4F/#5D4037 | 藤蔓蔓延

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, ice, dead plants`

---

#### V4 — 地面塌陷（地震环境）

**中文描述**: 地面随机出现坑洞，碎片掉入的灾害特效。地面开裂形成坑洞，碎石掉落。配色：土黄#DAA520、灰#808080、暗棕#5D4037。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, ground collapsing into sinkholes, ground cracks forming pits, debris falling into holes, unstable terrain, flat colors earth yellow #DAA520 gray #808080 dark brown #5D4037, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, ice, water
```

**关键参数:** 比例16:9 | #DAA520/#808080/#5D4037 | 地面坑洞+碎石

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, ice, water, plants`

---

#### V5 — 雾海入侵（火山环境）

**中文描述**: 浓雾从边缘涌入，视野缩小的灾害特效。浓密雾气从画面边缘向中心蔓延，能见度逐渐降低。配色：灰#9E9E9E、白#F5F5F5、淡绿#C8E6C9。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, thick fog rolling in from edges, dense mist spreading inward, visibility decreasing, foggy layers, flat colors gray #9E9E9E white #F5F5F5 light green #C8E6C9, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, clear sky
```

**关键参数:** 比例16:9 | #9E9E9E/#F5F5F5/#C8E6C9 | 浓雾蔓延

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, clear sky, sunny`

---

#### V6 — 酸雾弥漫（火山环境）

**中文描述**: 有毒雾气弥漫，角色持续掉血的灾害特效。绿色有毒雾气覆盖全图，雾气有腐蚀感。配色：暗绿#2F4F4F、灰#708090、绿黄#ADFF2F。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, toxic acid mist covering entire area, green poisonous fog, corrosive vapor everywhere, hazardous atmosphere, flat colors dark green #2F4F4F gray #708090 green-yellow #ADFF2F, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, clean air
```

**关键参数:** 比例16:9 | #2F4F4F/#708090/#ADFF2F | 毒雾弥漫

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, clean air, blue`

---

### 2.3 时空类灾难（5种）

---

#### T1 — 时间裂隙（陨石环境）

**中文描述**: 时空裂隙内时间流速不同的灾害特效。紫色时空裂缝出现在空中，裂隙周围有时钟扭曲效果。配色：紫#9370DB、黑#1A1A1A、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, time rifts in space, purple temporal cracks in air, clock distortion effect around rifts, time warp visual, flat colors purple #9370DB black #1A1A1A white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#1A1A1A/#F5F5F5 | 时空裂缝+时钟扭曲

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, natural`

---

#### T2 — 空间折叠（地震环境）

**中文描述**: 地图区域折叠，距离关系混乱的灾害特效。空间扭曲变形，区域像折纸一样折叠，视觉错位。配色：紫#9370DB、黑#1A1A1A、灰#9E9E9E。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, space folding distortion, origami-like spatial warping, areas bending and folding, visual displacement, flat colors purple #9370DB black #1A1A1A gray #9E9E9E, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#1A1A1A/#9E9E9E | 空间折叠扭曲

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, normal space`

---

#### T3 — 重力反转（陨石环境）

**中文描述**: 重力方向改变，角色和碎片被拉向天空的灾害特效。向上箭头重力指示，物体向上飘浮。配色：紫#9370DB、黑#1A1A1A、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, gravity reversal, upward gravity arrows, objects floating upward, anti-gravity visual, falling upward effect, flat colors purple #9370DB black #1A1A1A white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#1A1A1A/#F5F5F5 | 重力反转+向上箭头

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, normal gravity`

---

#### T4 — 镜像领域（陨石环境）

**中文描述**: 地图出现镜像，视觉位置与实际不符的灾害特效。镜面反射效果，空间像镜子一样翻转。配色：紫#9370DB、银#C0C0C0、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, mirror dimension, reflective surfaces in space, mirror image flip effect, glass-like distortion, flat colors purple #9370DB silver #C0C0C0 white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#C0C0C0/#F5F5F5 | 镜面反射

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, normal space`

---

#### T5 — 时间停滞（陨石环境）

**中文描述**: 某些区域时间停止，碎片和角色冻结的灾害特效。灰色静止区域，区域内一切冻结，边缘有时间停滞波纹。配色：灰#9E9E9E、黑#1A1A1A、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, time stop zones, gray frozen areas, everything stationary within zones, temporal ripple edges,停滞 zone boundaries, flat colors gray #9E9E9E black #1A1A1A white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water, colorful
```

**关键参数:** 比例16:9 | #9E9E9E/#1A1A1A/#F5F5F5 | 静止区域+时间波纹

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, colorful, motion`

---

### 2.4 感知类灾难（5种）

---

#### S1 — 幻象迷雾（暴风雪环境）

**中文描述**: 产生虚假碎片幻象，捡不到的灾害特效。半透明虚假碎片在雾中浮现，碎片有闪烁不稳定的虚影感。配色：灰#9E9E9E、白#F5F5F5、淡紫#E1BEE7。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, illusion fog, semi-transparent fake crystal shards floating in mist, flickering phantom items, hallucination visual, flat colors gray #9E9E9E white #F5F5F5 light purple #E1BEE7, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #9E9E9E/#F5F5F5/#E1BEE7 | 虚假碎片+幻影

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, solid objects, clear`

---

#### S2 — 声波干扰（地震环境）

**中文描述**: 无法听到声音提示的灾害特效。声波消失图标，静音符号在空中浮动。配色：灰#9E9E9E、黑#1A1A1A、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, sound wave interference, muted sound icons floating, silence symbols in air, sound wave breaking visual, flat colors gray #9E9E9E black #1A1A1A white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water, colorful
```

**关键参数:** 比例16:9 | #9E9E9E/#1A1A1A/#F5F5F5 | 声波消失+静音符号

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, colorful, sound waves visible`

---

#### S3 — 意识混乱（陨石环境）

**中文描述**: 颠倒移动方向，左变右上变下的灾害特效。方向箭头反转图标在空中显示，左右颠倒的视觉感。配色：紫#9370DB、黑#1A1A1A、白#F5F5F5。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, consciousness confusion, reversed direction arrows floating, inverted control icons, left-right flip visual, dizzy effect, flat colors purple #9370DB black #1A1A1A white #F5F5F5, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#1A1A1A/#F5F5F5 | 方向反转图标

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, normal arrows`

---

#### S4 — 光线扭曲（暴风雪环境）

**中文描述**: 碎片视觉位置与实际位置不一致的灾害特效。光线折射扭曲，画面有热浪般的扭曲效果。配色：淡蓝#B0E0E6、白#F5F5F5、淡紫#E1BEE7。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, light distortion, light refraction warping, heat-wave distortion effect, visual position shifting, wavy light bending, flat colors light blue #B0E0E6 white #F5F5F5 light purple #E1BEE7, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #B0E0E6/#F5F5F5/#E1BEE7 | 光线折射扭曲

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, clear vision, normal`

---

#### S5 — 色彩反转（陨石环境）

**中文描述**: 所有颜色反转，水火外观互换的灾害特效。画面色彩反转滤镜效果，蓝色变橙色，橙色变蓝色。配色：反色#FF6F00↔#4FC3F7。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, color inversion, inverted color filter, blue becomes orange orange becomes blue, negative color effect, color swap visual, flat colors inverted blue orange #FF6F00 #4FC3F7, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, normal colors
```

**关键参数:** 比例16:9 | 反色#FF6F00↔#4FC3F7 | 色彩反转滤镜

**负面提示词:** `realistic, 3D, photograph, text, characters, normal colors, natural`

---

### 2.5 物理类灾难（5种）

---

#### P1 — 磁力吸引（洪水环境）

**中文描述**: 碎片被吸向地图边缘的灾害特效。磁力箭头指向边缘，碎片向边缘偏移的轨迹线。配色：蓝#00008B、灰#708090、暗绿#2F4F4F。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, magnetic attraction pulling fragments to edges, magnetic force arrows pointing outward, debris trajectory lines bending toward edges, magnetic field visual, flat colors dark blue #00008B gray #708090 dark green #2F4F4F, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #00008B/#708090/#2F4F4F | 磁力箭头+轨迹偏移

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, inward attraction`

---

#### P2 — 冲击波（地震环境）

**中文描述**: 周期性冲击波推开角色和碎片的灾害特效。环形冲击波从中心向外扩散，地面震动效果。配色：土黄#DAA520、灰#808080、暗棕#5D4037。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, shockwave rings expanding from center, concentric force rings pushing outward, ground shaking impact, dust kicking up, flat colors earth yellow #DAA520 gray #808080 dark brown #5D4037, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #DAA520/#808080/#5D4037 | 环形冲击波

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, inward waves`

---

#### P3 — 漩涡吸引（洪水环境）

**中文描述**: 地面漩涡吸入角色的灾害特效。地面水面形成漩涡，螺旋吸入效果。配色：深蓝#00008B、灰#708090、暗绿#2F4F4F。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, ground whirlpool sucking inward, water vortex on ground surface, spiral suction effect, debris being pulled into center, flat colors dark blue #00008B gray #708090 dark green #2F4F4F, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #00008B/#708090/#2F4F4F | 地面漩涡

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, outward push`

---

#### P4 — 地震波（地震环境）

**中文描述**: 地面产生波浪，角色上下起伏的灾害特效。地面如波浪般起伏，地面波形扭曲。配色：土黄#DAA520、灰#808080、暗棕#5D4037。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, ground wave undulation, earth surface rippling like waves, ground buckling up and down, seismic ripple, flat colors earth yellow #DAA520 gray #808080 dark brown #5D4037, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #DAA520/#808080/#5D4037 | 地面波浪起伏

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, flat ground`

---

#### P5 — 风暴眼（暴风雪环境）

**中文描述**: 中心安全，边缘危险，区域不断移动的灾害特效。中心安全圈（绿色光柱边界），边缘有风暴效果。配色：冰蓝#B0E0E6、白#F0F8FF、绿#50FF64。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, storm eye, safe zone center with green light pillar boundary, storm raging at edges, safe circle moving, calm center turbulent edges, flat colors ice blue #B0E0E6 white #F0F8FF green #50FF64, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #B0E0E6/#F0F8FF/#50FF64 | 安全圈+绿色光柱边界

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, uniform storm`

---

### 2.6 机制类灾难（6种）

---

#### M1 — 元素枯竭（洪水环境）

**中文描述**: 合成所需碎片数量翻倍的灾害特效。蓝色能量衰减效果，碎片光芒减弱。配色：深蓝#00008B、灰#708090、暗绿#2F4F4F。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, elemental depletion, blue energy fading, fragment glow dimming, energy drain visual, withering element particles, flat colors dark blue #00008B gray #708090 dark green #2F4F4F, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire
```

**关键参数:** 比例16:9 | #00008B/#708090/#2F4F4F | 能量衰减+碎片暗淡

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, bright energy, glowing`

---

#### M2 — 共振过载（地震环境）

**中文描述**: 建筑耐久下降速度翻倍的灾害特效。建筑震动效果，共振波纹从建筑发出。配色：土黄#DAA520、灰#808080、暗棕#5D4037。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, resonance overload, building vibration visual, resonance ripples emanating from structures, structural stress visual, shaking effect, flat colors earth yellow #DAA520 gray #808080 dark brown #5D4037, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #DAA520/#808080/#5D4037 | 建筑震动+共振波纹

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, stable, calm`

---

#### M3 — 能量反噬（火山环境）

**中文描述**: 使用技能消耗HP而非冷却的灾害特效。红色能量波动，技能图标上有红色反噬效果。配色：暗红#8B0000、橙#FF4500、焦黑#1A1A1A。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, energy backlash, red energy waves pulsing, skill reversal visual, harmful energy overflow, red corruption effect, flat colors dark red #8B0000 orange #FF4500 charred black #1A1A1A, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, ice, blue
```

**关键参数:** 比例16:9 | #8B0000/#FF4500/#1A1A1A | 红色能量波动

**负面提示词:** `realistic, 3D, photograph, text, characters, ice, blue, calm energy`

---

#### M4 — 预言干扰（陨石环境）

**中文描述**: 蓝图在建造中改变的灾害特效。蓝图闪烁变化效果，蓝图图标交替变换。配色：紫#9370DB、黑#1A1A1A、绿#50FF64。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, prophecy interference, blueprint flickering and changing, blueprint icons swapping, green blueprint outline pulsing, glitch effect on plans, flat colors purple #9370DB black #1A1A1A green #50FF64, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #9370DB/#1A1A1A/#50FF64 | 蓝图闪烁变换

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, stable blueprint`

---

#### M5 — 庇护削弱（地震环境）

**中文描述**: 能量恢复速度减半，消耗速度翻倍的灾害特效。能量条闪烁，恢复减慢消耗加快的视觉感。配色：土黄#DAA520、灰#808080、红#E53935。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, shelter weakening, energy bar flickering, energy drain acceleration, shelter field destabilizing, weakening aura, flat colors earth yellow #DAA520 gray #808080 red #E53935, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water
```

**关键参数:** 比例16:9 | #DAA520/#808080/#E53935 | 能量条闪烁+不稳定场

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, water, stable energy`

---

#### M6 — 碎片逃逸（暴风雪环境）

**中文描述**: 碎片落地后会弹跳逃跑，需要追捕的灾害特效。碎片弹跳轨迹，冰花溅射效果。配色：冰蓝#B0E0E6、白#F0F8FF、深蓝#191970。

**英文 Prompt (Midjourney):**
```
2D cartoon disaster effect, fragment escaping, shards bouncing away on landing, ice splashing effect, slippery bouncing fragments, chase visual, flat colors ice blue #B0E0E6 white #F0F8FF deep blue #191970, thick black outlines, cel-shaded, game VFX sprite, transparent background, side-scroller view --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, orange
```

**关键参数:** 比例16:9 | #B0E0E6/#F0F8FF/#191970 | 碎片弹跳+冰花溅射

**负面提示词:** `realistic, 3D, photograph, text, characters, fire, orange, stable fragments`

---

## 四、Phase 3 — UI资源

### 3.1 HUD面板

---

#### 3.1.1 水人HUD面板背景

**中文描述**: 水人信息面板背景，半透明深色底rgba(20,20,30,0.7)，尺寸200×80px。左上角放置，圆角矩形，左侧有蓝色装饰条。简洁矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game HUD panel background, semi-transparent dark rectangle, dark navy rgba(20,20,30,0.7), rounded corners, blue accent stripe on left side, blue #4FC3F7 accent, vector art style, flat design, clean minimal, no text, game UI element, transparent background --ar 5:2 --style raw --no realistic, 3D, photograph, text, characters, complex decorations
```

**关键参数:**
- 比例: 5:2（200×80px）
- 配色: rgba(20,20,30,0.7) + #4FC3F7装饰
- 风格: 半透明、圆角、简洁

**负面提示词:**
```
realistic, 3D, photograph, text, characters, complex decorations, bright colors, ornate
```

---

#### 3.1.2 火人HUD面板背景

**中文描述**: 火人信息面板背景，半透明深色底rgba(20,20,30,0.7)，尺寸200×80px。右上角放置，圆角矩形，右侧有橙色装饰条。简洁矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game HUD panel background, semi-transparent dark rectangle, dark navy rgba(20,20,30,0.7), rounded corners, orange accent stripe on right side, orange #FF6F00 accent, vector art style, flat design, clean minimal, no text, game UI element, transparent background --ar 5:2 --style raw --no realistic, 3D, photograph, text, characters, complex decorations
```

**关键参数:** 同3.1.1，装饰色改#FF6F00，装饰条在右侧

**负面提示词:** 同3.1.1

---

#### 3.1.3 HP血条（通用）

**中文描述**: HP血条填充图，宽180px高12px。红色填充#E53935，带圆角。分为正常(红)和低血量警告(红+脉冲)两种状态。矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game HP health bar fill, horizontal bar, red fill #E53935, rounded corners, flat vector art style, clean, game UI element, transparent background, no text, 2-tone shading with darker red shadow --ar 15:1 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**关键参数:**
- 比例: 15:1（180×12px）
- 配色: #E53935
- 风格: 红色填充、圆角

**负面提示词:**
```
realistic, 3D, photograph, text, characters, blue, green, gradient, complex
```

---

#### 3.1.4 能量条 — 水人版

**中文描述**: 水人能量条填充图，宽180px高8px。蓝色填充#4FC3F7，带圆角。矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game energy bar fill, horizontal bar, blue fill #4FC3F7, rounded corners, flat vector art style, clean, game UI element, transparent background, no text --ar 22:1 --style raw --no realistic, 3D, photograph, text, characters, complex, red, orange
```

**关键参数:**
- 比例: 22:1（180×8px）
- 配色: #4FC3F7

**负面提示词:**
```
realistic, 3D, photograph, text, characters, red, orange, gradient, complex
```

---

#### 3.1.5 能量条 — 火人版

**中文描述**: 火人能量条填充图，宽180px高8px。橙色填充#FF9800，带圆角。矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game energy bar fill, horizontal bar, orange fill #FF9800, rounded corners, flat vector art style, clean, game UI element, transparent background, no text --ar 22:1 --style raw --no realistic, 3D, photograph, text, characters, complex, blue, green
```

**关键参数:** 同3.1.4，配色改#FF9800

**负面提示词:** 同3.1.4，改blue/green

---

#### 3.1.6 阶段进度条

**中文描述**: 底部中央阶段进度条背景，宽400px高6px。7段分段，各段颜色：灰(预告)/金(收集)/橙(灾告)/绿(建造)/红(冲击)/蓝(修整)/紫(升级)。矢量风格。

**英文 Prompt (Midjourney):**
```
game phase progress bar, 7-segment horizontal bar, colors gray gold orange green red blue purple, each segment different color, flat vector art style, game UI element, transparent background, no text, clean minimal --ar 66:1 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**关键参数:**
- 比例: 66:1（400×6px）
- 配色: 灰/金/橙/绿/红/蓝/紫 七段

**负面提示词:**
```
realistic, 3D, photograph, text, characters, single color, gradient
```

---

#### 3.1.7 碎片携带图标

**中文描述**: 碎片携带显示用的3种小图标：蓝色圆点(冰晶碎片)、橙色圆点(熔岩碎片)、金色圆点(温砖)。每种16×16px。粗黑描边，矢量风格。

**英文 Prompt (Midjourney):**
```
3 game UI fragment icons in a row, small circular dots, blue dot #4FC3F7 orange dot #FF6F00 gold dot #FFD700, thick black outline, flat vector art style, game UI element, transparent background, no text --ar 3:1 --style raw --no realistic, 3D, photograph, text, complex
```

**关键参数:**
- 比例: 3:1（三个图标并排）
- 尺寸: 每个16×16px
- 配色: #4FC3F7 / #FF6F00 / #FFD700

**负面提示词:**
```
realistic, 3D, photograph, text, large, complex, detailed
```

---

### 3.2 技能图标（28个）

> **设计说明**: 每个技能图标48×48px。风格统一：矢量几何、粗黑描边、平涂二分明暗、透明背景。水人技能以蓝色系为主，火人技能以橙红色系为主。稀有度通过边框颜色区分：普通=白色边框，稀有=蓝色边框，史诗=紫色边框。

---

#### 3.2.1 水人E技能图标（8个）

| # | 技能名 | 稀有度 | 中文描述概要 |
|---|--------|--------|-------------|
| 1 | 冰霜冲击 | ★普通 | 蓝色寒霜波纹向前扩散 |
| 2 | 水流冲击 | ★普通 | 蓝色水流弧线向前冲出 |
| 3 | 冰墙 | ★普通 | 蓝色冰晶墙壁竖立 |
| 4 | 净化 | ★★稀有 | 蓝色净化光环扩散 |
| 5 | 寒霜护盾 | ★★稀有 | 蓝色冰晶六边形护盾 |
| 6 | 冰霜新星 | ★★稀有 | 蓝色冰霜爆发星形 |
| 7 | 绝对零度 | ★★★史诗 | 蓝色全屏冰冻时钟 |
| 8 | 极寒领域 | ★★★史诗 | 蓝色冰封领域圆形场 |

---

**#1 冰霜冲击 ★**

**中文描述**: 技能图标，蓝色寒霜波纹向前扩散的图案。冰蓝色弧形波纹，中心有冰晶符号。配色：#4FC3F7/#E1F5FE/#0277BD。白色边框(普通稀有度)。48×48px。

**英文 Prompt:**
```
game skill icon, blue frost wave spreading forward, ice blue arc wave pattern, ice crystal symbol in center, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 白色边框

**负面提示词:** `realistic, 3D, photograph, text, background, fire, orange, red`

---

**#2 水流冲击 ★**

**中文描述**: 技能图标，蓝色水流弧线向前冲出的图案。水蓝色弧形水流线条，水花飞溅。配色：#4FC3F7/#B3E5FC/#0277BD。白色边框。

**英文 Prompt:**
```
game skill icon, blue water stream rushing forward, water arc line, water splash droplets, flat colors blue #4FC3F7 light blue #B3E5FC dark blue #0277BD, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#B3E5FC/#0277BD | 白色边框

**负面提示词:** 同#1

---

**#3 冰墙 ★**

**中文描述**: 技能图标，蓝色冰晶墙壁竖立的图案。竖直冰晶墙面，有冰花纹路。配色：#B0E0E6/#E1F5FE/#0277BD。白色边框。

**英文 Prompt:**
```
game skill icon, blue ice wall standing vertical, ice crystal wall surface, frost patterns, flat colors ice blue #B0E0E6 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #B0E0E6/#E1F5FE/#0277BD | 白色边框

**负面提示词:** 同#1

---

**#4 净化 ★★**

**中文描述**: 技能图标，蓝色净化光环向外扩散的图案。同心圆光环，中心有净化光芒。配色：#4FC3F7/#E1F5FE/#0277BD。蓝色边框(稀有)。

**英文 Prompt:**
```
game skill icon, blue purify halo expanding outward, concentric ring aura, cleansing light in center, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#5 寒霜护盾 ★★**

**中文描述**: 技能图标，蓝色冰晶六边形护盾图案。六边形护盾，表面有冰晶纹理。配色：#4FC3F7/#E1F5FE/#0277BD。蓝色边框。

**英文 Prompt:**
```
game skill icon, blue frost hexagonal shield, ice crystal shield shape, hexagonal protective barrier, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#6 冰霜新星 ★★**

**中文描述**: 技能图标，蓝色冰霜爆发的星形图案。中心爆发点，向外扩散的冰霜星芒。配色：#4FC3F7/#E1F5FE/#0277BD。蓝色边框。

**英文 Prompt:**
```
game skill icon, blue frost nova burst, star-shaped ice explosion, radiating ice shards from center, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#7 绝对零度 ★★★**

**中文描述**: 技能图标，蓝色全屏冰冻的时钟图案。冰冻时钟表盘，指针冻结，周围有冰晶。配色：#4FC3F7/#E1F5FE/#0277BD。紫色边框(史诗)。

**英文 Prompt:**
```
game skill icon, blue absolute zero freeze clock, frozen clock face, frozen clock hands, ice crystals around, ultimate freeze ability, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 紫色边框

**负面提示词:** 同#1

---

**#8 极寒领域 ★★★**

**中文描述**: 技能图标，蓝色冰封领域的圆形场图案。圆形领域边界，内部冰封效果。配色：#4FC3F7/#E1F5FE/#0277BD。紫色边框。

**英文 Prompt:**
```
game skill icon, blue permafrost domain field, circular area boundary, frozen ground inside domain, ice field zone, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 紫色边框

**负面提示词:** 同#1

---

#### 3.2.2 水人Q技能图标（6个）

| # | 技能名 | 稀有度 | 中文描述概要 |
|---|--------|--------|-------------|
| 9 | 暴风雪 | ★普通 | 蓝色暴风雪覆盖区域 |
| 10 | 海啸 | ★★稀有 | 蓝色巨大海啸波浪 |
| 11 | 冰封领域 | ★★稀有 | 蓝色冰封地面领域 |
| 12 | 寒冰爆发 | ★★稀有 | 蓝色寒冰爆发冲击 |
| 13 | 绝对冰封 | ★★★史诗 | 蓝色全屏冰封棱晶 |
| 14 | 冰河时代 | ★★★史诗 | 蓝色冰河覆盖大地 |

---

**#9 暴风雪 ★**

**中文描述**: 技能图标，蓝色暴风雪覆盖区域的图案。风雪漩涡覆盖方形区域。配色：#4FC3F7/#E1F5FE/#0277BD。白色边框。

**英文 Prompt:**
```
game skill icon, blue blizzard covering area, snowstorm swirl over square zone, wind and snow, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 白色边框

**负面提示词:** 同#1

---

**#10 海啸 ★★**

**中文描述**: 技能图标，蓝色巨大海啸波浪的图案。巨大弧形波浪从左到右席卷。配色：#4FC3F7/#B3E5FC/#0277BD。蓝色边框。

**英文 Prompt:**
```
game skill icon, blue massive tsunami wave, giant arc wave sweeping from left to right, ocean tidal wave, flat colors blue #4FC3F7 light blue #B3E5FC dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#B3E5FC/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#11 冰封领域 ★★**

**中文描述**: 技能图标，蓝色冰封地面领域的图案。地面覆盖冰层，冰晶蔓延。配色：#B0E0E6/#E1F5FE/#0277BD。蓝色边框。

**英文 Prompt:**
```
game skill icon, blue frozen ground domain, ground covered with ice layer, ice crystal spreading, frozen terrain, flat colors ice blue #B0E0E6 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #B0E0E6/#E1F5FE/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#12 寒冰爆发 ★★**

**中文描述**: 技能图标，蓝色寒冰爆发冲击的图案。中心爆发向外扩散冰晶冲击波。配色：#4FC3F7/#E1F5FE/#0277BD。蓝色边框。

**英文 Prompt:**
```
game skill icon, blue cryogenic burst, ice explosion shockwave from center, radiating ice shards, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 蓝色边框

**负面提示词:** 同#1

---

**#13 绝对冰封 ★★★**

**中文描述**: 技能图标，蓝色全屏冰封的棱晶图案。巨大冰晶棱镜覆盖，全屏冰封效果。配色：#4FC3F7/#E1F5FE/#0277BD。紫色边框。

**英文 Prompt:**
```
game skill icon, blue absolute freeze prism, giant ice crystal prism covering, ultimate freeze, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 紫色边框

**负面提示词:** 同#1

---

**#14 冰河时代 ★★★**

**中文描述**: 技能图标，蓝色冰河覆盖大地的图案。冰川覆盖地面，冰层蔓延至全图。配色：#4FC3F7/#E1F5FE/#0277BD。紫色边框。

**英文 Prompt:**
```
game skill icon, blue ice age covering ground, glacier covering terrain, ice layer spreading across land, ultimate ice age, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, fire, orange
```

**关键参数:** 1:1 | 48×48px | #4FC3F7/#E1F5FE/#0277BD | 紫色边框

**负面提示词:** 同#1

---

#### 3.2.3 火人E技能图标（8个）

> **说明**: 与水人E技能结构对称，配色改为火人色系：中心#FFE082 → 中段#FF6F00 → 边缘#BF360C。

| # | 技能名 | 稀有度 | 中文描述概要 |
|---|--------|--------|-------------|
| 15 | 火焰冲击 | ★普通 | 橙红火焰波纹向前扩散 |
| 16 | 火焰箭 | ★普通 | 橙红火焰箭矢飞行 |
| 17 | 热浪 | ★普通 | 橙红热浪弧线扩散 |
| 18 | 点燃 | ★★稀有 | 橙红点燃火焰标记 |
| 19 | 火焰护盾 | ★★稀有 | 橙红火焰六边形护盾 |
| 20 | 火焰新星 | ★★稀有 | 橙红火焰爆发星形 |
| 21 | 焚天灭地 | ★★★史诗 | 橙红全屏燃烧火焰 |
| 22 | 烈焰领域 | ★★★史诗 | 橙红火焰领域圆形场 |

---

**#15 火焰冲击 ★**

**英文 Prompt:**
```
game skill icon, orange-red flame wave spreading forward, fire arc wave pattern, flame symbol in center, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 白色边框

**负面提示词:** `realistic, 3D, photograph, text, background, ice, blue, water`

---

**#16 火焰箭 ★**

**英文 Prompt:**
```
game skill icon, orange-red fire arrow flying, flaming arrow projectile, flame trail behind, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 白色边框

**负面提示词:** 同#15

---

**#17 热浪 ★**

**英文 Prompt:**
```
game skill icon, orange-red heat wave radiating, thermal wave ripples, heat distortion arcs, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 白色边框

**负面提示词:** 同#15

---

**#18 点燃 ★★**

**英文 Prompt:**
```
game skill icon, orange-red ignite flame mark, ignition symbol, small flame with spark, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#19 火焰护盾 ★★**

**英文 Prompt:**
```
game skill icon, orange-red flame hexagonal shield, fire crystal shield shape, hexagonal protective barrier with flames, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#20 火焰新星 ★★**

**英文 Prompt:**
```
game skill icon, orange-red flame nova burst, star-shaped fire explosion, radiating flames from center, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#21 焚天灭地 ★★★**

**英文 Prompt:**
```
game skill icon, orange-red world burn, all-consuming flames, massive fire covering everything, ultimate burn ability, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 紫色边框

**负面提示词:** 同#15

---

**#22 烈焰领域 ★★★**

**英文 Prompt:**
```
game skill icon, orange-red inferno domain field, circular area boundary with flames, fire field zone, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 紫色边框

**负面提示词:** 同#15

---

#### 3.2.4 火人Q技能图标（6个）

| # | 技能名 | 稀有度 | 中文描述概要 |
|---|--------|--------|-------------|
| 23 | 烈焰风暴 | ★普通 | 橙红火焰风暴漩涡 |
| 24 | 熔岩喷发 | ★★稀有 | 橙红地面熔岩喷发柱 |
| 25 | 火焰领域 | ★★稀有 | 橙红火焰领域圆形场 |
| 26 | 火焰爆发 | ★★稀有 | 橙红火焰爆发冲击 |
| 27 | 焚天灭世 | ★★★史诗 | 橙红全屏末日燃烧 |
| 28 | 烈焰地狱 | ★★★史诗 | 橙红烈焰覆盖大地 |

---

**#23 烈焰风暴 ★**

**英文 Prompt:**
```
game skill icon, orange-red firestorm swirl, flame vortex spiral, spinning fire storm, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, white border, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 白色边框

**负面提示词:** 同#15

---

**#24 熔岩喷发 ★★**

**英文 Prompt:**
```
game skill icon, orange-red lava eruption from ground, molten lava column shooting up, ground fissure with lava, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#25 火焰领域 ★★**

**英文 Prompt:**
```
game skill icon, orange-red flame domain field, circular area boundary with fire, fire field zone, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#26 火焰爆发 ★★**

**英文 Prompt:**
```
game skill icon, orange-red flame burst, fire explosion shockwave from center, radiating flames, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, blue border #4FC3F7, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 蓝色边框

**负面提示词:** 同#15

---

**#27 焚天灭世 ★★★**

**英文 Prompt:**
```
game skill icon, orange-red apocalypse burning, world-ending flames, all-consuming inferno, ultimate destruction, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 紫色边框

**负面提示词:** 同#15

---

**#28 烈焰地狱 ★★★**

**英文 Prompt:**
```
game skill icon, orange-red inferno hell covering ground, hellfire spreading across land, ground fully ablaze, ultimate inferno, flat colors deep orange #FF6F00 light gold #FFE082 dark red-orange #BF360C, thick black outline, purple border #9370DB, 2D vector art, 48x48 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, background, ice, blue
```

**关键参数:** 1:1 | 48×48px | #FF6F00/#FFE082/#BF360C | 紫色边框

**负面提示词:** 同#15

---

### 3.3 天赋卡片图标

> **设计说明**: 天赋图标64×64px，用于天赋卡片上半部分。53个天赋分为水人专属(5个独特)、火人专属(5个独特)、水火共用(13个概念，分蓝/橙两版)、共享池(15个独特)。共计约38个独特图标概念。以下按类别提供提示词，水火共用的仅给一版提示词，注明元素色替换规则。

#### 3.3.1 天赋卡片边框

**中文描述**: 天赋卡片边框，3种稀有度。普通=白色边框#FFFFFF，稀有=蓝色边框#4FC3F7，史诗=紫色边框#9370DB。卡片尺寸180×260px，竖向卡片。矢量风格装饰边框。

**英文 Prompt (普通★):**
```
game talent card frame, white border #FFFFFF, vertical card frame, ornamental corners, flat vector art style, game UI element, transparent background, no text, no character inside --ar 3:4 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**英文 Prompt (稀有★★):**
```
game talent card frame, blue border #4FC3F7, vertical card frame, ornamental corners, flat vector art style, game UI element, transparent background, no text, no character inside --ar 3:4 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**英文 Prompt (史诗★★★):**
```
game talent card frame, purple border #9370DB, vertical card frame, ornamental corners, flat vector art style, game UI element, transparent background, no text, no character inside --ar 3:4 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**关键参数:**
- 比例: 3:4（180×260px）
- 配色: 白#FFFFFF / 蓝#4FC3F7 / 紫#9370DB

**负面提示词:** `realistic, 3D, photograph, text, characters, complex, filled center`

---

#### 3.3.2 水人专属天赋图标（5个）

> **元素色替换规则**: 以下水人天赋图标使用蓝色系#4FC3F7/#E1F5FE/#0277BD。如需火人版本，将蓝色替换为#FF6F00/#FFE082/#BF360C。

| 天赋 | 稀有度 | 图标描述 |
|------|--------|---------|
| 寒霜深化/烈焰深化 | ★ | 冻结/点燃符号+向上箭头 |
| 冰晶吸收/火焰吸收 | ★ | 碎片+HP恢复心形 |
| 寒霜扩散/烈焰扩散 | ★★ | 冻结/点燃效果扩散波纹 |
| 极寒体质/极焰体质 | ★★★ | 角色全身强化光环 |
| 冰霜护体/烈焰护体 | ★★★ | 护盾+盾牌图标 |

**寒霜深化 ★**
```
game talent icon, frost enhancement symbol, snowflake with upward arrow, blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, fire, orange
```

**冰晶吸收 ★**
```
game talent icon, ice crystal absorption, shard with HP heart recovery, blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, fire, orange
```

**寒霜扩散 ★★**
```
game talent icon, frost spread, freeze effect ripple expanding, spreading ice waves, blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, fire, orange
```

**极寒体质 ★★★**
```
game talent icon, permafrost body enhancement, character silhouette with ice aura, ultimate ice enhancement, blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, fire, orange
```

**冰霜护体 ★★★**
```
game talent icon, frost body shield, ice armor protection, shield with snowflake, blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, fire, orange
```

---

#### 3.3.3 水火共用天赋图标（13个概念）

> **说明**: 以下13个天赋概念水人和火人通用，图标相同但元素色不同。生成时选择一种色系，另一种通过颜色替换实现。

| 天赋 | 稀有度 | 图标描述 |
|------|--------|---------|
| 冷却缩短 | ★ | 时钟+向下箭头 |
| 范围扩大 | ★ | 同心圆扩展 |
| 护盾强化 | ★★ | 盾牌+加号 |
| 双重释放 | ★★ | 双重箭头/镜像技能 |
| 生命强化 | ★ | 红心+向上箭头 |
| 生命回复 | ★ | 红心+循环箭头 |
| 能量扩容 | ★ | 能量条+扩展符号 |
| 能量恢复 | ★★ | 能量条+向上箭头 |
| 急救 | ★★ | 医疗十字+心形 |
| 碎片吸引 | ★ | 磁铁+碎片 |
| 移动加速 | ★ | 速度线+鞋印 |
| 跳跃强化 | ★★ | 向上箭头+弹簧 |
| 合成加速 | ★★ | 齿轮+闪电 |

**冷却缩短 ★**
```
game talent icon, cooldown reduction, clock with downward arrow, time decrease, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**范围扩大 ★**
```
game talent icon, range expansion, concentric circles expanding outward, area increase, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**护盾强化 ★★**
```
game talent icon, shield enhancement, shield with plus sign, stronger protection, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**双重释放 ★★★**
```
game talent icon, double release, dual arrows mirror skill, double cast symbol, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**生命强化 ★**
```
game talent icon, HP enhancement, red heart with upward arrow, health increase, flat colors red #E53935 light red #FFCDD2 dark red #B71C1C, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**生命回复 ★**
```
game talent icon, HP recovery, red heart with circular arrow, health regeneration, flat colors red #E53935 light red #FFCDD2 dark red #B71C1C, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**能量扩容 ★**
```
game talent icon, energy capacity expansion, energy bar with expand symbol, increased max energy, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**能量恢复 ★★**
```
game talent icon, energy recovery speed, energy bar with upward arrow, faster regeneration, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**急救 ★★**
```
game talent icon, first aid, medical cross with heart, emergency heal, flat colors red #E53935 light red #FFCDD2 dark red #B71C1C, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**碎片吸引 ★**
```
game talent icon, fragment attraction, magnet with shard, increased pickup range, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**移动加速 ★**
```
game talent icon, movement speed, speed lines with footprint, faster movement, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**跳跃强化 ★★**
```
game talent icon, jump enhancement, upward arrow with spring, higher jump, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**合成加速 ★★**
```
game talent icon, crafting speed, gear with lightning bolt, faster synthesis, flat colors blue #4FC3F7 light blue #E1F5FE dark blue #0277BD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

---

#### 3.3.4 共享池天赋图标（15个）

> **说明**: 共享池天赋为两名角色共用的天赋，图标使用中性色或双色（蓝+橙）。

| 天赋 | 稀有度 | 图标描述 |
|------|--------|---------|
| 默契配合 | ★ | 双手握手/合作符号 |
| 温砖亲和 | ★ | 冰火融合砖块 |
| 双人共鸣 | ★★ | 双人波形共振 |
| 心有灵犀 | ★★★ | 双人心形连接 |
| 搬运强化 | ★ | 背包+加号 |
| 碎片磁铁 | ★ | 磁铁+碎片 |
| 碎片延寿 | ★ | 碎片+时钟 |
| 合成优化 | ★★ | 齿轮+减号 |
| 碎片丰收 | ★★ | 多碎片散落 |
| 建造加速 | ★ | 砖块+闪电 |
| 结构强化 | ★ | 建筑+盾牌 |
| 材料精通 | ★★ | 多种砖块排列 |
| 建筑大师 | ★★★ | 皇冠+建筑 |
| 庇护扩展 | ★ | 双人+扩展圈 |
| 坚固庇护 | ★★★ | 双人+坚固护盾 |

**默契配合 ★**
```
game talent icon, teamwork synergy, two hands shaking cooperation symbol, partnership, flat colors dual blue #4FC3F7 orange #FF6F00, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**温砖亲和 ★**
```
game talent icon, warm brick affinity, half ice half fire fusion brick, dual element, flat colors left blue #4FC3F7 right orange #FF6F00, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**双人共鸣 ★★**
```
game talent icon, dual resonance, two waveforms resonating together, synergy waves, flat colors dual blue #4FC3F7 orange #FF6F00, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**心有灵犀 ★★★**
```
game talent icon, soul link, two hearts connected, telepathy bond, flat colors dual blue #4FC3F7 orange #FF6F00, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**搬运强化 ★**
```
game talent icon, carry capacity, backpack with plus sign, increased carry limit, flat colors brown #795548 light brown #D7CCC8, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**碎片磁铁 ★**
```
game talent icon, fragment magnet, magnet attracting crystal shards, magnetic attraction, flat colors gray #757575 blue #4FC3F7, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**碎片延寿 ★**
```
game talent icon, fragment longevity, crystal shard with clock, extended duration, flat colors blue #4FC3F7 white #F5F5F5, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**合成优化 ★★**
```
game talent icon, crafting optimization, gear with minus sign, reduced cost, flat colors gray #757575 light gray #BDBDBD, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**碎片丰收 ★★**
```
game talent icon, fragment harvest, multiple crystals scattered, abundant shards, flat colors blue #4FC3F7 orange #FF6F00 gray #757575, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**建造加速 ★**
```
game talent icon, build speed, brick with lightning bolt, faster construction, flat colors gray #757575 yellow #FFD700, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**结构强化 ★**
```
game talent icon, structure reinforcement, building with shield, stronger construction, flat colors gray #757575 light gray #BDBDBD, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**材料精通 ★★**
```
game talent icon, material mastery, multiple brick types arranged, element expert, flat colors blue #4FC3F7 orange #FF6F00 gray #757575, thick black outline, blue border #4FC3F7, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**建筑大师 ★★★**
```
game talent icon, master builder, crown on building, ultimate construction, flat colors gold #FFD700 gray #757575, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**庇护扩展 ★**
```
game talent icon, shelter expansion, two figures with expanding circle, increased shelter range, flat colors dual blue #4FC3F7 orange #FF6F00, thick black outline, white border, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

**坚固庇护 ★★★**
```
game talent icon, fortified shelter, two figures with strong shield, ultimate protection, flat colors dual blue #4FC3F7 orange #FF6F00, thick black outline, purple border #9370DB, 2D vector art, 64x64 game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text
```

---

### 3.4 蓝图UI

---

#### 3.4.1 蓝图高亮格子

**中文描述**: 蓝图建造区域的半透明绿色高亮格子，32×32px。半透明绿色填充rgba(50,255,100,0.2)，格子边框rgba(50,255,100,0.4)。矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game blueprint grid cell, semi-transparent green highlight, green fill rgba(50,255,100,0.2) green border rgba(50,255,100,0.4), square grid cell, 2D vector art, game UI element, transparent background, no text --ar 1:1 --style raw --no realistic, 3D, photograph, text, complex, opaque
```

**关键参数:**
- 比例: 1:1 | 尺寸: 32×32px
- 配色: rgba(50,255,100,0.2) / rgba(50,255,100,0.4)

**负面提示词:**
```
realistic, 3D, photograph, text, complex, opaque, dark, red, blue
```

---

#### 3.4.2 蓝图已完成格子

**中文描述**: 蓝图已完成的格子，绿色实心填充rgba(50,255,100,0.6)，带勾选标记。32×32px。

**英文 Prompt (Midjourney):**
```
game blueprint completed cell, solid green fill rgba(50,255,100,0.6), checkmark inside, square grid cell, 2D vector art, game UI element, transparent background, no text --ar 1:1 --style raw --no realistic, 3D, photograph, text, red, incomplete
```

**关键参数:** 1:1 | 32×32px | rgba(50,255,100,0.6)

**负面提示词:** `realistic, 3D, photograph, text, red, incomplete, empty`

---

#### 3.4.3 蓝图信息面板背景

**中文描述**: 蓝图信息面板背景，尺寸160×120px。半透明深色底rgba(20,20,30,0.7)，圆角矩形。简洁矢量风格。

**英文 Prompt (Midjourney):**
```
game blueprint info panel background, semi-transparent dark rectangle, dark navy rgba(20,20,30,0.7), rounded corners, green accent stripe on top, vector art style, flat design, clean minimal, no text, game UI element, transparent background --ar 4:3 --style raw --no realistic, 3D, photograph, text, characters, complex
```

**关键参数:** 4:3 | 160×120px | rgba(20,20,30,0.7)

**负面提示词:** `realistic, 3D, photograph, text, characters, complex, bright`

---

### 3.5 灾难预告UI

---

#### 3.5.1 灾难预告面板背景

**中文描述**: 灾难预告面板背景，尺寸320×140px。深红色半透明底rgba(40,10,10,0.85)，圆角矩形。带有警示三角装饰。矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game disaster warning panel background, dark red semi-transparent rectangle rgba(40,10,10,0.85), rounded corners, warning triangle decoration on top, alert aesthetic, vector art style, flat design, no text, game UI element, transparent background --ar 16:7 --style raw --no realistic, 3D, photograph, text, characters, complex, bright colors
```

**关键参数:**
- 比例: 16:7（320×140px）
- 配色: rgba(40,10,10,0.85)
- 风格: 警示感、深红半透明

**负面提示词:**
```
realistic, 3D, photograph, text, characters, complex, bright colors, cheerful
```

---

#### 3.5.2 灾难强度条

**中文描述**: 灾难强度指示条，宽120px高8px。三段渐变色：黄色(30%)→橙色(60%)→红色(100%)。矢量风格。

**英文 Prompt (Midjourney):**
```
game disaster intensity bar, horizontal bar with gradient yellow #FFD700 to orange #FF9800 to red #E53935, 3-segment intensity indicator, flat vector art style, game UI element, transparent background, no text --ar 15:1 --style raw --no realistic, 3D, photograph, text, complex, blue, green
```

**关键参数:** 15:1 | 120×8px | #FFD700→#FF9800→#E53935

**负面提示词:** `realistic, 3D, photograph, text, blue, green, single color`

---

#### 3.5.3 35种灾难图标（通用模板）

> **设计说明**: 每种灾难需要一个24×24px的小图标用于HUD和灾难预告面板。以下按6大类提供通用提示词模板，每类一个基础图标，通过修改关键词和配色生成不同灾难的图标。

**元素类灾难图标模板:**
```
game disaster icon, [KEYWORD], 24x24, flat colors [COLOR1] [COLOR2] [COLOR3], thick black outline, 2D vector art, game UI icon, transparent background --ar 1:1 --style raw --no realistic, 3D, photograph, text, complex
```

| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| E1 熔岩潮 | lava wave from ground | #8B0000 #FF4500 #1A1A1A |
| E2 冰封领域 | freezing ice spreading | #B0E0E6 #F0F8FF #191970 |
| E3 元素风暴 | water-fire collision explosion | #9370DB #4169E1 #DC143C |
| E4 蒸汽领域 | steam vapor clouds | #F5F5F5 #9E9E9E #FFCC80 |
| E5 沸腾领域 | boiling water bubbles | #8B0000 #FF4500 #F5F5F5 |
| E6 火焰龙卷 | fire tornado spiral | #FF4500 #8B0000 #1A1A1A |
| E7 水晶风暴 | flying ice crystals | #B0E0E6 #F0F8FF #191970 |
| E8 元素漩涡 | dual-color vortex spiral | #4FC3F7 #FF6F00 #9370DB |

**环境类灾难图标模板:**
| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| V1 酸雨腐蚀 | acid rain drops corroding | #2F4F4F #708090 #9ACD32 |
| V2 沙尘暴 | sandstorm swirl | #DAA520 #5D4037 #808080 |
| V3 植物疯长 | vines growing upward | #228B22 #2F4F4F #5D4037 |
| V4 地面塌陷 | ground sinkhole pit | #DAA520 #808080 #5D4037 |
| V5 雾海入侵 | fog rolling in | #9E9E9E #F5F5F5 #C8E6C9 |
| V6 酸雾弥漫 | toxic green mist | #2F4F4F #708090 #ADFF2F |

**时空类灾难图标模板:**
| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| T1 时间裂隙 | time rift crack | #9370DB #1A1A1A #F5F5F5 |
| T2 空间折叠 | space folding distortion | #9370DB #1A1A1A #9E9E9E |
| T3 重力反转 | gravity arrow pointing up | #9370DB #1A1A1A #F5F5F5 |
| T4 镜像领域 | mirror reflection | #9370DB #C0C0C0 #F5F5F5 |
| T5 时间停滞 | frozen time zone | #9E9E9E #1A1A1A #F5F5F5 |

**感知类灾难图标模板:**
| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| S1 幻象迷雾 | phantom crystal illusion | #9E9E9E #F5F5F5 #E1BEE7 |
| S2 声波干扰 | muted sound icon | #9E9E9E #1A1A1A #F5F5F5 |
| S3 意识混乱 | reversed direction arrows | #9370DB #1A1A1A #F5F5F5 |
| S4 光线扭曲 | light refraction waves | #B0E0E6 #F5F5F5 #E1BEE7 |
| S5 色彩反转 | inverted color swirl | #FF6F00 #4FC3F7 #9370DB |

**物理类灾难图标模板:**
| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| P1 磁力吸引 | magnetic arrows to edge | #00008B #708090 #2F4F4F |
| P2 冲击波 | concentric shockwave rings | #DAA520 #808080 #5D4037 |
| P3 漩涡吸引 | ground whirlpool spiral | #00008B #708090 #2F4F4F |
| P4 地震波 | ground wave ripples | #DAA520 #808080 #5D4037 |
| P5 风暴眼 | storm eye safe zone | #B0E0E6 #F0F8FF #50FF64 |

**机制类灾难图标模板:**
| 灾难 | KEYWORD | 配色 |
|------|---------|------|
| M1 元素枯竭 | fading energy particles | #00008B #708090 #2F4F4F |
| M2 共振过载 | building vibration waves | #DAA520 #808080 #5D4037 |
| M3 能量反噬 | red energy backlash | #8B0000 #FF4500 #1A1A1A |
| M4 预言干扰 | flickering blueprint | #9370DB #1A1A1A #50FF64 |
| M5 庇护削弱 | flickering energy bar | #DAA520 #808080 #E53935 |
| M6 碎片逃逸 | bouncing fragment | #B0E0E6 #F0F8FF #191970 |

---

### 3.6 技能冷却蒙版

**中文描述**: 技能冷却灰色蒙版覆盖层，48×48px。50%透明度灰色#9E9E9E覆盖，从底部向上消失的扫掠效果。矢量风格。

**英文 Prompt (Midjourney):**
```
game skill cooldown overlay, gray mask covering 50% transparency, dark gray #9E9E9E, sweeping reveal effect from bottom, 2D vector art, game UI element, transparent background, no text --ar 1:1 --style raw --no realistic, 3D, photograph, text, complex, colorful
```

**关键参数:** 1:1 | 48×48px | #9E9E9E 50%透明

**负面提示词:** `realistic, 3D, photograph, text, complex, colorful`

---

### 3.7 快捷消息气泡

**中文描述**: 角色头顶快捷消息气泡背景。圆角矩形对话气泡，指向下方（角色方向）。半透明深色底rgba(20,20,30,0.85)，圆角，底部有小三角指向。无文字，矢量风格。

**英文 Prompt (Midjourney):**
```
game chat bubble background, rounded rectangle speech bubble, dark semi-transparent rgba(20,20,30,0.85), pointing down with small triangle, 2D vector art, game UI element, transparent background, no text, clean minimal --ar 4:1 --style raw --no realistic, 3D, photograph, text, complex, bright colors
```

**关键参数:** 4:1 | 自适应宽度最大200px | rgba(20,20,30,0.85)

**负面提示词:** `realistic, 3D, photograph, text, complex, bright colors, opaque`

---

## 五、Phase 4 — 环境背景

> **设计说明**: 5种环境背景对应5种庇护环境。每种环境包含天空层、地面层和装饰元素。整体尺寸1280×640px（40×20格）。背景不包含角色和UI元素。

---

### 4.1 火山环境背景

**中文描述**: 火山地貌环境背景。地面为焦黑岩石#1A1A1A，有橙红色岩浆裂缝#FF4500。天空暗红#8B0000，有烟雾和火星飘散。远处有烧焦的枯树和熔岩石堆轮廓。灼热、危险的氛围。迷雾废墟风格。2D横板游戏背景。

**英文 Prompt (Midjourney):**
```
2D side-scroller game background, volcanic environment, charred black rock ground #1A1A1A with orange-red lava cracks #FF4500, dark red sky #8B0000 with smoke and floating embers, silhouettes of scorched dead trees and lava rock formations in distance, scorching dangerous atmosphere, misty ruins style, flat vector art, 2-tone cel shading, no characters, no UI, game background layer --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, bright, cheerful, water, ice
```

**关键参数:**
- 比例: 2:1 | 尺寸: 1280×640px
- 配色: #8B0000 / #FF4500 / #1A1A1A
- 风格: 火山地貌、焦黑岩石、岩浆裂缝

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, bright, cheerful, water, ice, snow, green plants
```

---

### 4.2 冰封环境背景

**中文描述**: 极地冰原环境背景。地面为冰面#B0E0E6，有霜花和冻结的水洼。天空冰蓝#191970加灰白色，飘雪。远处有冰晶柱和冻结瀑布轮廓。寒冷、寂静的氛围。迷雾废墟风格。

**英文 Prompt (Midjourney):**
```
2D side-scroller game background, arctic ice field environment, ice ground #B0E0E6 with frost flowers and frozen puddles, ice blue sky #191970 with gray-white clouds and falling snow, silhouettes of ice crystal pillars and frozen waterfall in distance, cold silent atmosphere, misty ruins style, flat vector art, 2-tone cel shading, no characters, no UI, game background layer --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, warm, fire, lava, hot
```

**关键参数:**
- 比例: 2:1 | 尺寸: 1280×640px
- 配色: #B0E0E6 / #F0F8FF / #191970
- 风格: 极地冰原、冰面、霜花、飘雪

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, warm, fire, lava, hot, green plants, desert
```

---

### 4.3 洪水环境背景

**中文描述**: 洪水泛滥环境背景。地面有水渍和湿漉漉的地面#2F4F4F，漂浮物。天空灰暗#00008B，雨滴，乌云。远处有漂浮木箱和水草轮廓。潮湿、压抑的氛围。迷雾废墟风格。

**英文 Prompt (Midjourney):**
```
2D side-scroller game background, flood environment, wet ground #2F4F4F with water puddles and floating debris, dark blue sky #00008B with gray clouds #708090 and rain, silhouettes of floating wooden crates and water plants in distance, damp oppressive atmosphere, misty ruins style, flat vector art, 2-tone cel shading, no characters, no UI, game background layer --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, bright, sunny, fire, lava
```

**关键参数:**
- 比例: 2:1 | 尺寸: 1280×640px
- 配色: #00008B / #708090 / #2F4F4F
- 风格: 洪水泛滥、水渍、雨滴、乌云

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, bright, sunny, fire, lava, dry, desert
```

---

### 4.4 地震环境背景

**中文描述**: 地震环境背景。地面有裂缝和不平整#5D4037，碎石。天空灰黄#DAA520，尘土飞扬。远处有断裂石柱和倾斜建筑轮廓。不稳定、危险的氛围。迷雾废墟风格。

**英文 Prompt (Midjourney):**
```
2D side-scroller game background, earthquake environment, cracked uneven ground #5D4037 with debris and rubble, gray-yellow sky #DAA520 with dusty air #808080, silhouettes of broken stone pillars and tilted buildings in distance, unstable dangerous atmosphere, misty ruins style, flat vector art, 2-tone cel shading, no characters, no UI, game background layer --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water, ice, stable ground
```

**关键参数:**
- 比例: 2:1 | 尺寸: 1280×640px
- 配色: #DAA520 / #808080 / #5D4037
- 风格: 地震、裂缝、碎石、尘土

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, fire, water, ice, stable ground, flat ground, clean
```

---

### 4.5 元素风暴环境背景

**中文描述**: 元素混乱环境背景。地面有冰霜和焦痕混合痕迹。天空紫色#9370DB，有能量漩涡和元素碎片飞舞。远处有漂浮的元素结晶和能量光束轮廓。混乱、神秘的氛围。迷雾废墟风格。

**英文 Prompt (Midjourney):**
```
2D side-scroller game background, element storm environment, ground with mixed frost and scorch marks, purple sky #9370DB with energy vortex and flying element fragments, blue #4169E1 and red #DC143C element clashes, silhouettes of floating element crystals and energy beams in distance, chaotic mysterious atmosphere, misty ruins style, flat vector art, 2-tone cel shading, no characters, no UI, game background layer --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, calm, peaceful, simple
```

**关键参数:**
- 比例: 2:1 | 尺寸: 1280×640px
- 配色: #9370DB / #4169E1 / #DC143C
- 风格: 元素混乱、能量漩涡、元素碎片飞舞

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, calm, peaceful, simple, monochrome
```

---

## 六、Phase 5 — 细节资源

### 5.1 合成台

**中文描述**: 合成台外观，放置在建筑区域中心。石质基座的方形工作台，台面有元素融合纹路（蓝+橙双色），台面中央有凹槽用于放置碎片。基座灰色#757575，台面有蓝#4FC3F7和橙#FF6F00双色装饰纹路。粗黑描边，圆角处理。约2×1格大小(64×32px)。

**英文 Prompt (Midjourney):**
```
game crafting station, stone base square workbench, element fusion patterns on surface dual blue #4FC3F7 and orange #FF6F00, central slot for fragments, gray stone base #757575 highlight #BDBDBD shadow #424242, thick black outline 3px, rounded corners, 2-tone cel shading, 2D vector art, game object sprite, transparent background, front view --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, UI, fire, water element only
```

**关键参数:**
- 比例: 2:1 | 尺寸: 64×32px（2×1格）
- 配色: #757575 / #BDBDBD / #424242 + #4FC3F7 + #FF6F00
- 风格: 石质基座、双色元素纹路

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, single color, plain, large, complex machinery
```

---

### 5.2 安全区边界 — 绿色光柱

**中文描述**: 建筑区域边界的半透明绿色光柱。竖直半透明绿色光柱，从地面延伸到天空，rgba(50,255,100,0.4)。微微发光，有粒子上升效果。透明背景，仅光柱本身。

**英文 Prompt (Midjourney):**
```
game safe zone boundary, vertical semi-transparent green light pillar, glowing green rgba(50,255,100,0.4), light beam from ground to sky, ascending particles, 2D vector art, game VFX sprite, transparent background, thin vertical beam --ar 1:4 --style raw --no realistic, 3D, photograph, text, characters, UI, red, blue, wide, horizontal
```

**关键参数:**
- 比例: 1:4（高瘦光柱）
- 配色: rgba(50,255,100,0.4)
- 风格: 半透明发光光柱、粒子上升

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, red, blue, wide, horizontal, opaque
```

---

### 5.3 安全区地面 — 亮绿区域

**中文描述**: 建筑区域内的地面纹理，偏亮绿色调。在深青灰#263238基础上有淡绿色#C8E6C9覆盖，表示安全区域。平坦无障碍物。2D横板游戏地面层。

**英文 Prompt (Midjourney):**
```
game safe zone ground tile, flat ground with light green tint #C8E6C9 over dark teal-gray #263238, subtle stone texture, no obstacles, safe area indicator, 2D vector art, flat colors, game ground layer, top surface visible, seamless tiling --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, obstacles, dark, red, decorations
```

**关键参数:**
- 比例: 2:1 | 尺寸: 480×64px（15×2格）
- 配色: #263238 + #C8E6C9覆盖

**负面提示词:**
```
realistic, 3D, photograph, text, characters, obstacles, decorations, dark, red, dangerous
```

---

### 5.4 危险区地面 — 暗灰区域

**中文描述**: 建筑区域外的地面纹理，偏暗灰色调。深灰#1A1A1A为主，比安全区更暗。平坦无障碍物但有轻微破损纹理。2D横板游戏地面层。

**英文 Prompt (Midjourney):**
```
game danger zone ground tile, flat ground dark gray #1A1A1A, subtle damaged stone texture, no obstacles, dangerous area indicator, 2D vector art, flat colors, game ground layer, top surface visible, seamless tiling --ar 2:1 --style raw --no realistic, 3D, photograph, text, characters, obstacles, bright, green, decorations
```

**关键参数:**
- 比例: 2:1 | 尺寸: 可拼接
- 配色: #1A1A1A

**负面提示词:**
```
realistic, 3D, photograph, text, characters, obstacles, bright, green, safe looking
```

---

### 5.5 庇护连线

**中文描述**: 两名角色之间的庇护能量连接线。半透明能量光带，连接两个角色。颜色根据角色组合变化：蓝-蓝连接为浅蓝#4FC3F7，蓝-橙连接为渐变蓝#4FC3F7→橙#FF6F00。能量光带有流动粒子效果。宽度约4px。

**英文 Prompt (Midjourney):**
```
game shelter energy connection beam, horizontal semi-transparent energy link, gradient blue #4FC3F7 to orange #FF6F00, flowing particles along beam, thin energy ribbon, 2D vector art, game VFX sprite, transparent background, horizontal beam --ar 8:1 --style raw --no realistic, 3D, photograph, text, characters, UI, thick, vertical, solid
```

**关键参数:**
- 比例: 8:1（细长能量带）
- 配色: #4FC3F7 → #FF6F00 渐变
- 风格: 半透明能量光带、流动粒子

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, thick, vertical, solid, opaque, chain, rope
```

---

### 5.6 主菜单背景

**中文描述**: 游戏主菜单背景。展示游戏核心场景：深青灰#263238的废墟城市轮廓，天空渐变#1A237E→#283593，迷雾弥漫。画面中央偏左有一个水人轮廓（蓝色微光），偏右有一个火人轮廓（橙色微光），两人之间有微弱的能量连线。整体氛围神秘、紧张但有希望感。16:9比例，适合1920×1080。

**英文 Prompt (Midjourney):**
```
game main menu background, dark teal-gray #263238 ruined city silhouettes, gradient sky deep indigo #1A237E to indigo #283593, misty fog atmosphere, left side blue glowing water character silhouette, right side orange glowing fire character silhouette, faint energy connection between them, mysterious tense hopeful mood, 2D vector art style, cinematic composition, no text, no UI --ar 16:9 --style raw --no realistic, 3D, photograph, text, UI, bright, cheerful, sunny, crowded
```

**关键参数:**
- 比例: 16:9 | 尺寸: 1920×1080px
- 配色: #263238 / #1A237E→#283593 / #4FC3F7 / #FF6F00
- 风格: 迷雾废墟、双角色轮廓、能量连线

**负面提示词:**
```
realistic, 3D, photograph, text, UI, bright, cheerful, sunny, crowded, detailed characters, complex
```

---

### 5.7 主菜单Logo

**中文描述**: 游戏Logo标题"双生迷城"的视觉设计。蓝色水元素和橙色火元素融合的标题文字效果。文字左半蓝色#4FC3F7带冰晶纹理，右半橙色#FF6F00带火焰纹理，中间有融合效果。粗黑描边，矢量风格。仅Logo图形，无背景。

**英文 Prompt (Midjourney):**
```
game logo title design, dual element text logo, left half blue #4FC3F7 with ice crystal texture, right half orange #FF6F00 with flame texture, fusion effect in center, thick black outline, 2D vector art style, game title logo, transparent background, no background scenery --ar 4:1 --style raw --no realistic, 3D, photograph, background scenery, text readable, complex
```

**关键参数:**
- 比例: 4:1（宽幅Logo）
- 配色: #4FC3F7 + #FF6F00 融合
- 风格: 双色元素融合标题

**负面提示词:**
```
realistic, 3D, photograph, background scenery, complex, monochrome, plain
```

---

### 5.8 碎片掉落预览 — 虚线轨迹

**中文描述**: 碎片掉落预览的虚线轨迹。竖直虚线，宽2px，半透明。3种颜色版本：蓝色(冰晶碎片)#4FC3F7、橙色(熔岩碎片)#FF6F00、金色(温砖)#FFD700。虚线从上到下，底部有半透明落点圆圈标记。

**英文 Prompt (蓝色版) (Midjourney):**
```
game fragment drop preview, vertical dashed line, semi-transparent blue #4FC3F7, dashed trajectory from top to bottom, semi-transparent circle marker at bottom, 2D vector art, game VFX sprite, transparent background, thin vertical dashed line --ar 1:8 --style raw --no realistic, 3D, photograph, text, characters, UI, solid line, horizontal, thick
```

**关键参数:**
- 比例: 1:8（竖直细线）
- 配色: #4FC3F7(蓝) / #FF6F00(橙) / #FFD700(金) 三种版本
- 风格: 半透明虚线+落点圆圈

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, solid line, horizontal, thick, opaque
```

---

### 5.9 Ping标记图标

**中文描述**: 沟通系统Ping标记图标，3种类型。碎片Ping=金色向上箭头#FFD700，危险Ping=红色圆圈#E53935，方向Ping=白色箭头#FFFFFF。半透明alpha 0.4-0.5。每种24×24px。粗黑描边。

**英文 Prompt (Midjourney):**
```
3 game ping marker icons in a row, gold upward arrow #FFD700 red circle #E53935 white directional arrow #FFFFFF, semi-transparent, thick black outline, 2D vector art, game UI icons, transparent background, no text --ar 3:1 --style raw --no realistic, 3D, photograph, text, large, complex, opaque
```

**关键参数:**
- 比例: 3:1（三种并排）
- 尺寸: 每个24×24px
- 配色: #FFD700 / #E53935 / #FFFFFF，alpha 0.4-0.5

**负面提示词:**
```
realistic, 3D, photograph, text, large, complex, opaque, detailed
```

---

### 5.10 阶段图标（7种）

**中文描述**: 7种阶段图标，对应单轮7阶段。每个24×24px。①预告=眼睛图标(灰)，②碎片收集=碎片图标(金)，③灾害预告=警告三角(橙)，④建造=砖块图标(绿)，⑤灾害冲击=爆炸图标(红)，⑥修整=扳手图标(蓝)，⑦升级=星形图标(紫)。矢量风格。

**英文 Prompt (Midjourney):**
```
7 game phase icons in a row, eye icon gray #9E9E9E, crystal shard gold #FFD700, warning triangle orange #FF9800, brick green #4CAF50, explosion red #E53935, wrench blue #4FC3F7, star purple #9370DB, thick black outline, 2D vector art, game UI icons, transparent background, no text --ar 7:1 --style raw --no realistic, 3D, photograph, text, large, complex
```

**关键参数:**
- 比例: 7:1（七种并排）
- 尺寸: 每个24×24px
- 配色: 灰/金/橙/绿/红/蓝/紫

**负面提示词:**
```
realistic, 3D, photograph, text, large, complex, same color
```

---

### 5.11 章节过渡画面背景

**中文描述**: 章节过渡画面背景。深色底#1A237E，画面中央有章节标题区域的空白空间。背景有迷雾废墟轮廓，配色随章节变化：第一章=蓝绿色调，第二章=紫红色调，第三章=深紫金色调。2D矢量风格，无文字。

**英文 Prompt (第一章) (Midjourney):**
```
game chapter transition background, dark indigo #1A237E base, misty ruins silhouettes in background, blue-green tint chapter 1, empty center space for title, atmospheric depth, 2D vector art style, no text, no characters, cinematic composition --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, bright, cheerful
```

**关键参数:**
- 比例: 16:9 | 尺寸: 1920×1080px
- 配色: 第一章#1A237E+蓝绿 / 第二章#1A237E+紫红 / 第三章#1A237E+深紫金
- 风格: 迷雾废墟、章节过渡

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, bright, cheerful, simple, flat
```

---

### 5.12 游戏失败画面背景

**中文描述**: 游戏失败画面背景。深红色调#8B0000，画面暗淡。废墟轮廓更破碎，天空阴沉。中央有空白区域用于显示失败信息。2D矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game over screen background, dark red tone #8B0000, dim atmosphere, broken ruined city silhouettes, dark gloomy sky, empty center space for text, somber mood, 2D vector art style, no text, no characters, cinematic composition --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, bright, cheerful, hopeful
```

**关键参数:**
- 比例: 16:9 | 1920×1080px
- 配色: #8B0000 深红暗调
- 风格: 失败感、破碎废墟

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, bright, cheerful, hopeful, sunny
```

---

### 5.13 游戏胜利画面背景

**中文描述**: 游戏胜利画面背景。深蓝紫色调#283593，画面有光亮感。废墟轮廓中有新的生长（冰晶和火焰元素融合的光芒），天空有微光。中央有空白区域用于显示胜利信息。2D矢量风格，无文字。

**英文 Prompt (Midjourney):**
```
game victory screen background, deep blue-purple #283593, luminous atmosphere, ruins with new growth, ice crystal and fire element fusion light, glowing sky, hopeful triumph mood, empty center space for text, 2D vector art style, no text, no characters, cinematic composition --ar 16:9 --style raw --no realistic, 3D, photograph, text, characters, UI, dark, gloomy, sad
```

**关键参数:**
- 比例: 16:9 | 1920×1080px
- 配色: #283593 深蓝紫+微光
- 风格: 胜利感、希望感、元素融合光芒

**负面提示词:**
```
realistic, 3D, photograph, text, characters, UI, dark, gloomy, sad, monochrome
```

---

## 附录A：色值速查表

### 角色配色

| 元素 | 色值 | 用途 |
|------|------|------|
| #E1F5FE | 浅水蓝 | 水人中心、冰晶高光 |
| #4FC3F7 | 湖水蓝 | 水人中段、水砖、蓝色UI装饰 |
| #0277BD | 深海蓝 | 水人边缘、深色阴影 |
| #FFE082 | 浅金黄 | 火人中心 |
| #FF6F00 | 深橙 | 火人中段、火砖 |
| #BF360C | 暗红橙 | 火人边缘 |
| #050505 | 纯黑 | 角色描边 |

### 环境配色

| 环境 | 主色 | 辅色 | 暗色 |
|------|------|------|------|
| 火山 | #8B0000 | #FF4500 | #1A1A1A |
| 冰封 | #B0E0E6 | #F0F8FF | #191970 |
| 洪水 | #00008B | #708090 | #2F4F4F |
| 地震 | #DAA520 | #808080 | #5D4037 |
| 元素风暴 | #9370DB | #4169E1 | #DC143C |

### UI配色

| 元素 | 色值 | 用途 |
|------|------|------|
| #263238 | 深青灰 | 背景基色 |
| #1A237E | 深靛蓝 | 天空上部 |
| #283593 | 靛蓝 | 天空下部 |
| #E53935 | 红色 | HP条 |
| #FF9800 | 橙色 | 火人能量条 |
| #50FF64 | 亮绿 | 安全区/蓝图 |
| #9370DB | 紫色 | 史诗稀有度 |
| #FFD700 | 金色 | 温砖/碎片收集阶段 |
| rgba(20,20,30,0.7) | 深半透明 | UI面板背景 |
| rgba(40,10,10,0.85) | 深红半透明 | 灾难预告面板 |

### 稀有度配色

| 稀有度 | 边框色 | 星级 |
|--------|--------|------|
| 普通(★) | #FFFFFF 白色 | ★ |
| 稀有(★★) | #4FC3F7 蓝色 | ★★ |
| 史诗(★★★) | #9370DB 紫色 | ★★★ |

---

## 附录B：资源统计总览

### 按Phase统计

| Phase | 类别 | 资源数 | 说明 |
|-------|------|--------|------|
| Phase 1 | 角色 | 8 | 水人/火人各4(站立+行走+跳跃+头像) |
| Phase 1 | 碎片 | 3 | 冰晶/熔岩/岩石 |
| Phase 1 | 材料 | 6 | 5种砖+温砖 |
| Phase 1 | 建筑 | 5 | 防火墙/防洪堤/加固塔/避难所/导流板 |
| Phase 1 | 基础背景 | 2 | 天空+地面 |
| **Phase 1 小计** | | **24** | |
| Phase 2 | 灾害特效 | 35 | 6大类(元素8+环境6+时空5+感知5+物理5+机制6) |
| **Phase 2 小计** | | **35** | |
| Phase 3 | HUD面板 | 7 | 2面板+HP条+2能量条+进度条+碎片图标 |
| Phase 3 | 技能图标 | 28 | 水人E8+Q6, 火人E8+Q6 |
| Phase 3 | 天赋图标 | ~38 | 水人专属5+共用13+共享池15+卡片边框3+其他2 |
| Phase 3 | 蓝图UI | 3 | 高亮格+完成格+面板背景 |
| Phase 3 | 灾难预告UI | 3 | 面板+强度条+35灾难图标 |
| Phase 3 | 其他UI | 2 | 冷却蒙版+消息气泡 |
| **Phase 3 小计** | | **~81** | |
| Phase 4 | 环境背景 | 5 | 火山/冰封/洪水/地震/元素风暴 |
| **Phase 4 小计** | | **5** | |
| Phase 5 | 合成台 | 1 | |
| Phase 5 | 安全区视觉 | 4 | 光柱+亮绿地+暗灰地+连线 |
| Phase 5 | 主菜单 | 2 | 背景+Logo |
| Phase 5 | 其他细节 | 5 | 掉落预览+Ping+阶段图标+章节过渡+失败/胜利画面 |
| **Phase 5 小计** | | **12** | |
| **总计** | | **~157** | |

### 按AI工具建议

| 工具 | 适用资源 | 说明 |
|------|----------|------|
| Midjourney v6 | 角色、建筑、环境背景、主菜单、Logo | 擅长风格化插画，--style raw保持矢量感 |
| SDXL | 技能图标、天赋图标、UI元素、灾难图标 | 擅长精确控制小图标，配合ControlNet可精确控制构图 |
| SDXL + LoRA | 灾害特效 | 可训练灾难特效LoRA保持风格统一 |

### 工作流建议

1. **先生成风格参考图**: 先生成1张水人和1张火人作为风格锚点
2. **使用参考图(Reference Image)**: 后续所有角色/技能图标生成时使用风格参考图
3. **批量生成UI图标**: UI图标尺寸小且统一，建议用SDXL批量生成
4. **环境背景先行**: 先生成5种环境背景，灾害特效叠加在环境上
5. **后处理**: 所有生成图片需要去背景(透明PNG)并调整到目标尺寸(PPU=32的倍数)

---

> **文档结束**  
> 本文档包含约157个独立AI生成提示词，覆盖游戏全部美术资源需求。所有提示词基于GDD v6.1、美术需求文档v2.0和CODELY.md风格规范编写。生成后需进行后处理（去背景、尺寸调整至PPU=32倍数）方可导入Unity工程。
