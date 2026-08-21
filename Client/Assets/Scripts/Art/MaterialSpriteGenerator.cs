/// ============================================================
/// 文件名: MaterialSpriteGenerator.cs
/// 创建时间: 2026-08-21
/// 作者: DualEnigma
/// 描述: 建筑材料Sprite生成器。程序化生成6种材料砖块Sprite：
///       水砖/冰砖/火砖/岩浆砖/石砖/温砖。
///       矢量几何风格，粗黑描边3px，渐变填充，零外部资源依赖。
/// ============================================================

using UnityEngine;
using DualEnigma.Synthesis;

namespace DualEnigma.Art
{
    /// <summary>
    /// 建筑材料Sprite生成器。
    /// 生成规格：32×32像素（1格 = 1单位，PPU=32），切角方形砖块主体，
    /// 每种材料配专属细节纹理（水纹/冰晶切面/炽热核心/岩浆裂缝/铆钉内框/冷暖交融）。
    /// 引用：CODELY.md 美术风格规范, 美术需求文档 v2.0 §5.2, MaterialType.cs
    /// </summary>
    public static class MaterialSpriteGenerator
    {
        // ---- 颜色定义 ----

        /// <summary>描边色 #050505</summary>
        private static readonly Color32 _outlineColor = new Color32(0x05, 0x05, 0x05, 0xFF);

        // 水砖：与水人同族配色（水元素凝聚）
        private static readonly Color32 _waterCenter = new Color32(0xE1, 0xF5, 0xFE, 0xFF);
        private static readonly Color32 _waterMid = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 _waterEdge = new Color32(0x02, 0x77, 0xBD, 0xFF);

        // 冰砖：白→冰蓝（冰霜质感）
        private static readonly Color32 _iceCenter = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color32 _iceMid = new Color32(0xE1, 0xF5, 0xFE, 0xFF);
        private static readonly Color32 _iceEdge = new Color32(0x81, 0xD4, 0xFA, 0xFF);

        // 火砖：与火人同族配色（炽热质感）
        private static readonly Color32 _fireCenter = new Color32(0xFF, 0xE0, 0x82, 0xFF);
        private static readonly Color32 _fireMid = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 _fireEdge = new Color32(0xBF, 0x36, 0x0C, 0xFF);

        // 岩浆砖：深熔岩色（流动质感，靠裂缝细节区分火砖）
        private static readonly Color32 _lavaCenter = new Color32(0xFF, 0xAB, 0x40, 0xFF);
        private static readonly Color32 _lavaMid = new Color32(0xE6, 0x4A, 0x19, 0xFF);
        private static readonly Color32 _lavaEdge = new Color32(0x8D, 0x1B, 0x06, 0xFF);

        // 石砖：灰系（厚重质感）
        private static readonly Color32 _stoneCenter = new Color32(0xCF, 0xCF, 0xCF, 0xFF);
        private static readonly Color32 _stoneMid = new Color32(0x9E, 0x9E, 0x9E, 0xFF);
        private static readonly Color32 _stoneEdge = new Color32(0x5A, 0x5A, 0x5A, 0xFF);

        // 温砖：左暖右冷横向渐变（火水交融特殊材料）
        private static readonly Color32 _warmLeft = new Color32(0xFF, 0x70, 0x43, 0xFF);
        private static readonly Color32 _warmCore = new Color32(0xFF, 0xD1, 0x80, 0xFF);
        private static readonly Color32 _warmRight = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        // ---- 细节颜色 ----

        /// <summary>水纹 #0288D1</summary>
        private static readonly Color32 _waterWave = new Color32(0x02, 0x88, 0xD1, 0xFF);

        /// <summary>冰晶切面线 #4FC3F7</summary>
        private static readonly Color32 _iceFacet = new Color32(0x4F, 0xC3, 0xF7, 0xFF);

        /// <summary>冰砖闪光 #FFFFFF</summary>
        private static readonly Color32 _iceSparkle = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        /// <summary>火砖核心（径向亮芯）</summary>
        private static readonly Color32 _fireCoreCenter = new Color32(0xFF, 0xF9, 0xC4, 0xFF);
        private static readonly Color32 _fireCoreMid = new Color32(0xFF, 0xE0, 0x82, 0xFF);
        private static readonly Color32 _fireCoreEdge = new Color32(0xFF, 0xCA, 0x28, 0xFF);

        /// <summary>岩浆裂缝亮黄 #FFEA00</summary>
        private static readonly Color32 _lavaCrack = new Color32(0xFF, 0xEA, 0x00, 0xFF);

        /// <summary>石砖内框线 #4F4F4F</summary>
        private static readonly Color32 _stoneRing = new Color32(0x4F, 0x4F, 0x4F, 0xFF);

        /// <summary>石砖左上高光 #E8E8E8</summary>
        private static readonly Color32 _stoneHighlight = new Color32(0xE8, 0xE8, 0xE8, 0xFF);

        /// <summary>温砖中心白光 #FFFFFF</summary>
        private static readonly Color32 _warmGlow = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        // ---- 尺寸常量 ----

