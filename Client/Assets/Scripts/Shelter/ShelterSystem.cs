/// ============================================================
/// 文件名: ShelterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护系统管理器，管理能量、扣血、双生庇护距离检测。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Character;
using DualEnigma.Building;
using DualEnigma.Data;

namespace DualEnigma.Shelter
{
    /// <summary>
    /// 庇护系统管理器。继承 Singleton<T>，注册 IShelterSystem 到 ServiceLocator。
    /// 引用：庇护系统.md §3.1
    /// </summary>
    public class ShelterSystem : Singleton<ShelterSystem>, IShelterSystem
    {
        private const int MAX_HP = 100;

        /// <summary>水人能量</summary>
        private float _aquaEnergy = 100f;
        /// <summary>火人能量</summary>
        private float _ignisEnergy = 100f;
        /// <summary>水人HP</summary>
        private int _aquaHP = MAX_HP;
        /// <summary>火人HP</summary>
        private int _ignisHP = MAX_HP;
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

        public float AquaEnergy => _aquaEnergy;
        public float IgnisEnergy => _ignisEnergy;
        public int AquaHP => _aquaHP;
        public int IgnisHP => _ignisHP;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IShelterSystem>(this);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);

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

            Debug.Log("[ShelterSystem] 庇护系统初始化完成");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EventBus.HasInstance)
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
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
            if (GameManager.Instance.State.IsGameOver)
                return;

            Vector2 aquaPos, ignisPos;
            GetCharacterPositions(out aquaPos, out ignisPos);

            float distance = Vector2.Distance(aquaPos, ignisPos);
            float shelterDist = _isFragmentCollectPhase
                ? _params.FragmentCollectDistance
                : _params.ShelterDistance;
            float consumptionRate = _isFragmentCollectPhase
                ? _params.FragmentCollectConsumptionRate
                : _params.ConsumptionRate;

            bool inRange = distance <= shelterDist;

            UpdateEnergy(ref _aquaEnergy, inRange, deltaTime, consumptionRate);
            UpdateEnergy(ref _ignisEnergy, inRange, deltaTime, consumptionRate);

            UpdateBufferAndDamage(
                _aquaEnergy, ref _aquaBuffering, ref _aquaBufferTimer,
                deltaTime, CharacterType.Aqua, true, aquaPos);
            UpdateBufferAndDamage(
                _ignisEnergy, ref _ignisBuffering, ref _ignisBufferTimer,
                deltaTime, CharacterType.Ignis, false, ignisPos);
        }

        /// <summary>
        /// 角色受伤。ShelterSystem 为HP唯一权威，修改后同步至 CharacterController。
        /// </summary>
        public void DealDamage(CharacterType target, int damage)
        {
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
                _aquaHP = Mathf.Min(MAX_HP, _aquaHP + amount);
                SyncHPToCharacter(target);
            }
            else
            {
                _ignisHP = Mathf.Min(MAX_HP, _ignisHP + amount);
                SyncHPToCharacter(target);
            }
        }

        /// <summary>
        /// 修改庇护参数（天赋系统调用）。
        /// </summary>
        public void ModifyParams(ShelterParams modifications)
        {
            if (modifications == null) return;

            _params.MaxEnergy += modifications.MaxEnergy;
            _params.RecoveryRate *= (1f + modifications.RecoveryRate / 20f);
            _params.ShelterDistance += modifications.ShelterDistance;
            _params.DamageMultiplier *= modifications.DamageMultiplier;
            _params.DamageMultiplier = Mathf.Max(_params.DamageMultiplier, 0.1f);
        }

        /// <summary>
        /// 设置 M5 庇护削弱状态。
        /// 开启后恢复速率减半、消耗速率增加50%。
        /// </summary>
        public void SetM5Weakening(bool enabled)
        {
            _m5Weakening = enabled;
            Debug.Log($"[ShelterSystem] M5庇护削弱: {(_m5Weakening ? "开启" : "关闭")}");
        }

        private void UpdateEnergy(ref float energy, bool inRange, float dt, float consumptionRate)
        {
            float recoveryRate = _m5Weakening ? _params.RecoveryRate * 0.5f : _params.RecoveryRate;
            float actualConsumptionRate = _m5Weakening ? consumptionRate * 1.5f : consumptionRate;

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

            float damageRate = GetEnvironmentDamageRate(type);
            if (damageRate <= 0f)
                return;

            // 陨石环境：掩体内0%概率，掩体外50%概率扣血
            if (CurrentEnvironment == ShelterEnvironment.Meteorite)
            {
                if (IsInBuildingZone(charPos))
                    return; // 掩体内不扣血
                if (Random.Range(0f, 1f) >= 0.5f)
                    return; // 50%概率不扣血
            }

            // 灾害冲击阶段，建筑区域内扣血减免50%
            if (_currentPhase == GamePhase.DisasterImpact && IsInBuildingZone(charPos))
                damageRate *= 0.5f;

            float multiplier = _params.DamageMultiplier;
            int currentHP = isAqua ? _aquaHP : _ignisHP;
            if (currentHP <= _dyingProtectThreshold)
                multiplier *= (1f - _dyingProtectReduction);

            float damage = damageRate * multiplier * dt;
            int intDamage = Mathf.CeilToInt(damage);
            if (intDamage > 0)
                DealDamage(type, intDamage);
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
            IBuildSystem buildSys = ServiceLocator.Get<IBuildSystem>();
            if (buildSys == null || buildSys.Buildings.Count == 0)
                return false;

            const float proximityThreshold = 2.5f;
            foreach (var building in buildSys.Buildings)
            {
                Vector2 buildingPos = new Vector2(building.GridPosition.x, building.GridPosition.y);
                if (Vector2.Distance(worldPos, buildingPos) < proximityThreshold)
                    return true;
            }
            return false;
        }

        private void GetCharacterPositions(out Vector2 aqua, out Vector2 ignis)
        {
            ICharacterSystem charSys = ServiceLocator.Get<ICharacterSystem>();
            if (charSys != null)
            {
                aqua = charSys.GetCharacter(CharacterType.Aqua) != null
                    ? charSys.GetCharacter(CharacterType.Aqua).transform.position
                    : Vector2.zero;
                ignis = charSys.GetCharacter(CharacterType.Ignis) != null
                    ? charSys.GetCharacter(CharacterType.Ignis).transform.position
                    : Vector2.zero;
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
            _aquaHP = MAX_HP;
            _ignisHP = MAX_HP;
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
