/// ============================================================
/// 文件名: ClickEffectEnums.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 点击特效系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Art
{
    /// <summary>
    /// 点击特效类型（10种）。
    /// 覆盖屏幕点击、按钮点击等点击反馈场景：
    /// 元素主题（水/火/冰/岩/温）+ 通用主题（脉冲/星光/聚拢/烟雾/冲击波）。
    /// 引用：ClickEffectPrefabGenerator.cs（预制体生成顺序与此枚举一致）, ClickEffectInput.cs
    /// </summary>
    public enum ClickEffectType
    {
        /// <summary>水波纹 — 三圈扩散涟漪 + 水滴迸溅（水元素）</summary>
        WaterRipple = 0,
        /// <summary>火花迸溅 — 中心闪光 + 火星四射（火元素）</summary>
        FireSpark = 1,
        /// <summary>冰晶碎裂 — 白色脉冲环 + 冰屑飞散（冰元素）</summary>
        IceShatter = 2,
        /// <summary>岩石碎尘 — 灰尘升腾 + 碎屑弹开（岩元素）</summary>
        RockDust = 3,
        /// <summary>圆环脉冲 — 单圈扩散淡出（通用默认）</summary>
        RingPulse = 4,
        /// <summary>星光闪烁 — 四芒星错位弹出（通用强调）</summary>
        StarTwinkle = 5,
        /// <summary>元素交融 — 蓝橙光点向心聚拢 + 中心白闪（双生主题）</summary>
        ElementMix = 6,
        /// <summary>烟雾消散 — 柔和烟团升腾淡出（通用轻反馈）</summary>
        Poof = 7,
        /// <summary>冲击波 — 双层快慢冲击环 + 高速光点（通用强反馈）</summary>
        Shockwave = 8,
        /// <summary>温暖光晕 — 暖光呼吸 + 环绕光点（温砖/庇护主题）</summary>
        WarmGlow = 9,
    }

    /// <summary>
    /// 粒子贴图类型（白色+Alpha，可被 startColor 任意染色）。
    /// Soft/Dot 直接使用 Unity 自带 Default-Particle 材质；
    /// Ring/Spark/Chip 由程序化贴图生成（自带材质无法表现环形/星形/方形）。
    /// 引用：ParticleTextureGenerator.cs, ClickEffectPrefabGenerator.cs
    /// </summary>
    public enum ParticleTextureType
    {
        /// <summary>柔光圆 — 径向渐变透明（光晕/烟尘）</summary>
        Soft,
        /// <summary>实心圆点 — 硬边（液滴/光点/碎屑替代）</summary>
        Dot,
        /// <summary>圆环 — 环形（涟漪/脉冲/冲击波）</summary>
        Ring,
        /// <summary>四芒星 — 十字星形（火花/闪光）</summary>
        Spark,
        /// <summary>方形碎片 — 实心方块（冰屑/岩屑）</summary>
        Chip,
    }
}
