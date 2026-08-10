/// ============================================================
/// 文件名: ISkillSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统服务接口。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Character;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能系统服务接口，注册到 ServiceLocator。
    /// 引用：技能系统.md §3.1
    /// </summary>
    public interface ISkillSystem
    {
        /// <summary>抽取卡牌</summary>
        List<SkillData> DrawCards(CharacterType owner, SkillType type);

        /// <summary>选择卡牌</summary>
        void SelectCard(CharacterType owner, SkillType type, int skillId);

        /// <summary>释放技能</summary>
        void ActivateSkill(CharacterType owner, SkillType type, Vector2 targetPosition);

        /// <summary>每帧更新（冷却倒计时）</summary>
        void OnUpdate(float deltaTime);

        /// <summary>设置冷却缩短（天赋系统调用）</summary>
        void SetCooldownReduction(float reduction);

        /// <summary>
        /// 设置范围扩大修饰器（天赋系统调用）。
        /// 引用：技能系统.md §4.6 范围扩大天赋
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="multiplier">范围加成（如 0.3 表示 +30%）</param>
        void SetRangeMultiplier(byte playerId, float multiplier);

        /// <summary>
        /// 设置护盾强化标志（天赋系统调用）。
        /// 引用：技能系统.md §4.6 护盾强化天赋
        /// </summary>
        /// <param name="active">护盾强化是否激活</param>
        void SetShieldActive(bool active);

        /// <summary>
        /// 设置双重释放概率（天赋系统调用）。
        /// 引用：技能系统.md §4.6 双重释放天赋
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="chance">双重释放概率（0-1，1=必定触发）</param>
        void SetDoubleCastChance(byte playerId, float chance);

        /// <summary>
        /// 设置被动技能触发概率加成（天赋系统调用）。
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="bonus">概率加成（0-1）</param>
        void SetPassiveChanceBonus(byte playerId, float bonus);

        /// <summary>
        /// 查询被动技能触发概率加成（供 FragmentSystem 调用）。
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <returns>概率加成（0-1，未设置则返回0）</returns>
        float GetPassiveChanceBonus(byte playerId);

        /// <summary>
        /// 注册被动技能。
        /// 引用：技能系统.md §4.2 被动技能
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="passive">被动技能类型</param>
        void RegisterPassive(byte playerId, PassiveSkillType passive);

        /// <summary>
        /// 查询被动技能是否激活（供 FragmentSystem 调用）。
        /// 引用：技能系统.md §4.2 被动技能触发时机
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <param name="passive">被动技能类型</param>
        /// <returns>是否已注册该被动技能</returns>
        bool IsPassiveActive(byte playerId, PassiveSkillType passive);

        /// <summary>
        /// 查询护盾减伤比例（供 ShelterSystem 调用以减少伤害）。
        /// 引用：技能系统.md §4.5 寒霜护盾/火焰护盾
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <returns>减伤比例（0=无护盾，0.5=减少50%伤害）</returns>
        float GetShieldReduction(byte playerId);

        /// <summary>
        /// 设置确定性随机种子（供同步/回放使用）。
        /// </summary>
        /// <param name="seed">随机种子</param>
        void SetSeed(uint seed);
    }
}
