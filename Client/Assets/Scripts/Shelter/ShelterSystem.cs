/// ============================================================
/// 文件名: ShelterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护系统管理器，管理能量、扣血、双生庇护距离检测。
/// ============================================================

using UnityEngine;
using System.Collections.Generic;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Building;
using DualEnigma.Data;
using DualEnigma.Disaster;
using DualEnigma.Skill;
using CharacterController = DualEnigma.Character.CharacterController;
namespace DualEnigma.Shelter
{
    /// <summary>
    /// 庇护系统管理器。继承 Singleton<T>，注册 IShelterSystem 到 ServiceLocator。
    /// 引用：庇护系统.md §3.1
    /// </summary>
    public class ShelterSystem : Singleton<ShelterSystem>, IShelterSystem
    {
        private int _maxHP = 100;

        /// <summary>水人能量</summary>
        private float _aquaEnergy = 100f;
        /// <summary>火人能量</summary>
        private float _ignisEnergy = 100f;
        /// <summary>水人HP</summary>
        private int _aquaHP = 100;
        /// <summary>火人HP</summary>
        private int _ignisHP = 100;
        /// <summary>水人缓冲计时器</summary>
        private float _aquaBufferTimer;
        /// <summary>火人缓冲计时器</summary>
        private float _ignisBufferTimer;
        /// <summary>是否在缓冲期</summary>
        private bool _aquaBuffering;
        private bool _ignisBuffering;

        /// <summary>当前庇护环境</summary>
        public ShelterEnvironment CurrentEnvironment { get; private set; }

        /// <summary>庇护参数</summary>
        private ShelterParams _params = new ShelterParams();

        /// <summary>是否处于碎片收集阶段</summary>
        private bool _isFragmentCollectPhase;

        /// <summary>M5庇护削弱标记</summary>
        private bool _m5Weakening = false;

        /// <summary>当前游戏阶段</summary>
        private GamePhase _currentPhase = GamePhase.Preview;

        /// <summary>环境扣血速率（从配置加载，索引对应 ShelterEnvironment 枚举）</summary>
        private float[] _environmentDamageRates = { 3f, 3f, 2f, 3f, 3f };

        /// <summary>濒死保护阈值（HP%以下减伤）</summary>
        private float _dyingProtectThreshold = 30f;

        /// <summary>濒死保护减伤比例</summary>
        private float _dyingProtectReduction = 0.3f;

        /// <summary>章节恢复HP</summary>
        private int _chapterRestoreHP = 15;

        private float _earthquakeShockwaveTimer = 0f;
        private const float EARTHQUAKE_SHOCKWAVE_INTERVAL = 2f;
        private const int EARTHQUAKE_DAMAGE_PER_WAVE = 3;

        private readonly HashSet<Vector2Int> _buildingGridPositions = new HashSet<Vector2Int>();
        private bool _buildingPositionsDirty = true;

        private System.Random _meteoriteRandom;

        public float AquaEnergy => _aquaEnergy;
        public float IgnisEnergy => _ignisEnergy;
        public int AquaHP => _aquaHP;
        public int IgnisHP => _ignisHP;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IShelterSystem>(this);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Instance.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
            EventBus.Instance.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
            EventBus.Instance.Subscribe<DisasterStartedEvent>(OnDisasterStarted);

            // 从 ShelterConfig 加载参数
            var config = DataManager.Instance.LoadConfig<ShelterConfig>("ShelterConfig");
            if (config != null && config.Params != null)
            {
                _params = new ShelterParams
                {
                    MaxEnergy = config.Params.MaxEnergy,
                    RecoveryRate = config.Params.RecoveryRate,
                    ConsumptionRate = config.Params.ConsumptionRate,
                    ShelterDistance = config.Params.ShelterDistance,
                    FragmentCollectDistance = config.Params.FragmentCollectDistance,
                    FragmentCollectConsumptionRate = config.Params.FragmentCollectConsumptionRate,
                    DamageMultiplier = config.Params.DamageMultiplier,
                    BufferTime = config.Params.BufferTime
                };

                if (config.EnvironmentDamageRates != null && config.EnvironmentDamageRates.Length >= 5)
                {
                    _environmentDamageRates = (float[])config.EnvironmentDamageRates.Clone();
                    // 陨石基础速率不应为0（概率判定需要基础速率）
                    if (_environmentDamageRates[(int)ShelterEnvironment.Meteorite] <= 0f)
                        _environmentDamageRates[(int)ShelterEnvironment.Meteorite] = 3f;
                }
                _dyingProtectThreshold = config.DyingProtectThreshold;
                _dyingProtectReduction = config.DyingProtectReduction;
                _chapterRestoreHP = config.ChapterRestoreHP;
            }
            else
            {
                _params = new ShelterParams();
            }

