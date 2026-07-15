/// ============================================================
/// 文件名: SkillEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统相关枚举定义。
/// ============================================================

using DualEnigma.Character;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能/天赋稀有度。
    /// 引用：技能系统设计.md §1.3 卡牌稀有度
    /// </summary>
    public enum Rarity
    {
        /// <summary>普通 ★ — 效果系数×1.0，抽卡权重50%</summary>
        Common,
        /// <summary>稀有 ★★ — 效果系数×1.5，抽卡权重35%</summary>
        Rare,
        /// <summary>史诗 ★★★ — 效果系数×2.0，抽卡权重15%</summary>
        Epic,
    }

    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        /// <summary>E技能（冷却20-45秒，每轮可多次使用）</summary>
        E,
        /// <summary>Q技能（冷却60-90秒，可能跨轮冷却）</summary>
        Q,
        /// <summary>被动技能（固定，不可选择）</summary>
        Passive,
    }
}
