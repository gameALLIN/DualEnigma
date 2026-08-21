/// ============================================================
/// 文件名: FragmentSpriteGenerator.cs
/// 创建时间: 2026-08-21
/// 作者: DualEnigma
/// 描述: 碎片Sprite生成器。程序化生成3种掉落碎片Sprite：
///       冰晶(蓝白棱形半透明)、熔岩(橙红不规则形)、岩石(灰色方形粗糙)。
///       矢量几何风格，粗黑描边，径向渐变，零外部资源依赖。
/// ============================================================

using UnityEngine;
using DualEnigma.Fragment;

namespace DualEnigma.Art
{
    /// <summary>
    /// 碎片Sprite生成器。
    /// 生成规格：24×24像素（PPU=32），按稀有度区分剪影可读性：
    /// 冰晶★竖长棱形、熔岩★★不规则团块、岩石★★★方形。
    /// 引用：CODELY.md 美术风格规范, 美术需求文档 v2.0 §5.1, FragmentEnums.cs
    /// </summary>
    public static class FragmentSpriteGenerator
    {
        // ---- 颜色定义 ----

        /// <summary>描边色 #050505</summary>
        private static readonly Color32 _outlineColor = new Color32(0x05, 0x05, 0x05, 0xFF);

        // 冰晶：白 → 冰蓝 → 亮蓝，半透明（alpha 0.88 表现晶体质感）
        private static readonly Color32 _iceCenter = new Color32(0xFF, 0xFF, 0xFF, 0xE0);
        private static readonly Color32 _iceMid = new Color32(0xB3, 0xE5, 0xFC, 0xE0);
        private static readonly Color32 _iceEdge = new Color32(0x4F, 0xC3, 0xF7, 0xE0);

        // 熔岩：暖黄 → 橙 → 深红（与火人配色同族，炽热感）
        private static readonly Color32 _lavaCenter = new Color32(0xFF, 0xE0, 0x82, 0xFF);
        private static readonly Color32 _lavaMid = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 _lavaEdge = new Color32(0xBF, 0x36, 0x0C, 0xFF);

        // 岩石：浅灰 → 中灰 → 深灰
        private static readonly Color32 _rockCenter = new Color32(0xCF, 0xCF, 0xCF, 0xFF);
        private static readonly Color32 _rockMid = new Color32(0x9E, 0x9E, 0x9E, 0xFF);
        private static readonly Color32 _rockEdge = new Color32(0x60, 0x60, 0x60, 0xFF);

        /// <summary>岩石暗色斑点 #4F4F4F（粗糙质感）</summary>
        private static readonly Color32 _rockSpeckle = new Color32(0x4F, 0x4F, 0x4F, 0xFF);

        /// <summary>岩石左上高光 #E8E8E8（左上主光源）</summary>
        private static readonly Color32 _rockHighlight = new Color32(0xE8, 0xE8, 0xE8, 0xFF);

        // ---- 尺寸常量 ----

        /// <summary>纹理边长（像素），3种碎片统一 24×24</summary>
        private const int _textureSize = 24;

        /// <summary>纹理中心坐标</summary>
        private const int _center = 12;

        /// <summary>描边厚度（像素）。角色为3px，小尺寸碎片等比缩为2px保持剪影清晰</summary>
        private const int _outlineThickness = 2;

        /// <summary>
        /// 生成碎片Sprite。
        /// </summary>
        /// <param name="type">碎片类型（冰晶/熔岩/岩石）</param>
        /// <returns>程序化生成的 Sprite（24×24像素，PPU=32）</returns>
        public static Sprite GenerateFragmentSprite(FragmentType type)
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_textureSize, _textureSize);
            tex.name = GetSpriteName(type);

            switch (type)
            {
                case FragmentType.IceCrystal:
                    DrawIceCrystal(tex);
                    break;
                case FragmentType.Lava:
                    DrawLava(tex);
                    break;
                case FragmentType.Rock:
                    DrawRock(tex);
                    break;
            }

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 获取碎片Sprite资源名称。
        /// </summary>
        public static string GetSpriteName(FragmentType type)
        {
            switch (type)
            {
                case FragmentType.IceCrystal: return "Fragment_IceCrystal";
                case FragmentType.Lava: return "Fragment_Lava";
                case FragmentType.Rock: return "Fragment_Rock";
                default: return "Fragment_Unknown";
            }
        }

        /// <summary>
        /// 绘制冰晶碎片：竖长棱形（半宽6/半高9），菱形度量径向渐变，半透明。
        /// </summary>
        private static void DrawIceCrystal(Texture2D tex)
        {
            DrawOutlinedGradientDiamond(
                tex, _center, _center, 6, 9,
                _iceCenter, _iceMid, _iceEdge,
                _outlineColor, _outlineThickness);
        }

