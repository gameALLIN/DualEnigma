/// ============================================================
/// 文件名: MapSpriteGenerator.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 地图Sprite生成器。程序化生成第一关地图所需的背景、天空、
///       地面、墙壁和安全区标记Sprite，矢量几何风格，零外部资源依赖。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Art
{
    /// <summary>
    /// 地图Sprite生成器。
    /// 生成规格：基于 GDD 地图设计规范，地图整体 40×20格 (1280×640px)。
    /// 引用：CODELY.md 美术风格规范, GDD §9.地图设计
    /// </summary>
    public static class MapSpriteGenerator
    {
        // ---- 颜色定义 ----

        /// <summary>背景色 #263238</summary>
        private static readonly Color32 _backgroundColor = new Color32(0x26, 0x32, 0x38, 0xFF);

        /// <summary>天空顶部色 #1A237E</summary>
        private static readonly Color32 _skyTopColor = new Color32(0x1A, 0x23, 0x7E, 0xFF);

        /// <summary>天空底部色 #283593</summary>
        private static readonly Color32 _skyBottomColor = new Color32(0x28, 0x35, 0x93, 0xFF);

        /// <summary>地面色 #383838</summary>
        private static readonly Color32 _groundColor = new Color32(0x38, 0x38, 0x38, 0xFF);

        /// <summary>墙壁色 #212121</summary>
        private static readonly Color32 _wallColor = new Color32(0x21, 0x21, 0x21, 0xFF);

        /// <summary>安全区色 #4CAF50, alpha=0.2</summary>
        private static readonly Color _safeZoneColor = new Color(0.3f, 0.69f, 0.31f, 0.2f);

        /// <summary>网格线色 白色 alpha=0.15</summary>
        private static readonly Color _gridLineColor = new Color(1f, 1f, 1f, 0.15f);

        // ---- 尺寸常量 (基于GDD: 1格 = 32px, 地图 40×20格) ----

        private const int _gridSize = 32;

        /// <summary>背景: 40格宽 × 20格高</summary>
        private const int _backgroundWidth = 40 * _gridSize;   // 1280
        private const int _backgroundHeight = 20 * _gridSize;  // 640

        /// <summary>天空: 40格宽 × 10格高</summary>
        private const int _skyWidth = 40 * _gridSize;   // 1280
        private const int _skyHeight = 10 * _gridSize;  // 320

        /// <summary>地面: 40格宽 × 2格高</summary>
        private const int _groundWidth = 40 * _gridSize;  // 1280
        private const int _groundHeight = 2 * _gridSize;  // 64

        /// <summary>墙壁: 2格宽 × 4格高</summary>
        private const int _wallWidth = 2 * _gridSize;   // 64
        private const int _wallHeight = 4 * _gridSize;  // 128

        /// <summary>安全区/建筑区: 15格宽 × 8格高</summary>
        private const int _safeZoneWidth = 15 * _gridSize;  // 480
        private const int _safeZoneHeight = 8 * _gridSize;  // 256

        /// <summary>
        /// 生成背景Sprite — 纯色 #263238，40×20格。
        /// </summary>
        public static Sprite GenerateBackgroundSprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_backgroundWidth, _backgroundHeight);
            tex.name = "Map_Background";
            ProceduralSpriteGenerator.FillRect(tex, 0, 0, _backgroundWidth - 1, _backgroundHeight - 1, _backgroundColor);
            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 生成天空Sprite — 垂直渐变 #1A237E→#283593，40×10格。
        /// </summary>
        public static Sprite GenerateSkySprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_skyWidth, _skyHeight);
            tex.name = "Map_Sky";

            for (int y = 0; y < _skyHeight; y++)
            {
                float t = (float)y / _skyHeight;
                Color32 c = Color32.Lerp(_skyBottomColor, _skyTopColor, t);
                ProceduralSpriteGenerator.DrawHorizontalLine(tex, 0, _skyWidth - 1, y, c);
            }

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 生成地面Sprite — 纯色 #383838，40×2格。
        /// </summary>
        public static Sprite GenerateGroundSprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_groundWidth, _groundHeight);
            tex.name = "Map_Ground";
            ProceduralSpriteGenerator.FillRect(tex, 0, 0, _groundWidth - 1, _groundHeight - 1, _groundColor);
            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 生成墙壁Sprite — 纯色 #212121，2×4格。
        /// </summary>
        public static Sprite GenerateWallSprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_wallWidth, _wallHeight);
            tex.name = "Map_Wall";
            ProceduralSpriteGenerator.FillRect(tex, 0, 0, _wallWidth - 1, _wallHeight - 1, _wallColor);
            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 生成安全区Sprite — 半透明绿色 #4CAF50 alpha=0.2，15×8格。
        /// </summary>
        public static Sprite GenerateSafeZoneSprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_safeZoneWidth, _safeZoneHeight);
            tex.name = "Map_SafeZone";
            ProceduralSpriteGenerator.FillRect(tex, 0, 0, _safeZoneWidth - 1, _safeZoneHeight - 1, _safeZoneColor);

            // 边框线（略亮绿色，alpha=0.4）
            Color border = new Color(0.3f, 0.69f, 0.31f, 0.4f);
            ProceduralSpriteGenerator.DrawHorizontalLine(tex, 0, _safeZoneWidth - 1, 0, border);
            ProceduralSpriteGenerator.DrawHorizontalLine(tex, 0, _safeZoneWidth - 1, _safeZoneHeight - 1, border);
            // 左右边
            for (int y = 0; y < _safeZoneHeight; y++)
            {
                ProceduralSpriteGenerator.SetPixel(tex, 0, y, border);
                ProceduralSpriteGenerator.SetPixel(tex, _safeZoneWidth - 1, y, border);
            }

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 生成建筑网格Sprite — 透明背景 + 白色网格线，15×8格。
        /// 每格32px，线宽1px，alpha=0.15。
        /// </summary>
        public static Sprite GenerateGridSprite()
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_safeZoneWidth, _safeZoneHeight);
            tex.name = "Map_BuildingGrid";

            // 水平线
            for (int row = 0; row <= 8; row++)
            {
                int y = row * _gridSize;
                if (y >= _safeZoneHeight) y = _safeZoneHeight - 1;
                ProceduralSpriteGenerator.DrawHorizontalLine(tex, 0, _safeZoneWidth - 1, y, _gridLineColor);
            }

            // 垂直线
            for (int col = 0; col <= 15; col++)
            {
                int x = col * _gridSize;
                if (x >= _safeZoneWidth) x = _safeZoneWidth - 1;
                for (int y = 0; y < _safeZoneHeight; y++)
                    ProceduralSpriteGenerator.SetPixel(tex, x, y, _gridLineColor);
            }

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }
    }
}
