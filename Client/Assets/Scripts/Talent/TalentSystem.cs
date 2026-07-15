/// ============================================================
/// 文件名: TalentSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 天赋系统管理器，管理天赋池、选择机制和效果叠加。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Skill;
using DualEnigma.Shelter;

namespace DualEnigma.Talent
{
    /// <summary>
    /// 天赋系统管理器。继承 Singleton<T>，注册 ITalentSystem 到 ServiceLocator。
    /// 引用：天赋系统.md §3.1
    /// </summary>
    public class TalentSystem : Singleton<TalentSystem>, ITalentSystem
    {
        /// <summary>水人已选天赋列表</summary>
        public List<TalentData> AquaTalents { get; } = new List<TalentData>();

        /// <summary>火人已选天赋列表</summary>
        public List<TalentData> IgnisTalents { get; } = new List<TalentData>();

        /// <summary>天赋配置</summary>
        private TalentConfig _config;

        /// <summary>未获得稀有+计数器</summary>
        private int _noRarityCounter;
        /// <summary>急救天赋出现次数（保底）</summary>
        private int _firstAidCount;
        /// <summary>当前章节</summary>
        private int _currentChapter = 1;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ITalentSystem>(this);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            Debug.Log("[TalentSystem] 天赋系统初始化完成");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
        }

        /// <summary>
        /// 发放3个天赋供选择。
        /// </summary>
        public List<TalentData> DrawTalents(CharacterType owner, int chapter)
        {
            _currentChapter = chapter;

            if (_config == null)
                _config = Resources.Load<TalentConfig>("TalentConfig");

            if (_config == null)
            {
                Debug.LogWarning("[TalentSystem] TalentConfig 未加载");
                return new List<TalentData>();
            }

            float[] rates = _config.GetRarityRates(chapter);

            bool applyBoost = _noRarityCounter >= _config.RarityBoostThreshold;
            if (applyBoost)
            {
                rates = BoostRates(rates);
                Debug.Log("[TalentSystem] 稀有提升触发！");
            }

            List<TalentData> pool = GetTargetPool(owner);
            return DrawFromPool(pool, 3, rates, applyBoost);
        }

        /// <summary>
        /// 选择天赋。
        /// </summary>
        public void SelectTalent(CharacterType owner, int talentId)
        {
            TalentData talent = FindTalentById(talentId);
            if (talent == null)
            {
                Debug.LogWarning($"[TalentSystem] 未找到天赋: {talentId}");
                return;
            }

            List<TalentData> targetList = owner == CharacterType.Aqua ? AquaTalents : IgnisTalents;

            if (!talent.Stackable && targetList.Exists(t => t.Id == talentId))
            {
                Debug.Log($"[TalentSystem] {owner} 天赋{talent.Name}不可叠加，跳过");
                return;
            }

            if (talent.MaxStacks > 0)
            {
                int currentStacks = targetList.FindAll(t => t.Id == talentId).Count;
                if (currentStacks >= talent.MaxStacks)
                {
                    Debug.Log($"[TalentSystem] {owner} 天赋{talent.Name}已达叠加上限");
                    return;
                }
            }

            targetList.Add(talent);

            if (talent.Rarity >= Rarity.Rare)
                _noRarityCounter = 0;
            else
                _noRarityCounter++;

            ApplyTalentEffects(owner, talent);

            byte playerId = (byte)(owner == CharacterType.Aqua ? 0 : 1);
            EventBus.Instance.Publish(new TalentSelectedEvent
            {
                talentId = talentId,
                playerId = playerId
            });

            Debug.Log($"[TalentSystem] {owner} 选择天赋: {talent.Name} ({talent.Rarity})");
        }

        /// <summary>
        /// 获取已选天赋的叠加效果。
        /// </summary>
        public TalentEffectSummary GetEffectSummary(CharacterType owner)
        {
            List<TalentData> talents = owner == CharacterType.Aqua ? AquaTalents : IgnisTalents;
            TalentEffectSummary summary = new TalentEffectSummary();

            foreach (var talent in talents)
            {
                ApplyEffect(summary, talent);
            }

            summary.CooldownReduction = Mathf.Min(summary.CooldownReduction, 0.8f);
            summary.DamageMultiplier = Mathf.Max(summary.DamageMultiplier, 0.1f);
            summary.MoveSpeedMultiplier = Mathf.Min(summary.MoveSpeedMultiplier, 2f);
            summary.EnergyRecoveryMultiplier = Mathf.Min(summary.EnergyRecoveryMultiplier, 3f);

            return summary;
        }

