/// ============================================================
/// 文件名: SkillSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统管理器，管理卡牌抽取、技能释放、被动技能和冷却。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Fragment;
using DualEnigma.Shelter;
using DualEnigma.Disaster;
using DualEnigma.Building;
using DualEnigma.Data;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能系统管理器。继承 Singleton<T>，注册 ISkillSystem 到 ServiceLocator。
    /// 引用：技能系统.md §3.1
    /// </summary>
    public class SkillSystem : Singleton<SkillSystem>, ISkillSystem
    {
        // ──────────────────────────────────────────────
        //  已选技能状态
        // ──────────────────────────────────────────────

        /// <summary>水人已选E技能</summary>
        public SkillState AquaESkill { get; private set; }
        /// <summary>水人已选Q技能</summary>
        public SkillState AquaQSkill { get; private set; }
        /// <summary>火人已选E技能</summary>
        public SkillState IgnisESkill { get; private set; }
        /// <summary>火人已选Q技能</summary>
        public SkillState IgnisQSkill { get; private set; }

        // ──────────────────────────────────────────────
        //  配置
        // ──────────────────────────────────────────────

        /// <summary>技能配置</summary>
        private SkillConfig _config;

        // ──────────────────────────────────────────────
        //  天赋修饰器
        // ──────────────────────────────────────────────

        /// <summary>冷却缩短（天赋修改，0-0.8）</summary>
        private float _cooldownReduction;

        /// <summary>范围扩大修饰器（玩家ID → 范围加成，如 0.3 = +30%）</summary>
        private readonly Dictionary<byte, float> _rangeMultiplier = new Dictionary<byte, float>();

        /// <summary>护盾强化标志（护盾天赋激活时为 true，延长护盾持续时间 50%）</summary>
        private bool _shieldActive;

        /// <summary>双重释放概率（玩家ID → 概率 0-1）</summary>
        private readonly Dictionary<byte, float> _doubleCastChance = new Dictionary<byte, float>();

        // ──────────────────────────────────────────────
        //  被动技能
        // ──────────────────────────────────────────────

        /// <summary>每个角色的被动技能集合（玩家ID → 被动技能集合）</summary>
        private readonly Dictionary<byte, HashSet<PassiveSkillType>> _activePassives =
            new Dictionary<byte, HashSet<PassiveSkillType>>();

        // ──────────────────────────────────────────────
        //  运行时效果状态
        // ──────────────────────────────────────────────

        /// <summary>护盾效果剩余时间（秒）</summary>
        private float _shieldRemainingTime;
        /// <summary>护盾持有者玩家ID</summary>
        private byte _shieldOwner = 0xFF;
        /// <summary>护盾减伤比例（0-1，如 0.5 = 减少50%伤害）</summary>
        private float _shieldReduction;

        /// <summary>加速效果剩余时间（秒）</summary>
        private float _speedBoostRemainingTime;
        /// <summary>加速持有者玩家ID</summary>
        private byte _speedBoostOwner = 0xFF;
        /// <summary>加速前的原始移速（用于恢复）</summary>
        private float _originalMoveSpeed;

        // ──────────────────────────────────────────────
        //  阶段追踪（跨轮冷却）
        // ──────────────────────────────────────────────

        /// <summary>当前游戏阶段</summary>
        private GamePhase _currentPhase = GamePhase.Preview;

        // ──────────────────────────────────────────────
        //  生命周期
        // ──────────────────────────────────────────────

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ISkillSystem>(this);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            Debug.Log("[SkillSystem] 技能系统初始化完成");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
        }

        /// <summary>
        /// MonoBehaviour Update — 自驱动冷却递减，确保所有阶段（含修整/升级）冷却持续。
        /// 引用：技能系统.md §4.4 Q技能跨轮冷却规则
        /// </summary>
        private void Update()
        {
            OnUpdate(Time.deltaTime);
        }

        // ──────────────────────────────────────────────
        //  天赋修饰器设置
        // ──────────────────────────────────────────────

        /// <summary>
        /// 设置冷却缩短（天赋系统调用）。
        /// </summary>
        public void SetCooldownReduction(float reduction)
        {
            _cooldownReduction = Mathf.Clamp01(reduction);
        }

        /// <summary>
        /// 设置范围扩大修饰器（天赋系统调用）。
        /// 引用：技能系统.md §4.6 范围扩大天赋
        /// </summary>
        public void SetRangeMultiplier(byte playerId, float multiplier)
        {
            _rangeMultiplier[playerId] = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// 设置护盾强化标志（天赋系统调用）。
        /// 引用：技能系统.md §4.6 护盾强化天赋
        /// </summary>
        public void SetShieldActive(bool active)
        {
            _shieldActive = active;
        }

        /// <summary>
        /// 设置双重释放概率（天赋系统调用）。
        /// 引用：技能系统.md §4.6 双重释放天赋
        /// </summary>
        public void SetDoubleCastChance(byte playerId, float chance)
        {
            _doubleCastChance[playerId] = Mathf.Clamp01(chance);
        }

        /// <summary>
        /// 查询护盾减伤比例（供 ShelterSystem 调用以减少伤害）。
        /// 引用：技能系统.md §4.5 寒霜护盾/火焰护盾
        /// </summary>
        /// <param name="playerId">玩家ID（0=Aqua, 1=Ignis）</param>
        /// <returns>减伤比例（0=无护盾，0.5=减少50%伤害）</returns>
        public float GetShieldReduction(byte playerId)
        {
            if (_shieldOwner == playerId && _shieldRemainingTime > 0f)
                return _shieldReduction;
            return 0f;
        }

        // ──────────────────────────────────────────────
        //  被动技能
        // ──────────────────────────────────────────────

        /// <summary>
        /// 注册被动技能。
        /// 引用：技能系统.md §4.2 被动技能
        /// </summary>
        public void RegisterPassive(byte playerId, PassiveSkillType passive)
        {
            if (!_activePassives.TryGetValue(playerId, out HashSet<PassiveSkillType> set))
            {
                set = new HashSet<PassiveSkillType>();
                _activePassives[playerId] = set;
            }

            if (set.Add(passive))
            {
                Debug.Log($"[SkillSystem] 玩家{playerId} 注册被动技能: {passive}");
            }
        }

        /// <summary>
        /// 查询被动技能是否激活（供 FragmentSystem 调用）。
        /// 引用：技能系统.md §4.2 被动技能触发时机
        /// </summary>
        public bool IsPassiveActive(byte playerId, PassiveSkillType passive)
        {
            if (_activePassives.TryGetValue(playerId, out HashSet<PassiveSkillType> set))
                return set.Contains(passive);
            return false;
        }

        // ──────────────────────────────────────────────
        //  卡牌抽取
        // ──────────────────────────────────────────────

        /// <summary>
        /// 抽取卡牌（游戏开始时调用）。
        /// </summary>
        public List<SkillData> DrawCards(CharacterType owner, SkillType type)
        {
            EnsureConfigLoaded();

            if (_config == null)
            {
                Debug.LogWarning("[SkillSystem] SkillConfig 未加载");
                return new List<SkillData>();
            }

            List<SkillData> pool = GetPool(owner, type);
            return DrawFromPool(pool, 3, _config.DrawWeights);
        }

        // ──────────────────────────────────────────────
        //  选择卡牌
        // ──────────────────────────────────────────────

        /// <summary>
        /// 选择卡牌。
        /// </summary>
        public void SelectCard(CharacterType owner, SkillType type, int skillId)
        {
            // 被动技能类型 → 注册被动技能
            if (type == SkillType.Passive)
            {
                SelectPassiveCard(owner, skillId);
                return;
            }

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
        /// 选择被动技能卡牌，注册到被动技能集合。
        /// </summary>
        private void SelectPassiveCard(CharacterType owner, int skillId)
        {
            byte playerId = (byte)(owner == CharacterType.Aqua ? 0 : 1);

            // 根据 skillId 映射到 PassiveSkillType
            // 水人 → FrostAura, 火人 → FlameAura
            PassiveSkillType passive = owner == CharacterType.Aqua
                ? PassiveSkillType.FrostAura
                : PassiveSkillType.FlameAura;

            RegisterPassive(playerId, passive);
            Debug.Log($"[SkillSystem] {owner} 选定被动技能: {passive}");
        }

        // ──────────────────────────────────────────────
        //  释放技能
        // ──────────────────────────────────────────────

        /// <summary>
        /// 释放技能。
        /// 引用：技能系统.md §4.3 技能释放流程
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

            // ── 应用天赋修饰器 ──
            float range = skill.Data.Range;
            if (_rangeMultiplier.TryGetValue(playerId, out float rangeBonus) && rangeBonus > 0f)
                range *= (1f + rangeBonus);

            float duration = skill.Data.Duration;
            // 护盾强化天赋：护盾类技能持续时间 +50%
            if (_shieldActive && skill.Data.EffectType == SkillEffectType.Shield)
                duration *= 1.5f;

            // ── 执行技能效果 ──
            ExecuteSkillEffect(skill.Data, owner, targetPosition, range, duration);

            // ── 双重释放判定（仅 E 技能） ──
            if (type == SkillType.E
                && _doubleCastChance.TryGetValue(playerId, out float chance)
                && chance > 0f
                && Random.Range(0f, 1f) <= chance)
            {
                // 第二次释放，效果×0.5
                SkillData halfSkill = CreateHalfEffectSkill(skill.Data);
                ExecuteSkillEffect(halfSkill, owner, targetPosition, range * 0.5f, duration * 0.5f);
                Debug.Log($"[SkillSystem] 双重释放触发！第二次效果×0.5");
            }

            // ── 发布技能释放事件 ──
            EventBus.Instance.Publish(new SkillActivatedEvent
            {
                skillId = skill.Data.SkillId,
                playerId = playerId,
                targetPos = targetPosition
            });

            // ── 设置冷却 ──
            float cooldown = skill.Data.Cooldown * (1f - _cooldownReduction);
            skill.CooldownRemaining = cooldown;

            Debug.Log($"[SkillSystem] {owner} 释放{type}技能: {skill.Data.Name}, " +
                      $"效果类型: {skill.Data.EffectType}, 冷却{cooldown}s" +
                      (cooldown > 60f ? " (跨轮冷却)" : ""));
        }

        // ──────────────────────────────────────────────
        //  技能效果执行
        // ──────────────────────────────────────────────

        /// <summary>
        /// 根据 SkillEffectType 分发执行不同效果。
        /// 引用：技能系统.md §4.3 / §4.5
        /// </summary>
        private void ExecuteSkillEffect(SkillData skill, CharacterType owner,
            Vector2 targetPosition, float range, float duration)
        {
            float multiplier = skill.EffectMultiplier;
            // Q 技能效果更强
            if (skill.Type == SkillType.Q)
                multiplier *= 1.5f;

            switch (skill.EffectType)
            {
                case SkillEffectType.Damage:
                    ExecuteDamageEffect(skill, owner, targetPosition, range, multiplier);
                    break;

                case SkillEffectType.Freeze:
                    ExecuteFreezeEffect(skill, targetPosition, range, multiplier);
                    break;

                case SkillEffectType.Shield:
                    ExecuteShieldEffect(skill, owner, duration, multiplier);
                    break;

                case SkillEffectType.SpeedBoost:
                    ExecuteSpeedBoostEffect(skill, owner, duration, multiplier);
                    break;

                case SkillEffectType.Heal:
                    ExecuteHealEffect(skill, owner, multiplier);
                    break;

                default:
                    Debug.LogWarning($"[SkillSystem] 未知技能效果类型: {skill.EffectType}");
                    break;
            }
        }

        /// <summary>
        /// 伤害型效果：对范围内灾难造成伤害/停止灾难。
        /// 引用：技能系统.md §4.5 技能与庇护系统交互
        /// </summary>
        private void ExecuteDamageEffect(SkillData skill, CharacterType owner,
            Vector2 targetPosition, float range, float multiplier)
        {
            var disasterSys = ServiceLocator.Get<IDisasterSystem>();
            if (disasterSys != null && disasterSys.CurrentDisaster != null
                && disasterSys.CurrentDisaster.IsRunning)
            {
                // 灾难存在范围，检查目标是否在灾难影响范围内
                float disasterRange = disasterSys.CurrentDisaster.Params.Range;
                float distanceToDisaster = Vector2.Distance(targetPosition, Vector2.zero);

                if (distanceToDisaster <= disasterRange + range)
                {
                    // 伤害计算：基础伤害 × 效果系数
                    float damage = 50f * multiplier;
                    Debug.Log($"[SkillSystem] 技能{skill.Name} 对灾难造成{damage}伤害 " +
                              $"(灾难DPS={disasterSys.CurrentDisaster.Params.BaseDPS})");

                    // 原型阶段：伤害型技能直接停止灾难
                    // TODO: 后续应根据伤害量减少灾难剩余HP/持续时间
                    disasterSys.StopDisaster();
                }
            }

            // 伤害型技能同时可为范围内建筑提供修缮效果（正向效果）
            var buildSys = ServiceLocator.Get<IBuildSystem>();
            if (buildSys != null && buildSys.Buildings != null)
            {
                int repairedCount = 0;
                float healAmount = 10f * multiplier;

                foreach (var building in buildSys.Buildings)
                {
                    if (building == null) continue;

                    // 将网格坐标近似为世界坐标（1格=1单位）
                    Vector2 buildingPos = new Vector2(building.GridPosition.x, building.GridPosition.y);
                    if (Vector2.Distance(buildingPos, targetPosition) <= range)
                    {
                        building.CurrentHP = Mathf.Min(building.BaseHP, building.CurrentHP + healAmount);
                        repairedCount++;
                    }
                }

                if (repairedCount > 0)
                    Debug.Log($"[SkillSystem] 技能{skill.Name} 修缮了{repairedCount}座建筑 (+{healAmount}HP)");
            }
        }

        /// <summary>
        /// 冻结型效果：冻结区域内碎片（设置 FragmentState.Frozen）。
        /// 引用：技能系统.md §4.2 被动技能 / 碎片系统 §4.4 温砖触发
        /// </summary>
        private void ExecuteFreezeEffect(SkillData skill,
            Vector2 targetPosition, float range, float multiplier)
        {
            FragmentController[] fragments = FindObjectsOfType<FragmentController>();
            int frozenCount = 0;

            foreach (var fragment in fragments)
            {
                if (fragment == null || fragment.State != FragmentState.Falling)
                    continue;

                float dist = Vector2.Distance(fragment.transform.position, targetPosition);
                if (dist <= range)
                {
                    fragment.SetState(FragmentState.Frozen);
                    frozenCount++;
                }
            }

            Debug.Log($"[SkillSystem] 技能{skill.Name} 冻结了{frozenCount}个碎片" +
                      (frozenCount == 0 ? "（范围内无可用碎片）" : ""));
        }

        /// <summary>
        /// 护盾型效果：为角色添加临时护盾（减少受到的伤害）。
        /// 引用：技能系统.md §4.5 寒霜护盾/火焰护盾
        /// </summary>
        private void ExecuteShieldEffect(SkillData skill, CharacterType owner,
            float duration, float multiplier)
        {
            byte playerId = (byte)(owner == CharacterType.Aqua ? 0 : 1);

            _shieldOwner = playerId;
            _shieldRemainingTime = duration;
            // 减伤比例：基础50%，效果系数影响
            _shieldReduction = Mathf.Clamp01(0.5f * multiplier);

            Debug.Log($"[SkillSystem] {owner} 获得护盾，持续{duration}s，减伤{_shieldReduction * 100}%" +
                      (_shieldActive ? " (护盾强化天赋激活)" : ""));
        }

        /// <summary>
        /// 加速型效果：临时提升角色移速。
        /// 引用：技能系统.md §4.3 技能释放
        /// </summary>
        private void ExecuteSpeedBoostEffect(SkillData skill, CharacterType owner,
            float duration, float multiplier)
        {
            var charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys == null) return;

            CharacterController character = charSys.GetCharacter(owner);
            if (character == null || character.Stats == null) return;

            // 如果已有加速效果，先恢复原始移速
            if (_speedBoostOwner != 0xFF && _speedBoostRemainingTime > 0f)
                RestoreMoveSpeed();

            byte playerId = (byte)(owner == CharacterType.Aqua ? 0 : 1);
            _speedBoostOwner = playerId;
            _speedBoostRemainingTime = duration;
            _originalMoveSpeed = character.Stats.MoveSpeed;

            // 移速提升：基础+50%，效果系数影响
            float boost = 0.5f * multiplier;
            character.Stats.MoveSpeed = _originalMoveSpeed * (1f + boost);

            Debug.Log($"[SkillSystem] {owner} 获得加速，移速 +{boost * 100}%，持续{duration}s");
        }

        /// <summary>
        /// 治疗型效果：恢复角色HP。
        /// 引用：技能系统.md §4.3 技能释放
        /// </summary>
        private void ExecuteHealEffect(SkillData skill, CharacterType owner, float multiplier)
        {
            var shelterSys = ServiceLocator.Get<IShelterSystem>();
            if (shelterSys == null)
            {
                Debug.LogWarning("[SkillSystem] ShelterSystem 未注册，无法治疗");
                return;
            }

            // 治疗量：基础20HP，效果系数影响
            int healAmount = Mathf.RoundToInt(20f * multiplier);
            shelterSys.Heal(owner, healAmount);

            Debug.Log($"[SkillSystem] {owner} 恢复{healAmount}HP (技能: {skill.Name})");
        }

        // ──────────────────────────────────────────────
        //  每帧更新
        // ──────────────────────────────────────────────

        /// <summary>
        /// 每帧更新（冷却倒计时 + 临时效果计时）。
        /// 引用：技能系统.md §4.4 冷却管理
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            // 冷却递减（所有阶段持续，含修整/升级）
            UpdateCooldown(AquaESkill, deltaTime);
            UpdateCooldown(AquaQSkill, deltaTime);
            UpdateCooldown(IgnisESkill, deltaTime);
            UpdateCooldown(IgnisQSkill, deltaTime);

            // 护盾效果计时
            if (_shieldRemainingTime > 0f)
            {
                _shieldRemainingTime -= deltaTime;
                if (_shieldRemainingTime <= 0f)
                {
                    _shieldRemainingTime = 0f;
                    _shieldOwner = 0xFF;
                    Debug.Log("[SkillSystem] 护盾效果结束");
                }
            }

            // 加速效果计时
            if (_speedBoostRemainingTime > 0f)
            {
                _speedBoostRemainingTime -= deltaTime;
                if (_speedBoostRemainingTime <= 0f)
                {
                    RestoreMoveSpeed();
                    Debug.Log("[SkillSystem] 加速效果结束");
                }
            }
        }

        private void UpdateCooldown(SkillState skill, float dt)
        {
            if (skill != null && skill.CooldownRemaining > 0f)
                skill.CooldownRemaining = Mathf.Max(0f, skill.CooldownRemaining - dt);
        }

        // ──────────────────────────────────────────────
        //  阶段切换回调（跨轮冷却）
        // ──────────────────────────────────────────────

        /// <summary>
        /// 阶段切换事件回调。
        /// 引用：技能系统.md §4.4 Q技能跨轮冷却规则
        /// </summary>
        private void OnPhaseChanged(PhaseChangedEvent evt)
        {
            GamePhase previousPhase = _currentPhase;
            _currentPhase = evt.phase;

            // 修整和升级阶段冷却继续递减（由 Update 驱动，无需额外处理）
            // 此处仅做日志记录，便于调试跨轮冷却
            if (evt.phase == GamePhase.Rest)
            {
                Debug.Log("[SkillSystem] 进入修整阶段，冷却继续递减");
            }
            else if (evt.phase == GamePhase.Upgrade)
            {
                Debug.Log("[SkillSystem] 进入升级阶段，冷却继续递减");
            }
            else if (evt.phase == GamePhase.Preview && previousPhase == GamePhase.Upgrade)
            {
                // 新轮次开始 — 冷却不重置，跨轮继续累积
                LogCrossRoundCooldown();
            }
        }

        /// <summary>
        /// 记录跨轮冷却状态（调试用）。
        /// </summary>
        private void LogCrossRoundCooldown()
        {
            if (AquaQSkill != null && AquaQSkill.CooldownRemaining > 0f)
                Debug.Log($"[SkillSystem] 水人Q技能跨轮冷却剩余: {AquaQSkill.CooldownRemaining}s");
            if (IgnisQSkill != null && IgnisQSkill.CooldownRemaining > 0f)
                Debug.Log($"[SkillSystem] 火人Q技能跨轮冷却剩余: {IgnisQSkill.CooldownRemaining}s");
            if (AquaESkill != null && AquaESkill.CooldownRemaining > 0f)
                Debug.Log($"[SkillSystem] 水人E技能跨轮冷却剩余: {AquaESkill.CooldownRemaining}s");
            if (IgnisESkill != null && IgnisESkill.CooldownRemaining > 0f)
                Debug.Log($"[SkillSystem] 火人E技能跨轮冷却剩余: {IgnisESkill.CooldownRemaining}s");
        }

        // ──────────────────────────────────────────────
        //  辅助方法
        // ──────────────────────────────────────────────

        /// <summary>
        /// 恢复角色原始移速（加速效果结束时调用）。
        /// </summary>
        private void RestoreMoveSpeed()
        {
            if (_speedBoostOwner == 0xFF)
                return;

            var charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys != null)
            {
                CharacterType owner = _speedBoostOwner == 0 ? CharacterType.Aqua : CharacterType.Ignis;
                CharacterController character = charSys.GetCharacter(owner);
                if (character != null && character.Stats != null)
                    character.Stats.MoveSpeed = _originalMoveSpeed;
            }

            _speedBoostOwner = 0xFF;
            _speedBoostRemainingTime = 0f;
        }

        /// <summary>
        /// 创建效果减半的技能数据副本（双重释放第二次使用）。
        /// 引用：技能系统.md §4.6 双重释放
        /// </summary>
        private SkillData CreateHalfEffectSkill(SkillData original)
        {
            return new SkillData
            {
                SkillId = original.SkillId,
                Name = original.Name + " (双重)",
                Type = original.Type,
                Rarity = original.Rarity,
                EffectMultiplier = original.EffectMultiplier * 0.5f,
                Cooldown = original.Cooldown,
                Duration = original.Duration,
                Range = original.Range,
                Description = original.Description,
                Owner = original.Owner,
                EffectType = original.EffectType,
            };
        }

        /// <summary>
        /// 确保技能配置已加载（使用 DataManager）。
        /// </summary>
        private void EnsureConfigLoaded()
        {
            if (_config != null) return;

            _config = DataManager.Instance.LoadConfig<SkillConfig>();

            if (_config == null)
                Debug.LogWarning("[SkillSystem] SkillConfig 通过 DataManager 加载失败");
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
