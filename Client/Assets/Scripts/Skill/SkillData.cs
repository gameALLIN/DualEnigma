/// ============================================================
/// 文件名: SkillData.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能卡牌数据结构。
/// ============================================================

using UnityEngine;
using DualEnigma.Character;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能卡牌数据。
    /// 引用：技能系统.md §2.2
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        /// <summary>技能ID</summary>
        public int SkillId;
        /// <summary>技能名称</summary>
        public string Name;
        /// <summary>技能类型</summary>
        public SkillType Type;
        /// <summary>稀有度</summary>
        public Rarity Rarity;
        /// <summary>效果系数（×1.0/×1.5/×2.0）</summary>
        public float EffectMultiplier = 1f;
        /// <summary>冷却时间（秒）</summary>
        public float Cooldown;
        /// <summary>持续时间（秒）</summary>
        public float Duration;
        /// <summary>效果范围</summary>
        public float Range;
        /// <summary>描述</summary>
        public string Description;
        /// <summary>所属角色（Aqua/Ignis）</summary>
        public CharacterType Owner;
        /// <summary>技能效果类型（用于效果分发）</summary>
        public SkillEffectType EffectType;
    }

    /// <summary>
    /// 技能运行时状态
    /// </summary>
    [System.Serializable]
    public class SkillState
    {
        /// <summary>技能数据</summary>
        public SkillData Data;
        /// <summary>当前冷却剩余时间（秒）</summary>
        public float CooldownRemaining;
        /// <summary>是否可用</summary>
        public bool IsReady => CooldownRemaining <= 0f;
    }
}
