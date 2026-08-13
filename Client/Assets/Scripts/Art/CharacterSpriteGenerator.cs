/// ============================================================
/// 文件名: CharacterSpriteGenerator.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 角色Sprite生成器。程序化生成水人(Aqua)和火人(Ignis)的
///       Q版大头小身Sprite，矢量几何风格，粗黑描边，径向渐变。
/// ============================================================

using UnityEngine;
using DualEnigma.Character;

namespace DualEnigma.Art
{
    /// <summary>
    /// 角色Sprite生成器。
    /// 生成规格：32px宽 × 64px高（1格宽 × 2格高，PPU=32）。
    /// 头部占75%（大头圆形径向渐变），身体占25%（小梯形边缘色填充）。
    /// 引用：CODELY.md 美术风格规范 — Q版大头小身(头占75%)，粗黑描边3px，径向渐变
    /// </summary>
    public static class CharacterSpriteGenerator
    {
        // ---- 颜色定义 ----

        /// <summary>描边色 #050505</summary>
        private static readonly Color32 _outlineColor = new Color32(0x05, 0x05, 0x05, 0xFF);

        /// <summary>描边厚度（像素）</summary>
        private const int _outlineThickness = 3;

        // 水人 Aqua 配色：中心#E1F5FE → 中段#4FC3F7 → 边缘#0277BD
        private static readonly Color32 _aquaCenter = new Color32(0xE1, 0xF5, 0xFE, 0xFF);
        private static readonly Color32 _aquaMid = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
        private static readonly Color32 _aquaEdge = new Color32(0x02, 0x77, 0xBD, 0xFF);

        // 火人 Ignis 配色：中心#FFE082 → 中段#FF6F00 → 边缘#BF360C
        private static readonly Color32 _ignisCenter = new Color32(0xFF, 0xE0, 0x82, 0xFF);
        private static readonly Color32 _ignisMid = new Color32(0xFF, 0x6F, 0x00, 0xFF);
        private static readonly Color32 _ignisEdge = new Color32(0xBF, 0x36, 0x0C, 0xFF);

        // ---- 尺寸常量 ----

        /// <summary>纹理宽度（像素），1格 = 32px</summary>
        private const int _textureWidth = 32;

        /// <summary>纹理高度（像素），2格 = 64px</summary>
        private const int _textureHeight = 64;

        // 头部参数
        /// <summary>头部圆心 X（纹理水平中心）</summary>
        private const int _headCenterX = 16;

        /// <summary>头部圆心 Y（位于上方75%区域的偏上位置）</summary>
        private const int _headCenterY = 47;

        /// <summary>头部填充半径（像素），不含描边</summary>
        private const float _headRadius = 12f;

        // 身体参数（梯形）
        /// <summary>身体顶部 Y（紧接头部下方）</summary>
        private const int _bodyTopY = 30;

        /// <summary>身体底部 Y（留1px底边距）</summary>
        private const int _bodyBottomY = 2;

        /// <summary>身体顶部半宽（像素），顶部较宽</summary>
        private const int _bodyTopHalfWidth = 9;

        /// <summary>身体底部半宽（像素），底部较窄</summary>
        private const int _bodyBottomHalfWidth = 5;

        /// <summary>
        /// 生成角色Sprite。
        /// </summary>
        /// <param name="type">角色类型（水人/火人）</param>
        /// <returns>程序化生成的 Sprite（32×64像素，PPU=32）</returns>
        public static Sprite GenerateCharacterSprite(CharacterType type)
        {
            Texture2D tex = ProceduralSpriteGenerator.CreateTexture(_textureWidth, _textureHeight);
            tex.name = type == CharacterType.Aqua ? "Sprite_Aqua" : "Sprite_Ignis";

            GetCharacterColors(type, out Color centerColor, out Color midColor, out Color edgeColor);

            // 1. 绘制头部：带描边的径向渐变大圆
            ProceduralSpriteGenerator.DrawOutlinedGradientCircle(
                tex,
                _headCenterX, _headCenterY, _headRadius,
                centerColor, midColor, edgeColor,
                _outlineColor, _outlineThickness);

            // 2. 绘制身体：带描边的梯形，用边缘色填充
            ProceduralSpriteGenerator.DrawOutlinedTrapezoid(
                tex,
                _bodyTopY, _bodyBottomY,
                _bodyTopHalfWidth, _bodyBottomHalfWidth,
                _headCenterX,
                edgeColor,
                _outlineColor, _outlineThickness);

            ProceduralSpriteGenerator.Apply(tex);
            return ProceduralSpriteGenerator.TextureToSprite(tex);
        }

        /// <summary>
        /// 根据角色类型获取三段配色。
        /// </summary>
        private static void GetCharacterColors(
            CharacterType type, out Color center, out Color mid, out Color edge)
        {
            if (type == CharacterType.Aqua)
            {
                center = _aquaCenter;
                mid = _aquaMid;
                edge = _aquaEdge;
            }
            else
            {
                center = _ignisCenter;
                mid = _ignisMid;
                edge = _ignisEdge;
            }
        }
    }
}