            CharacterConfig charConfig = DataManager.Instance.LoadConfig<CharacterConfig>("CharacterConfig");
            if (charConfig != null)
                _maxHP = charConfig.AquaStats.MaxHP;

            Debug.Log("[ShelterSystem] 庇护系统初始化完成");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
                EventBus.Instance.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
                EventBus.Instance.Unsubscribe<BuildingDestroyedEvent>(OnBuildingDestroyed);
                EventBus.Instance.Unsubscribe<DisasterStartedEvent>(OnDisasterStarted);
            }
        }

        /// <summary>
        /// 设置当前庇护环境。暴风雪环境降低水人移速50%，火人完全免疫。
        /// </summary>
        public void SetEnvironment(ShelterEnvironment environment)
        {
            // 退出暴风雪：恢复水人移速
            if (CurrentEnvironment == ShelterEnvironment.Blizzard
                && environment != ShelterEnvironment.Blizzard)
            {
                SetCharacterMoveSpeed(CharacterType.Aqua, 1f);
            }

            CurrentEnvironment = environment;

            // 进入暴风雪：水人移速降至50%（火人完全免疫）
            if (environment == ShelterEnvironment.Blizzard)
            {
                SetCharacterMoveSpeed(CharacterType.Aqua, 0.5f);
            }

            Debug.Log($"[ShelterSystem] 庇护环境切换 → {environment}");
        }

        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            if (GameManager.HasInstance && GameManager.Instance.State.IsGameOver)
                return;

            bool energyActive = _currentPhase == GamePhase.FragmentCollect
                             || _currentPhase == GamePhase.DisasterPreview
                             || _currentPhase == GamePhase.DisasterImpact;

            Vector2 aquaPos, ignisPos;
            GetCharacterPositions(out aquaPos, out ignisPos);

            float distance = Vector2.Distance(aquaPos, ignisPos);
            float shelterDist = _isFragmentCollectPhase
                ? _params.FragmentCollectDistance
                : _params.ShelterDistance;

            bool inRange = distance <= shelterDist;

            if (energyActive)
            {
                float consumptionRate = _isFragmentCollectPhase
                    ? _params.FragmentCollectConsumptionRate
                    : _params.ConsumptionRate;
                UpdateEnergy(ref _aquaEnergy, inRange, deltaTime, consumptionRate);
                UpdateEnergy(ref _ignisEnergy, inRange, deltaTime, consumptionRate);

                UpdateBufferAndDamage(
                    _aquaEnergy, ref _aquaBuffering, ref _aquaBufferTimer,
                    deltaTime, CharacterType.Aqua, true, aquaPos);
                UpdateBufferAndDamage(
                    _ignisEnergy, ref _ignisBuffering, ref _ignisBufferTimer,
                    deltaTime, CharacterType.Ignis, false, ignisPos);
            }
            else
            {
                UpdateEnergy(ref _aquaEnergy, true, deltaTime, 0f);
                UpdateEnergy(ref _ignisEnergy, true, deltaTime, 0f);
            }
        }

        /// <summary>
        /// 角色受伤。ShelterSystem 为HP唯一权威，修改后同步至 CharacterController。
        /// </summary>
        public void DealDamage(CharacterType target, int damage)
        {
            ISkillSystem skillSys = ServiceLocator.Get<ISkillSystem>();
            if (skillSys != null)
            {
                float shieldReduction = skillSys.GetShieldReduction((byte)target);
                damage = Mathf.RoundToInt(damage * (1f - shieldReduction));
            }

            if (damage <= 0) return;

            if (target == CharacterType.Aqua)
            {
                _aquaHP = Mathf.Max(0, _aquaHP - damage);
                SyncHPToCharacter(target);
                EventBus.Instance.Publish(new PlayerDamagedEvent { playerId = 0, damage = damage });
                if (_aquaHP <= 0)
                    EventBus.Instance.Publish(new PlayerDiedEvent { playerId = 0 });
            }
            else
            {
                _ignisHP = Mathf.Max(0, _ignisHP - damage);
                SyncHPToCharacter(target);
                EventBus.Instance.Publish(new PlayerDamagedEvent { playerId = 1, damage = damage });
                if (_ignisHP <= 0)
                    EventBus.Instance.Publish(new PlayerDiedEvent { playerId = 1 });
            }
        }

        /// <summary>
        /// 角色治疗。修改后同步至 CharacterController。
        /// </summary>
        public void Heal(CharacterType target, int amount)
        {
            if (target == CharacterType.Aqua)
            {
                _aquaHP = Mathf.Min(_maxHP, _aquaHP + amount);
                SyncHPToCharacter(target);
            }
            else
            {
                _ignisHP = Mathf.Min(_maxHP, _ignisHP + amount);
                SyncHPToCharacter(target);
            }

            EventBus.Instance.Publish(new PlayerHealedEvent
            {
                playerId = (byte)(target == CharacterType.Aqua ? 0 : 1),
                amount = amount
            });
        }

        /// <summary>
        /// 修改庇护参数（天赋系统调用）。
        /// </summary>
        public void ModifyParams(ShelterParams newParams)
        {
            if (newParams == null) return;

            _params.MaxEnergy = newParams.MaxEnergy;
            _params.RecoveryRate = newParams.RecoveryRate;
            _params.ShelterDistance = newParams.ShelterDistance;
            _params.DamageMultiplier = Mathf.Max(newParams.DamageMultiplier, 0.1f);
        }

        /// <summary>
        /// 设置 M5 庇护削弱状态。
        /// 开启后恢复速率减半、消耗速率翻倍。
        /// </summary>
        public void SetM5Weakening(bool enabled)
        {
            _m5Weakening = enabled;
            Debug.Log($"[ShelterSystem] M5庇护削弱: {(_m5Weakening ? "开启" : "关闭")}");
        }

        private void UpdateEnergy(ref float energy, bool inRange, float dt, float consumptionRate)
        {
            float recoveryRate = _m5Weakening ? _params.RecoveryRate * 0.5f : _params.RecoveryRate;
            float actualConsumptionRate = _m5Weakening ? consumptionRate * 2f : consumptionRate;

            if (inRange)
            {
                energy = Mathf.Min(_params.MaxEnergy, energy + recoveryRate * dt);
            }
            else
            {
                energy = Mathf.Max(0f, energy - actualConsumptionRate * dt);
            }
        }

        private void UpdateBufferAndDamage(
            float energy, ref bool buffering, ref float bufferTimer,
            float dt, CharacterType type, bool isAqua, Vector2 charPos)
        {
            if (energy > 0f)
            {
                buffering = false;
                bufferTimer = 0f;
                return;
            }

            if (!buffering)
            {
                buffering = true;
                bufferTimer = _params.BufferTime;
                return;
            }

            bufferTimer -= dt;
            if (bufferTimer > 0f)
                return;

            if (CurrentEnvironment == ShelterEnvironment.Earthquake)
            {
                _earthquakeShockwaveTimer += dt;
                if (_earthquakeShockwaveTimer >= EARTHQUAKE_SHOCKWAVE_INTERVAL)
                {
                    _earthquakeShockwaveTimer = 0f;

                    float multiplier = _params.DamageMultiplier;
                    int currentHP = isAqua ? _aquaHP : _ignisHP;
                    if (currentHP <= _dyingProtectThreshold)
                        multiplier *= (1f - _dyingProtectReduction);

                    if (_currentPhase == GamePhase.DisasterImpact && IsInBuildingZone(charPos))
                        return;

                    int damage = Mathf.CeilToInt(EARTHQUAKE_DAMAGE_PER_WAVE * multiplier);
                    if (damage > 0)
                        DealDamage(type, damage);
                }
                return;
            }

            if (CurrentEnvironment == ShelterEnvironment.Meteorite)
            {
                if (IsInBuildingZone(charPos))
                    return;

                IDisasterSystem disasterSys = ServiceLocator.Get<IDisasterSystem>();
                float meteoriteChance;
                if (disasterSys != null)
                {
                    Vector2 disasterPos = disasterSys.GetDisasterPosition();
                    float distToDisaster = Vector2.Distance(charPos, disasterPos);

                    if (distToDisaster <= 1f)
                        meteoriteChance = 0.9f;
                    else if (distToDisaster <= 3f)
                        meteoriteChance = 0.25f;
                    else
                        meteoriteChance = 0.05f;
                }
                else
                {
                    meteoriteChance = 0.5f;
                }

                if (_meteoriteRandom != null && _meteoriteRandom.NextDouble() >= meteoriteChance)
                    return;

                float damageRate = GetEnvironmentDamageRate(type);
                if (damageRate <= 0f)
                    return;

                float multiplier = _params.DamageMultiplier;
                int currentHP = isAqua ? _aquaHP : _ignisHP;
                if (currentHP <= _dyingProtectThreshold)
                    multiplier *= (1f - _dyingProtectReduction);

                float damage = damageRate * multiplier * dt;
                int intDamage = Mathf.CeilToInt(damage);
                if (intDamage > 0)
                    DealDamage(type, intDamage);
                return;
            }

            float baseDamageRate = GetEnvironmentDamageRate(type);
            if (baseDamageRate <= 0f)
                return;

            if (_currentPhase == GamePhase.DisasterImpact && IsInBuildingZone(charPos))
                return;

            float dmgMultiplier = _params.DamageMultiplier;
            int hp = isAqua ? _aquaHP : _ignisHP;
            if (hp <= _dyingProtectThreshold)
                dmgMultiplier *= (1f - _dyingProtectReduction);

            float continuousDamage = baseDamageRate * dmgMultiplier * dt;
            int intDmg = Mathf.CeilToInt(continuousDamage);
            if (intDmg > 0)
                DealDamage(type, intDmg);
        }

        private float GetEnvironmentDamageRate(CharacterType type)
        {
            int envIndex = (int)CurrentEnvironment;
            float baseRate = (envIndex >= 0 && envIndex < _environmentDamageRates.Length)
                ? _environmentDamageRates[envIndex]
                : 0f;

            switch (CurrentEnvironment)
            {
                case ShelterEnvironment.Volcano:
                    return type == CharacterType.Aqua ? baseRate : 0f;
                case ShelterEnvironment.Flood:
                    return type == CharacterType.Ignis ? baseRate : 0f;
                case ShelterEnvironment.Blizzard:
                    return type == CharacterType.Aqua ? baseRate : 0f;
                case ShelterEnvironment.Earthquake:
                    return baseRate;
                case ShelterEnvironment.Meteorite:
                    return baseRate;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 检查角色是否在建筑区域内（靠近任意已放置建筑）。
        /// 通过 IBuildSystem 查询建筑列表，判断角色世界坐标是否在建筑附近。
        /// </summary>
        private bool IsInBuildingZone(Vector2 worldPos)
        {
            if (_buildingPositionsDirty)
                RebuildBuildingPositions();

            if (_buildingGridPositions.Count == 0)
                return false;

            const float proximityThreshold = 2.5f;
            int radius = Mathf.CeilToInt(proximityThreshold);
            int centerX = Mathf.RoundToInt(worldPos.x);
            int centerY = Mathf.RoundToInt(worldPos.y);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var gridPos = new Vector2Int(centerX + dx, centerY + dy);
                    if (_buildingGridPositions.Contains(gridPos))
                    {
                        float dist = Vector2.Distance(worldPos, new Vector2(gridPos.x, gridPos.y));
                        if (dist < proximityThreshold)
                            return true;
                    }
                }
            }
            return false;
        }

        private void RebuildBuildingPositions()
        {
            _buildingGridPositions.Clear();
            IBuildSystem buildSys = ServiceLocator.Get<IBuildSystem>();
            if (buildSys != null)
            {
                foreach (var building in buildSys.Buildings)
                    _buildingGridPositions.Add(building.GridPosition);
            }
            _buildingPositionsDirty = false;
        }

        private void OnBuildingPlaced(BuildingPlacedEvent evt)
        {
            _buildingGridPositions.Add(evt.gridPos);
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent evt)
        {
            _buildingPositionsDirty = true;
        }

        private void OnDisasterStarted(DisasterStartedEvent evt)
        {
            IDisasterSystem disasterSys = ServiceLocator.Get<IDisasterSystem>();
            if (disasterSys != null && disasterSys.CurrentDisaster != null && disasterSys.CurrentDisaster.Params != null)
                _meteoriteRandom = new System.Random((int)disasterSys.CurrentDisaster.Params.RandomSeed);
            else
                _meteoriteRandom = new System.Random();
        }

        private void GetCharacterPositions(out Vector2 aqua, out Vector2 ignis)
        {
            ICharacterSystem charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys != null)
            {
                var aquaChar = charSys.GetCharacter(CharacterType.Aqua);
                aqua = aquaChar != null ? (Vector2)aquaChar.transform.position : Vector2.zero;

                var ignisChar = charSys.GetCharacter(CharacterType.Ignis);
                ignis = ignisChar != null ? (Vector2)ignisChar.transform.position : Vector2.zero;
            }
            else
            {
                aqua = Vector2.zero;
                ignis = Vector2.zero;
            }
        }

        private void OnPhaseChanged(PhaseChangedEvent evt)
        {
            _currentPhase = evt.phase;
            _isFragmentCollectPhase = (evt.phase == GamePhase.FragmentCollect);

            if (evt.phase == GamePhase.Rest)
            {
                int globalRound = GameManager.Instance.State.Progress.GlobalRound;
                if (globalRound % 12 == 0)
                {
                    Heal(CharacterType.Aqua, _chapterRestoreHP);
                    Heal(CharacterType.Ignis, _chapterRestoreHP);
                    Debug.Log($"[ShelterSystem] 章节结束恢复{_chapterRestoreHP}HP");
                }
            }
        }

        /// <summary>
        /// 将 HP 同步到 CharacterController.Stats.CurrentHP（保持显示一致）。
        /// ShelterSystem 为HP唯一权威，CharacterController.Stats.CurrentHP 仅用于显示。
        /// </summary>
        private void SyncHPToCharacter(CharacterType type)
        {
            ICharacterSystem charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys == null) return;

            CharacterController controller = charSys.GetCharacter(type);
            if (controller != null && controller.Stats != null)
            {
                controller.Stats.CurrentHP = type == CharacterType.Aqua ? _aquaHP : _ignisHP;
            }
        }

        /// <summary>
        /// 设置角色移动速度乘数（暴风雪环境调用）。
        /// </summary>
        private void SetCharacterMoveSpeed(CharacterType type, float multiplier)
        {
            ICharacterSystem charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys == null) return;

            CharacterController controller = charSys.GetCharacter(type);
            if (controller != null)
            {
                controller.SetMoveSpeedMultiplier(multiplier);
            }
        }

        /// <summary>
        /// 重置双方HP与能量为初始值（新局开始时由 GameManager 调用）。
        /// 同时恢复移速乘数，防止上一局暴风雪效果残留。
        /// </summary>
        public void ResetHP()
        {
            _aquaHP = _maxHP;
            _ignisHP = _maxHP;
            _aquaEnergy = _params.MaxEnergy;
            _ignisEnergy = _params.MaxEnergy;
            _aquaBuffering = false;
            _ignisBuffering = false;
            _aquaBufferTimer = 0f;
            _ignisBufferTimer = 0f;
            SetCharacterMoveSpeed(CharacterType.Aqua, 1f);
            SetCharacterMoveSpeed(CharacterType.Ignis, 1f);
            SyncHPToCharacter(CharacterType.Aqua);
            SyncHPToCharacter(CharacterType.Ignis);
            Debug.Log("[ShelterSystem] HP与能量已重置");
        }
    }
}