        /// <summary>纹理边长（像素），1格 = 32px</summary>
        private const int _textureSize = 32;

        /// <summary>纹理中心坐标</summary>
        private const int _center = 16;

        /// <summary>描边厚度（像素），与角色同规格 3px</summary>
        private const int _outlineThickness = 3;

        /// <summary>砖块主体半宽（像素），不含描边</summary>
        private const int _brickHalf = 12;

        /// <summary>砖块四角切角（像素）</summary>
        private const int _brickChamfer = 3;

        /// <summary>
        /// 生成材料砖块Sprite。
        /// </summary>
        /// <param name="type">材料类型（6种砖）</param>
        /// <returns>程序化生成的 Sprite（32×32像素，PPU=32）</returns>
        public static Sprite GenerateMaterialSprite(MaterialType type)
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_textureSize, _textureSize);
            tex.name = GetSpriteName(type);

            switch (type)
            {
                case MaterialType.WaterBrick:
                    DrawWaterBrick(tex);
                    break;
                case MaterialType.IceBrick:
                    DrawIceBrick(tex);
                    break;
                case MaterialType.FireBrick:
                    DrawFireBrick(tex);
                    break;
                case MaterialType.LavaBrick:
                    DrawLavaBrick(tex);
                    break;
                case MaterialType.StoneBrick:
                    DrawStoneBrick(tex);
                    break;
                case MaterialType.WarmBrick:
                    DrawWarmBrick(tex);
                    break;
            }

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 获取材料Sprite资源名称。
        /// </summary>
        public static string GetSpriteName(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.WaterBrick: return "Material_WaterBrick";
                case MaterialType.IceBrick: return "Material_IceBrick";
                case MaterialType.FireBrick: return "Material_FireBrick";
                case MaterialType.LavaBrick: return "Material_LavaBrick";
                case MaterialType.StoneBrick: return "Material_StoneBrick";
                case MaterialType.WarmBrick: return "Material_WarmBrick";
                default: return "Material_Unknown";
            }
        }

        // ---- 各材料绘制 ----

        /// <summary>
        /// 水砖：水人同族渐变 + 两道深蓝水纹。
        /// </summary>
        private static void DrawWaterBrick(Texture2D tex)
        {
            DrawBrickBase(tex, _waterCenter, _waterMid, _waterEdge);
            DrawWaveLine(tex, 12, _waterWave);
            DrawWaveLine(tex, 19, _waterWave);
        }

        /// <summary>
        /// 冰砖：白→冰蓝渐变 + 内嵌菱形冰晶切面线 + 白色闪光点。
        /// </summary>
        private static void DrawIceBrick(Texture2D tex)
        {
            DrawBrickBase(tex, _iceCenter, _iceMid, _iceEdge);
            DrawInsetDiamondOutline(tex, 8, _iceFacet);
            ProceduralSpriteGenerator.SetPixel(tex, 13, 19, _iceSparkle);
            ProceduralSpriteGenerator.SetPixel(tex, 20, 12, _iceSparkle);
        }

        /// <summary>
        /// 火砖：火人同族渐变 + 中心炽热亮芯 + 余烬亮点。
        /// </summary>
        private static void DrawFireBrick(Texture2D tex)
        {
            DrawBrickBase(tex, _fireCenter, _fireMid, _fireEdge);
            ProceduralSpriteGenerator.DrawRadialGradientCircle(
                tex, 16, 15, 4f, _fireCoreCenter, _fireCoreMid, _fireCoreEdge);
        }

        /// <summary>
        /// 岩浆砖：深熔岩渐变 + 锯齿状发光裂缝（流动感）。
        /// </summary>
        private static void DrawLavaBrick(Texture2D tex)
        {
            DrawBrickBase(tex, _lavaCenter, _lavaMid, _lavaEdge);
            DrawJaggedCrack(tex);
        }

        /// <summary>
        /// 石砖：灰系渐变 + 深灰内框（厚重感） + 左上高光。
        /// </summary>
        private static void DrawStoneBrick(Texture2D tex)
        {
            DrawBrickBase(tex, _stoneCenter, _stoneMid, _stoneEdge);
            DrawInnerRing(tex, _stoneRing);
            ProceduralSpriteGenerator.SetPixel(tex, 10, 20, _stoneHighlight);
            ProceduralSpriteGenerator.SetPixel(tex, 11, 21, _stoneHighlight);
        }

        /// <summary>
        /// 温砖：左暖右冷横向渐变 + 中心白光（火水交融）。
        /// </summary>
        private static void DrawWarmBrick(Texture2D tex)
        {
            // 横向渐变主体：左 #FF7043 → 中心 #FFD180 → 右 #4FC3F7
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = Mathf.Abs(x - _center);
                    float dy = Mathf.Abs(y - _center);

                    if (IsInsideSquare(dx, dy, _brickHalf, _brickChamfer))
                    {
                        float t = Mathf.Clamp01((x - (_center - _brickHalf)) / (float)(_brickHalf * 2));
                        Color32 color = Lerp3(_warmLeft, _warmCore, _warmRight, t);
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, color);
                    }
                    else if (IsInsideSquare(dx, dy, _brickHalf + _outlineThickness, _brickChamfer + _outlineThickness))
                    {
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, _outlineColor);
                    }
                }
            }

            // 中心白光 2×2
            ProceduralSpriteGenerator.SetPixel(tex, 15, 16, _warmGlow);
            ProceduralSpriteGenerator.SetPixel(tex, 16, 16, _warmGlow);
            ProceduralSpriteGenerator.SetPixel(tex, 15, 15, _warmGlow);
            ProceduralSpriteGenerator.SetPixel(tex, 16, 15, _warmGlow);
        }

        // ---- 砖块专用绘制辅助 ----

        /// <summary>
        /// 绘制砖块主体：带3px描边的切角方形，方形度量径向渐变。
        /// </summary>
        private static void DrawBrickBase(
            Texture2D tex, Color32 centerColor, Color32 midColor, Color32 edgeColor)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = Mathf.Abs(x - _center);
                    float dy = Mathf.Abs(y - _center);

                    if (IsInsideSquare(dx, dy, _brickHalf, _brickChamfer))
                    {
                        float t = Mathf.Max(dx, dy) / _brickHalf;
                        Color32 color = Lerp3(centerColor, midColor, edgeColor, t);
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, color);
                    }
                    else if (IsInsideSquare(dx, dy, _brickHalf + _outlineThickness, _brickChamfer + _outlineThickness))
                    {
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, _outlineColor);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制水纹线（锯齿波，每4px上下起伏1px）。
        /// </summary>
        private static void DrawWaveLine(Texture2D tex, int baseY, Color32 color)
        {
            for (int x = 7; x <= 25; x++)
            {
                int y = baseY + ((x - 7) / 4) % 2;
                ProceduralSpriteGenerator.SetPixel(tex, x, y, color);
            }
        }

        /// <summary>
        /// 绘制内嵌菱形切面线（|dx|+|dy| == radius 的菱形环）。
        /// </summary>
        private static void DrawInsetDiamondOutline(Texture2D tex, int radius, Color32 color)
        {
            for (int x = _center - radius; x <= _center + radius; x++)
            {
                int dy = radius - Mathf.Abs(x - _center);
                ProceduralSpriteGenerator.SetPixel(tex, x, _center + dy, color);
                ProceduralSpriteGenerator.SetPixel(tex, x, _center - dy, color);
            }
        }

        /// <summary>
        /// 绘制锯齿裂缝：主缝竖向每4px左右偏移1px + 两条横向分支。
        /// </summary>
        private static void DrawJaggedCrack(Texture2D tex)
        {
            // 主裂缝
            for (int y = 6; y <= 26; y++)
            {
                int x = 16 + (y / 4) % 2;
                ProceduralSpriteGenerator.SetPixel(tex, x, y, _lavaCrack);
            }

            // 上分支（向左）
            for (int x = 11; x <= 16; x++)
                ProceduralSpriteGenerator.SetPixel(tex, x, 9, _lavaCrack);

            // 下分支（向右）
            for (int x = 16; x <= 21; x++)
                ProceduralSpriteGenerator.SetPixel(tex, x, 22, _lavaCrack);
        }

        /// <summary>
        /// 绘制内框线（四边矩形环，四角留2px缺口呼应外框切角）。
        /// </summary>
        private static void DrawInnerRing(Texture2D tex, Color32 color)
        {
            const int x0 = 9, y0 = 9, x1 = 23, y1 = 23;

            // 上下边（跳过四角2px）
            for (int x = x0 + 2; x <= x1 - 2; x++)
            {
                ProceduralSpriteGenerator.SetPixel(tex, x, y0, color);
                ProceduralSpriteGenerator.SetPixel(tex, x, y1, color);
            }

            // 左右边（跳过四角2px）
            for (int y = y0 + 2; y <= y1 - 2; y++)
            {
                ProceduralSpriteGenerator.SetPixel(tex, x0, y, color);
                ProceduralSpriteGenerator.SetPixel(tex, x1, y, color);
            }
        }

        /// <summary>
        /// 方形区域判定（含45°切角）。dx/dy 为到中心的绝对偏移。
        /// </summary>
        private static bool IsInsideSquare(float dx, float dy, float half, float chamfer)
        {
            if (dx > half || dy > half)
                return false;

            // 四角切角：角部三角区不算在方形内
            float cornerX = dx - (half - chamfer);
            float cornerY = dy - (half - chamfer);
            if (cornerX > 0f && cornerY > 0f && cornerX + cornerY > chamfer)
                return false;

            return true;
        }

        /// <summary>
        /// 三段色线性插值：t≤0.5 中心→中段，t>0.5 中段→边缘。
        /// </summary>
        private static Color32 Lerp3(Color32 centerColor, Color32 midColor, Color32 edgeColor, float t)
        {
            if (t <= 0.5f)
                return Color32.Lerp(centerColor, midColor, t / 0.5f);
            return Color32.Lerp(midColor, edgeColor, (t - 0.5f) / 0.5f);
        }
    }
}
