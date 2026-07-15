/// ============================================================
/// 文件名: SkillSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统管理器，管理卡牌抽取、技能释放和冷却。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Character;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能系统管理器。继承 Singleton<T>，注册 ISkillSystem 到 ServiceLocator。
    /// 引用：技能系统.md §3.1
    /// </summary>
    public class SkillSystem : Singleton<SkillSystem>, ISkillSystem
    {
        /// <summary>水人已选E技能</summary>
        public SkillState AquaESkill { get; private set; }
        /// <summary>水人已选Q技能</summary>
        public SkillState AquaQSkill { get; private set; }
        /// <summary>火人已选E技能</summary>
        public SkillState IgnisESkill { get; private set; }
        /// <summary>火人已选Q技能</summary>
        public SkillState IgnisQSkill { get; private set; }

        /// <summary>技能配置</summary>
        private SkillConfig _config;

        /// <summary>冷却缩短（天赋修改，0-0.8）</summary>
        private float _cooldownReduction;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ISkillSystem>(this);
            Debug.Log("[SkillSystem] 技能系统初始化完成");
        }

        /// <summary>
        /// 设置冷却缩短（天赋系统调用）。
        /// </summary>
        public void SetCooldownReduction(float reduction)
        {
            _cooldownReduction = Mathf.Clamp01(reduction);
        }

        /// <summary>
        /// 抽取卡牌（游戏开始时调用）。
        /// </summary>
        public List<SkillData> DrawCards(CharacterType owner, SkillType type)
        {
            if (_config == null)
                _config = Resources.Load<SkillConfig>("SkillConfig");

            if (_config == null)
            {
                Debug.LogWarning("[SkillSystem] SkillConfig 未加载");
                return new List<SkillData>();
            }

            List<SkillData> pool = GetPool(owner, type);
            return DrawFromPool(pool, 3, _config.DrawWeights);
        }

        /// <summary>
        /// 选择卡牌。
        /// </summary>
        public void SelectCard(CharacterType owner, SkillType type, int skillId)
        {
            SkillState state = new SkillState
            {
                Data = FindSkillById(owner, type, skillId),
                CooldownRemaining = 0f
            };

            if (state.Data == null)
            {
                Debug.LogWarning($"[SkillSystem] 未找到技能: {skillId}");
                return;
            }

            if (owner == CharacterType.Aqua)
            {
                if (type == SkillType.E) AquaESkill = state;
                else if (type == SkillType.Q) AquaQSkill = state;
            }
            else
            {
                if (type == SkillType.E) IgnisESkill = state;
                else if (type == SkillType.Q) IgnisQSkill = state;
            }

            Debug.Log($"[SkillSystem] {owner} 选定{type}技能: {state.Data.Name}");
        }

        /// <summary>
        /// 释放技能。
        /// </summary>
        public void ActivateSkill(CharacterType owner, SkillType type, Vector2 targetPosition)
        {
            SkillState skill = GetSkillState(owner, type);
            if (skill == null || !skill.IsReady)
            {
                Debug.Log($"[SkillSystem] {owner}的{type}技能不可用");
                return;
            }

            byte playerId = (byte)(owner == CharacterType.Aqua ? 0 : 1);
            EventBus.Instance.Publish(new SkillActivatedEvent
            {
                skillId = skill.Data.SkillId,
                playerId = playerId,
                targetPos = targetPosition
            });

            float cooldown = skill.Data.Cooldown * (1f - _cooldownReduction);
            skill.CooldownRemaining = cooldown;

            Debug.Log($"[SkillSystem] {owner} 释放{type}技能: {skill.Data.Name}, 冷却{cooldown}s");
        }

        /// <summary>
        /// 每帧更新（冷却倒计时）。
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            UpdateCooldown(AquaESkill, deltaTime);
            UpdateCooldown(AquaQSkill, deltaTime);
            UpdateCooldown(IgnisESkill, deltaTime);
            UpdateCooldown(IgnisQSkill, deltaTime);
        }

        private void UpdateCooldown(SkillState skill, float dt)
        {
            if (skill != null && skill.CooldownRemaining > 0f)
                skill.CooldownRemaining = Mathf.Max(0f, skill.CooldownRemaining - dt);
        }

        private SkillState GetSkillState(CharacterType owner, SkillType type)
        {
            if (owner == CharacterType.Aqua)
                return type == SkillType.E ? AquaESkill : AquaQSkill;
            else
                return type == SkillType.E ? IgnisESkill : IgnisQSkill;
        }

        private List<SkillData> GetPool(CharacterType owner, SkillType type)
        {
            if (_config == null) return new List<SkillData>();

            if (owner == CharacterType.Aqua)
                return type == SkillType.E ? _config.AquaEPool : _config.AquaQPool;
            else
                return type == SkillType.E ? _config.IgnisEPool : _config.IgnisQPool;
        }

        private SkillData FindSkillById(CharacterType owner, SkillType type, int skillId)
        {
            List<SkillData> pool = GetPool(owner, type);
            foreach (var skill in pool)
            {
                if (skill.SkillId == skillId)
                    return skill;
            }
            return null;
        }

        private List<SkillData> DrawFromPool(List<SkillData> pool, int count, float[] weights)
        {
            List<SkillData> result = new List<SkillData>();
            List<SkillData> available = new List<SkillData>(pool);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int rarityIndex = WeightedRandom(weights);
                Rarity targetRarity = (Rarity)rarityIndex;

                List<SkillData> rarityPool = available.FindAll(s => s.Rarity == targetRarity);
                if (rarityPool.Count == 0)
                {
                    rarityPool = available;
                }

                int idx = Random.Range(0, rarityPool.Count);
                SkillData drawn = rarityPool[idx];
                result.Add(drawn);
                available.Remove(drawn);
            }

            return result;
        }

        private int WeightedRandom(float[] weights)
        {
            float total = 0f;
            foreach (float w in weights) total += w;

            float roll = Random.Range(0f, total);
            float cumulative = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                    return i;
            }

            return 0;
        }
    }
}