        private void ApplyTalentEffects(CharacterType owner, TalentData talent)
        {
            TalentEffectSummary summary = GetEffectSummary(owner);

            var skillSystem = ServiceLocator.Get<ISkillSystem>();
            if (skillSystem != null)
            {
                skillSystem.SetCooldownReduction(summary.CooldownReduction);
            }

            var shelterSystem = ServiceLocator.Get<IShelterSystem>();
            if (shelterSystem != null)
            {
                ShelterParams shelterParams = new ShelterParams();
                bool needsShelterUpdate = false;

                switch (talent.EffectId)
                {
                    case TalentEffectId.EnergyMaxBonus:
                        shelterParams.MaxEnergy = talent.EffectValue;
                        needsShelterUpdate = true;
                        break;
                    case TalentEffectId.ShelterDistance:
                        shelterParams.ShelterDistance = talent.EffectValue;
                        needsShelterUpdate = true;
                        break;
                    case TalentEffectId.DamageReduction:
                        shelterParams.DamageMultiplier = 0.5f;
                        needsShelterUpdate = true;
                        break;
                }

                if (needsShelterUpdate)
                    shelterSystem.ModifyParams(shelterParams);
            }
        }

        private List<TalentData> GetTargetPool(CharacterType owner)
        {
            List<TalentData> pool = new List<TalentData>();
            if (_config == null) return pool;

            if (owner == CharacterType.Aqua)
            {
                pool.AddRange(_config.AquaPool);
                pool.AddRange(_config.SharedPool);
            }
            else
            {
                pool.AddRange(_config.IgnisPool);
                pool.AddRange(_config.SharedPool);
            }
            return pool;
        }

        private TalentData FindTalentById(int talentId)
        {
            if (_config == null) return null;

            TalentData talent = _config.AquaPool.Find(t => t.Id == talentId);
            if (talent != null) return talent;

            talent = _config.IgnisPool.Find(t => t.Id == talentId);
            if (talent != null) return talent;

            return _config.SharedPool.Find(t => t.Id == talentId);
        }

        private List<TalentData> DrawFromPool(List<TalentData> pool, int count, float[] rates, bool boost)
        {
            List<TalentData> result = new List<TalentData>();
            List<TalentData> available = new List<TalentData>(pool);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int rarityIndex = WeightedRandom(rates);
                Rarity targetRarity = (Rarity)rarityIndex;

                List<TalentData> rarityPool = available.FindAll(t => t.Rarity == targetRarity);
                if (rarityPool.Count == 0)
                {
                    rarityPool = available;
                }

                int idx = Random.Range(0, rarityPool.Count);
                TalentData drawn = rarityPool[idx];
                result.Add(drawn);
                available.Remove(drawn);
            }

            return result;
        }

        private float[] BoostRates(float[] original)
        {
            float[] boosted = new float[3];
            boosted[0] = 0f;
            boosted[1] = original[1] + original[0] * 0.5f;
            boosted[2] = original[2] + original[0] * 0.5f;
            return boosted;
        }

        private void ApplyEffect(TalentEffectSummary summary, TalentData talent)
        {
            switch (talent.EffectId)
            {
                case TalentEffectId.HPBonus:
                    summary.HPBonus += (int)talent.EffectValue;
                    break;
                case TalentEffectId.EnergyMaxBonus:
                    summary.EnergyMaxBonus += talent.EffectValue;
                    break;
                case TalentEffectId.EnergyRecovery:
                    summary.EnergyRecoveryMultiplier += talent.EffectValue / 20f;
                    break;
                case TalentEffectId.ShelterDistance:
                    summary.ShelterDistanceBonus += talent.EffectValue;
                    break;
                case TalentEffectId.CooldownReduction:
                    summary.CooldownReduction += talent.EffectValue;
                    break;
                case TalentEffectId.RangeMultiplier:
                    summary.RangeMultiplier += talent.EffectValue;
                    break;
                case TalentEffectId.CarryLimit:
                    summary.CarryLimitBonus += (int)talent.EffectValue;
                    break;
                case TalentEffectId.MoveSpeed:
                    summary.MoveSpeedMultiplier += talent.EffectValue;
                    break;
                case TalentEffectId.DoubleRelease:
                    summary.CanDoubleRelease = true;
                    break;
                case TalentEffectId.PassiveChance:
                    summary.PassiveChanceBonus += talent.EffectValue;
                    break;
            }
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

        private void OnPhaseChanged(PhaseChangedEvent evt)
        {
            if (evt.phase == GamePhase.Upgrade)
            {
                _currentChapter = GameManager.Instance.State.Progress.Chapter;
            }
        }
    }
}
