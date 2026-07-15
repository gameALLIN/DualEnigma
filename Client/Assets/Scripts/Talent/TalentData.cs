/// ============================================================
/// 文件名: TalentData.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 天赋数据结构。
/// ============================================================

using System;
using DualEnigma.Skill;

namespace DualEnigma.Talent
{
    /// <summary>
    /// 天赋数据。
    /// 引用：天赋系统.md §2.1
    /// </summary>
    [Serializable]
    public class TalentData
    {
        /// <summary>天赋ID</summary>
        public int Id;
        /// <summary>天赋名称</summary>
        public string Name;
        /// <summary>稀有度</summary>
        public Rarity Rarity;
        /// <summary>目标类型</summary>
        public TalentTarget TargetType;
        /// <summary>效果类型</summary>
        public TalentEffectType EffectType;
        /// <summary>具体效果标识</summary>
        public TalentEffectId EffectId;
        /// <summary>效果值</summary>
        public float EffectValue;
        /// <summary>效果描述</summary>
        public string Description;
        /// <summary>是否可叠加</summary>
        public bool Stackable = true;
        /// <summary>最大叠加次数（0=无上限）</summary>
        public int MaxStacks;
    }

    /// <summary>
    /// 天赋效果汇总
    /// </summary>
    [Serializable]
    public class TalentEffectSummary
    {
        public int HPBonus;
        public float EnergyMaxBonus;
        public float EnergyRecoveryMultiplier = 1f;
        public float ShelterDistanceBonus;
        public float DamageMultiplier = 1f;
        public float CooldownReduction;
        public float RangeMultiplier = 1f;
        public int CarryLimitBonus;
        public float MoveSpeedMultiplier = 1f;
        public float PassiveChanceBonus;
        public bool CanDoubleRelease;
    }
}