        /// <summary>
        /// 绘制熔岩碎片：不规则形（主圆+3凸起重叠成团块）。
        /// 先绘制描边层（各圆半径+描边厚度），再绘制渐变填充层覆盖内部。
        /// </summary>
        private static void DrawLava(Texture2D tex)
        {
            // 描边层
            ProceduralSpriteGenerator.DrawSolidCircle(tex, 12, 11, 6.5f + _outlineThickness, _outlineColor);
            ProceduralSpriteGenerator.DrawSolidCircle(tex, 6, 15, 3f + _outlineThickness, _outlineColor);
            ProceduralSpriteGenerator.DrawSolidCircle(tex, 17, 14, 3f + _outlineThickness, _outlineColor);
            ProceduralSpriteGenerator.DrawSolidCircle(tex, 10, 17, 2.5f + _outlineThickness, _outlineColor);

            // 渐变填充层
            ProceduralSpriteGenerator.DrawRadialGradientCircle(tex, 12, 11, 6.5f, _lavaCenter, _lavaMid, _lavaEdge);
            ProceduralSpriteGenerator.DrawRadialGradientCircle(tex, 6, 15, 3f, _lavaCenter, _lavaMid, _lavaEdge);
            ProceduralSpriteGenerator.DrawRadialGradientCircle(tex, 17, 14, 3f, _lavaCenter, _lavaMid, _lavaEdge);
            ProceduralSpriteGenerator.DrawRadialGradientCircle(tex, 10, 17, 2.5f, _lavaCenter, _lavaMid, _lavaEdge);
        }

        /// <summary>
        /// 绘制岩石碎片：16×16方形，四角3px切角，方形度量渐变，
        /// 固定坐标暗斑 + 左上高光表现粗糙石质。
        /// </summary>
        private static void DrawRock(Texture2D tex)
        {
            DrawOutlinedGradientSquare(
                tex, _center, _center, 8, 3,
                _rockCenter, _rockMid, _rockEdge,
                _outlineColor, _outlineThickness);

            // 暗色斑点（固定坐标保证每次生成结果一致）
            ProceduralSpriteGenerator.SetPixel(tex, 8, 9, _rockSpeckle);
            ProceduralSpriteGenerator.SetPixel(tex, 13, 6, _rockSpeckle);
            ProceduralSpriteGenerator.SetPixel(tex, 9, 14, _rockSpeckle);
            ProceduralSpriteGenerator.SetPixel(tex, 15, 13, _rockSpeckle);
            ProceduralSpriteGenerator.SetPixel(tex, 6, 17, _rockSpeckle);
            ProceduralSpriteGenerator.SetPixel(tex, 16, 10, _rockSpeckle);

            // 左上高光（左上主光源）
            ProceduralSpriteGenerator.SetPixel(tex, 8, 15, _rockHighlight);
            ProceduralSpriteGenerator.SetPixel(tex, 9, 16, _rockHighlight);
        }

        // ---- 碎片专用绘制辅助（不改动 ProceduralSpriteGenerator 核心） ----

        /// <summary>
        /// 绘制带描边的径向渐变棱形。
        /// 菱形度量 d = |dx|/halfW + |dy|/halfH，d≤1 为填充区，渐变 t=d。
        /// </summary>
        private static void DrawOutlinedGradientDiamond(
            Texture2D tex, int cx, int cy, int halfW, int halfH,
            Color32 centerColor, Color32 midColor, Color32 edgeColor,
            Color32 outlineColor, int outlineThickness)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = Mathf.Abs(y - cy);
                    float d = dx / halfW + dy / halfH;

                    if (d <= 1f)
                    {
                        Color32 color = Lerp3(centerColor, midColor, edgeColor, d);
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, color);
                    }
                    else if (dx / (halfW + outlineThickness) + dy / (halfH + outlineThickness) <= 1f)
                    {
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, outlineColor);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制带描边的径向渐变方形（含45°切角）。
        /// 方形度量 t = max(|dx|,|dy|)/half，中心0 → 边缘1。
        /// </summary>
        private static void DrawOutlinedGradientSquare(
            Texture2D tex, int cx, int cy, int half, int chamfer,
            Color32 centerColor, Color32 midColor, Color32 edgeColor,
            Color32 outlineColor, int outlineThickness)
        {
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = Mathf.Abs(y - cy);

                    if (IsInsideSquare(dx, dy, half, chamfer))
                    {
                        float t = Mathf.Max(dx, dy) / half;
                        Color32 color = Lerp3(centerColor, midColor, edgeColor, t);
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, color);
                    }
                    else if (IsInsideSquare(dx, dy, half + outlineThickness, chamfer + outlineThickness))
                    {
                        ProceduralSpriteGenerator.SetPixel(tex, x, y, outlineColor);
                    }
                }
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
