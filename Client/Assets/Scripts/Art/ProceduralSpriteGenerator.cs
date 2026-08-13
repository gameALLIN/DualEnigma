/// ============================================================
/// 文件名: ProceduralSpriteGenerator.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 程序化Sprite生成核心工具类。提供像素级绘制、径向渐变圆形、
///       粗描边和Texture转Sprite功能，零外部资源依赖。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 程序化Sprite生成核心工具类。
    /// 提供静态方法，无需 MonoBehaviour 挂载。
    /// 引用：CODELY.md 美术风格规范
    /// </summary>
    public static class ProceduralSpriteGenerator
    {
        /// <summary>像素单位（每单位像素数），与项目 PPU 一致。</summary>
        public const int PixelsPerUnit = 32;

        /// <summary>
        /// 创建透明背景的 Texture2D。
        /// </summary>
        /// <param name="width">纹理宽度（像素）</param>
        /// <param name="height">纹理高度（像素）</param>
        /// <returns>初始化为完全透明的 Texture2D</returns>
        public static Texture2D CreateTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = transparent;

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// 设置单个像素颜色。坐标越界时静默忽略。
        /// </summary>
        public static void SetPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height)
                return;
            tex.SetPixel(x, y, color);
        }

        /// <summary>
        /// 填充矩形区域。
        /// </summary>
        /// <param name="x0">左下角 X</param>
        /// <param name="y0">左下角 Y</param>
        /// <param name="x1">右上角 X</param>
        /// <param name="y1">右上角 Y</param>
        public static void FillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixel(tex, x, y, color);
                }
            }
        }

        /// <summary>
        /// 绘制水平线段。
        /// </summary>
        public static void DrawHorizontalLine(Texture2D tex, int x0, int x1, int y, Color color)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            for (int x = minX; x <= maxX; x++)
                SetPixel(tex, x, y, color);
        }

        /// <summary>
        /// 绘制实心圆形（单色填充）。
        /// </summary>
        /// <param name="centerX">圆心 X</param>
        /// <param name="centerY">圆心 Y</param>
        /// <param name="radius">半径（像素）</param>
        /// <param name="color">填充颜色</param>
        public static void DrawSolidCircle(
            Texture2D tex, int centerX, int centerY, float radius, Color color)
        {
            int rCeil = Mathf.CeilToInt(radius);
            int xMin = centerX - rCeil;
            int xMax = centerX + rCeil;
            int yMin = centerY - rCeil;
            int yMax = centerY + rCeil;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius)
                        SetPixel(tex, x, y, color);
                }
            }
        }

        /// <summary>
        /// 绘制径向渐变圆形（3段色：中心色→中段色→边缘色）。
        /// 渐变在半径范围内线性插值：t=0.0 中心色，t=0.5 中段色，t=1.0 边缘色。
        /// </summary>
        /// <param name="centerX">圆心 X</param>
        /// <param name="centerY">圆心 Y</param>
        /// <param name="radius">半径（像素）</param>
        /// <param name="centerColor">中心色</param>
        /// <param name="midColor">中段色</param>
        /// <param name="edgeColor">边缘色</param>
        public static void DrawRadialGradientCircle(
            Texture2D tex, int centerX, int centerY, float radius,
            Color centerColor, Color midColor, Color edgeColor)
        {
            if (radius <= 0f)
                return;

            int rCeil = Mathf.CeilToInt(radius);
            int xMin = centerX - rCeil;
            int xMax = centerX + rCeil;
            int yMin = centerY - rCeil;
            int yMax = centerY + rCeil;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius)
                    {
                        float t = dist / radius; // 0~1
                        Color color;
                        if (t <= 0.5f)
                        {
                            // 中心色 → 中段色
                            color = Color.Lerp(centerColor, midColor, t / 0.5f);
                        }
                        else
                        {
                            // 中段色 → 边缘色
                            color = Color.Lerp(midColor, edgeColor, (t - 0.5f) / 0.5f);
                        }
                        SetPixel(tex, x, y, color);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制圆形描边（粗描边，像素级）。
        /// 在指定半径的外侧绘制指定厚度的描边环。
        /// </summary>
        /// <param name="centerX">圆心 X</param>
        /// <param name="centerY">圆心 Y</param>
        /// <param name="radius">圆形主体半径</param>
        /// <param name="outlineColor">描边颜色</param>
        /// <param name="thickness">描边厚度（像素）</param>
        public static void DrawCircleOutline(
            Texture2D tex, int centerX, int centerY, float radius,
            Color outlineColor, int thickness)
        {
            float outerRadius = radius + thickness;
            int rCeil = Mathf.CeilToInt(outerRadius);
            int xMin = centerX - rCeil;
            int xMax = centerX + rCeil;
            int yMin = centerY - rCeil;
            int yMax = centerY + rCeil;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // 描边区域：主体半径 < 距离 <= 外半径
                    if (dist > radius && dist <= outerRadius)
                        SetPixel(tex, x, y, outlineColor);
                }
            }
        }

        /// <summary>
        /// 绘制带描边的径向渐变圆形。
        /// 先绘制描边大圆（实心，半径=fillRadius+outlineThickness），
        /// 再绘制径向渐变小圆（半径=fillRadius）覆盖内部。
        /// </summary>
        public static void DrawOutlinedGradientCircle(
            Texture2D tex, int centerX, int centerY, float fillRadius,
            Color centerColor, Color midColor, Color edgeColor,
            Color outlineColor, int outlineThickness)
        {
            // 1. 描边大圆（实心，覆盖描边区域 + 内部）
            DrawSolidCircle(tex, centerX, centerY, fillRadius + outlineThickness, outlineColor);

            // 2. 渐变填充小圆（覆盖描边内部，留出描边环）
            DrawRadialGradientCircle(tex, centerX, centerY, fillRadius,
                centerColor, midColor, edgeColor);
        }

        /// <summary>
        /// 绘制梯形（单色填充）。
        /// 从 topY 到 bottomY，宽度从 topHalfWidth*2 线性渐变到 bottomHalfWidth*2。
        /// </summary>
        /// <param name="topY">顶部 Y 坐标</param>
        /// <param name="bottomY">底部 Y 坐标</param>
        /// <param name="topHalfWidth">顶部半宽</param>
        /// <param name="bottomHalfWidth">底部半宽</param>
        /// <param name="centerX">中心 X 坐标</param>
        public static void DrawTrapezoid(
            Texture2D tex,
            int topY, int bottomY,
            int topHalfWidth, int bottomHalfWidth,
            int centerX, Color color)
        {
            int yHigh = Mathf.Max(topY, bottomY);
            int yLow = Mathf.Min(topY, bottomY);
            int height = yHigh - yLow;

            if (height <= 0)
            {
                // 退化为水平线
                DrawHorizontalLine(tex, centerX - topHalfWidth, centerX + topHalfWidth, yLow, color);
                return;
            }

            for (int y = yLow; y <= yHigh; y++)
            {
                float t = (float)(y - yLow) / height; // 0=底部, 1=顶部
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(bottomHalfWidth, topHalfWidth, t));
                DrawHorizontalLine(tex, centerX - halfWidth, centerX + halfWidth, y, color);
            }
        }

        /// <summary>
        /// 绘制带描边的梯形。
        /// 先绘制描边大梯形（各边扩大 outlineThickness），再绘制填充小梯形覆盖内部。
        /// </summary>
        public static void DrawOutlinedTrapezoid(
            Texture2D tex,
            int topY, int bottomY,
            int topHalfWidth, int bottomHalfWidth,
            int centerX,
            Color fillColor,
            Color outlineColor, int outlineThickness)
        {
            // 1. 描边大梯形（向外扩展 outlineThickness）
            DrawTrapezoid(
                tex,
                topY + outlineThickness,
                bottomY - outlineThickness,
                topHalfWidth + outlineThickness,
                bottomHalfWidth + outlineThickness,
                centerX, outlineColor);

            // 2. 填充小梯形（覆盖描边内部）
            DrawTrapezoid(
                tex,
                topY, bottomY,
                topHalfWidth, bottomHalfWidth,
                centerX, fillColor);
        }

        /// <summary>
        /// 将 Texture2D 转换为 Sprite。
        /// 使用 SpriteMeshType.FullRect, FilterMode.Point, PPU=32。
        /// </summary>
        /// <param name="tex">源纹理</param>
        /// <returns>创建的 Sprite，枢轴在中心</returns>
        public static Sprite TextureToSprite(Texture2D tex)
        {
            tex.filterMode = FilterMode.Point;
            tex.Apply();

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), // 枢轴在中心
                PixelsPerUnit,
                0u, // extrude
                SpriteMeshType.FullRect
            );

            sprite.name = tex.name;
            return sprite;
        }

        /// <summary>
        /// 应用所有像素变更到 Texture2D（调用 Texture2D.Apply）。
        /// </summary>
        public static void Apply(Texture2D tex)
        {
            tex.Apply();
        }
    }
}
