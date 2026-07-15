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

    /// <summary>
    /// 被动技能类型。
    /// 引用：技能系统.md §4.2 被动技能
    /// </summary>
    public enum PassiveSkillType
    {
        /// <summary>寒霜体质（水人）— 接住碎片时有概率冻结</summary>
        FrostAura,
        /// <summary>烈焰体质（火人）— 接住碎片时有概率点燃</summary>
        FlameAura,
    }

    /// <summary>
    /// 技能效果类型，用于 ActivateSkill 中分发执行不同效果。
    /// 引用：技能系统.md §4.3 技能释放
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>伤害型 — 对范围内灾难造成伤害/停止灾难</summary>
        Damage,
        /// <summary>冻结型 — 冻结区域内碎片（设置 FragmentState.Frozen）</summary>
        Freeze,
        /// <summary>护盾型 — 为角色添加临时护盾（减少受到的伤害）</summary>
        Shield,
        /// <summary>加速型 — 临时提升角色移速</summary>
        SpeedBoost,
        /// <summary>治疗型 — 恢复角色HP</summary>
        Heal,
    }
}
