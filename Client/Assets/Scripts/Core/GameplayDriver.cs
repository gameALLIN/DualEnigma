/// ============================================================
/// 文件名: GameplayDriver.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 对局流程编排器（客户端玩法循环闭环）。
///       订阅阶段/开局/结束/死亡事件，按 7 阶段时间线驱动：
///       Preview=推进关卡进度+选定本轮灾害+生成蓝图+切换三系统环境
///       FragmentCollect=单机本地掉落计划（联机走服务器下发）
///       DisasterImpact=启动灾害（双端按全局轮次种子确定性选择，无需服务器下发）
///       Rest=停止灾害+恢复普通环境+校正建筑HP
///       Build 阶段本地交互：Q=合成（碎片最多类型首配方）、E=在最近蓝图格建造
///       死亡 → EndGame(false)；3-4-3 轮打完 → EndGame(true)
/// 引用：GameManager, GameStateMachine, BuildingSystem, SynthesisSystem,
///       DisasterSystem, ShelterSystem, FragmentSystem, GridCoord
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Building;
using DualEnigma.Synthesis;
using DualEnigma.Disaster;
using DualEnigma.Shelter;
using DualEnigma.Fragment;
using DualEnigma.Network;
// 消除与 UnityEngine.CharacterController 的二义性
using CharacterController = DualEnigma.Character.CharacterController;

namespace DualEnigma.Core
{
    /// <summary>对局流程编排器：单机/联机共用的玩法循环驱动</summary>
    public class GameplayDriver : Singleton<GameplayDriver>
    {
        /// <summary>本轮选定的灾害（Preview 阶段确定性选出，全轮有效）</summary>
        private DisasterId _roundDisasterId;
        private DisasterCategory _roundCategory;
        private bool _disasterPicked;
        private bool _firstRound = true;

        /// <summary>建造交互：E 键放置的去抖</summary>
        private bool _buildKeyHeld;
        private bool _synthKeyHeld;

        protected override void OnSingletonInitialized()
        {
            EventBus.Instance.Subscribe<GameStartEvent>(OnGameStart);
            EventBus.Instance.Subscribe<RoomGameStartEvent>(OnRoomGameStart);
            EventBus.Instance.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Instance.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Instance.Subscribe<GameEndEvent>(OnGameEnd);
            Debug.Log("[GameplayDriver] 对局流程编排器初始化完成");
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<GameStartEvent>(OnGameStart);
                EventBus.Instance.Unsubscribe<RoomGameStartEvent>(OnRoomGameStart);
                EventBus.Instance.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
                EventBus.Instance.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
                EventBus.Instance.Unsubscribe<GameEndEvent>(OnGameEnd);
            }
            base.OnDestroy();
        }

        // ============================================================
        //  对局生命周期
        // ============================================================

        /// <summary>联机开局：服务器携带的关卡进度先于 GameStartEvent 到达，写入全局进度</summary>
        private void OnRoomGameStart(RoomGameStartEvent e)
        {
            GameProgress p = GameManager.Instance.State.Progress;
            p.Chapter = Mathf.Clamp(e.chapter, 1, 3);
            p.Section = Mathf.Clamp(e.section, 1, 4);
            p.Round = Mathf.Clamp(e.round, 1, 3);
        }

        private void OnGameStart(GameStartEvent e)
        {
            _firstRound = true;
            _disasterPicked = false;

            // 单机模式从 1-1-1 开始；联机进度已由 RoomGameStartEvent 写入
            if (!(RoomSession.HasInstance && RoomSession.Instance.IsConnected))
            {
                GameProgress p = GameManager.Instance.State.Progress;
                p.Chapter = 1;
                p.Section = 1;
                p.Round = 1;
            }

            // 上一局的蓝图/建筑清场（FragmentSystem 清场由其自身 GameEnd 订阅处理）
            if (BuildingSystem.HasInstance)
                BuildingSystem.Instance.ClearAll();
        }

        private void OnGameEnd(GameEndEvent e)
        {
            if (DisasterSystem.HasInstance)
                DisasterSystem.Instance.StopDisaster();
            _disasterPicked = false;
        }

        /// <summary>任一玩家死亡 → 对局失败（结算面板由 UIGameOver 订阅 GameEndEvent 弹出）</summary>
        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (GameManager.HasInstance && !GameManager.Instance.State.IsGameOver)
            {
                Debug.Log($"[GameplayDriver] 玩家{e.playerId}死亡，对局结束");
                GameManager.Instance.EndGame(false);
            }
        }

        // ============================================================
        //  阶段驱动（单机本地状态机与联机 ApplyServerPhase 都发布 PhaseChangedEvent）
        // ============================================================

        private void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (GameManager.HasInstance && GameManager.Instance.State.IsGameOver)
                return;

            switch (e.phase)
            {
                case GamePhase.Preview:
                    OnPreview();
                    break;

                case GamePhase.FragmentCollect:
                    OnFragmentCollect();
                    break;

                case GamePhase.DisasterImpact:
                    OnDisasterImpact();
                    break;

                case GamePhase.Rest:
                    OnRest();
                    break;
            }
        }

        /// <summary>Preview：推进关卡 + 选定灾害 + 生成蓝图 + 环境切换</summary>
        private void OnPreview()
        {
            GameProgress p = GameManager.Instance.State.Progress;

            // 首轮即 GameStart 设定的进度；此后每轮回 Preview 推进一轮
            if (_firstRound)
            {
                _firstRound = false;
            }
            else
            {
                if (p.Chapter >= 3 && p.Section >= 4 && p.Round >= 3)
                {
                    // 3章4节3轮打完 → 通关（双端本地一致判定，无需服务器下发）
                    GameManager.Instance.EndGame(true);
                    return;
                }
                GameManager.Instance.AdvanceToNextLevel();
            }

            // 碎片存续时间随轮次变化
            if (FragmentSystem.HasInstance)
                FragmentSystem.Instance.SetCurrentRound(p.Round);

            // 确定性选定本轮灾害：种子=全局轮次（双端一致）
            PickRoundDisaster(p.GlobalRound);
        }

        /// <summary>按全局轮次种子确定性选择灾害（类别+灾害ID），并完成蓝图生成与环境切换</summary>
        private void PickRoundDisaster(int globalRound)
        {
            System.Random rng = new System.Random(globalRound * 7919 + 13);

            // 六大类（与服务器灾害轮盘独立；客户端仅驱动本地表现与建筑/合成/庇护环境）
            _roundCategory = (DisasterCategory)rng.Next(0, 6);
            _roundDisasterId = PickDisasterId(rng, _roundCategory);
            _disasterPicked = true;

            // 环境映射与蓝图生成（GenerateBlueprint 内部完成类别→环境映射）
            if (BuildingSystem.HasInstance)
                BuildingSystem.Instance.GenerateBlueprint(_roundCategory,
                    GameManager.Instance.State.Progress.Round);

            // 环境同步给建造/合成/庇护三系统（合成表随环境切换）
            ShelterEnvironment env = BuildingSystem.HasInstance
                ? MapCategoryToEnvironment(_roundCategory)
                : ShelterEnvironment.Normal;
            ApplyEnvironment(env);

            Debug.Log($"[GameplayDriver] 第{globalRound}轮灾害选定: {_roundDisasterId} (类别{_roundCategory}, 环境{env})");
        }

        private static DisasterId PickDisasterId(System.Random rng, DisasterCategory category)
        {
            // DisasterId 编号：E1-E8=1-8, V1-V6=100-105, T1-T5=200-204,
            // S1-S5=300-304, P1-P5=400-404, M1-M6=500-505（百位数=类别）
            switch (category)
            {
                case DisasterCategory.Element:
                    return (DisasterId)(1 + rng.Next(0, 8));
                case DisasterCategory.Environment:
                    return (DisasterId)(100 + rng.Next(0, 6));
                case DisasterCategory.TimeSpace:
                    return (DisasterId)(200 + rng.Next(0, 5));
                case DisasterCategory.Perception:
                    return (DisasterId)(300 + rng.Next(0, 5));
                case DisasterCategory.Physics:
                    return (DisasterId)(400 + rng.Next(0, 5));
                case DisasterCategory.Mechanism:
                    return (DisasterId)(500 + rng.Next(0, 6));
                default:
                    return DisasterId.V1;
            }
        }

        /// <summary>类别→庇护环境（与 BuildingSystem.MapCategoryToEnvironment 同规则，Mechanism 固定取第 rng 项保证双端一致）</summary>
        private ShelterEnvironment MapCategoryToEnvironment(DisasterCategory category)
        {
            switch (category)
            {
                case DisasterCategory.Element: return ShelterEnvironment.Volcano;
                case DisasterCategory.Environment: return ShelterEnvironment.Flood;
                case DisasterCategory.TimeSpace: return ShelterEnvironment.Blizzard;
                case DisasterCategory.Perception: return ShelterEnvironment.Earthquake;
                case DisasterCategory.Physics: return ShelterEnvironment.Meteorite;
                case DisasterCategory.Mechanism: return ShelterEnvironment.Normal; // 机制类不改环境
                default: return ShelterEnvironment.Normal;
            }
        }

        /// <summary>环境切换广播到建造/合成/庇护三系统</summary>
        private void ApplyEnvironment(ShelterEnvironment env)
        {
            if (BuildingSystem.HasInstance)
                BuildingSystem.Instance.SetEnvironment(env);
            if (SynthesisSystem.HasInstance)
                SynthesisSystem.Instance.SetEnvironment(env);
            if (ShelterSystem.HasInstance)
                ShelterSystem.Instance.SetEnvironment(env);
        }

        /// <summary>碎片收集阶段：单机本地生成掉落计划（联机由服务器下发）</summary>
        private void OnFragmentCollect()
        {
            if (RoomSession.HasInstance && RoomSession.Instance.IsConnected)
                return; // 联机：等待 S2C_FragmentDropPlan

            if (!FragmentSystem.HasInstance) return;

            GameProgress p = GameManager.Instance.State.Progress;
            float density = p.DifficultyMultiplier;
            uint seed = unchecked((uint)(p.GlobalRound * 2654435761u));
            // GenerateDropPlan 入参为灾害 ID（内部 /100 得类别调整碎片概率）
            List<FragmentDropPlan> plan = FragmentSystem.Instance.GenerateDropPlan(
                (int)_roundDisasterId, density, seed);
            FragmentSystem.Instance.ExecuteDropPlan(plan);
        }

        /// <summary>灾害冲击阶段：启动灾害（本地表现+本地伤害判定）</summary>
        private void OnDisasterImpact()
        {
            if (!_disasterPicked || !DisasterSystem.HasInstance) return;

            GameProgress p = GameManager.Instance.State.Progress;
            DisasterSystem.Instance.StartDisaster(_roundDisasterId, p.DifficultyMultiplier,
                unchecked((uint)(p.GlobalRound * 40503u)));
        }

        /// <summary>修整阶段：停灾 + 恢复普通环境 + 校正建筑HP</summary>
        private void OnRest()
        {
            if (DisasterSystem.HasInstance)
                DisasterSystem.Instance.StopDisaster();

            ApplyEnvironment(ShelterEnvironment.Normal);

            if (BuildingSystem.HasInstance)
                BuildingSystem.Instance.SyncBuildingHPs();
        }

        // ============================================================
        //  建造阶段本地交互（Q=合成 E=建造，与 WASD/Space 移动不冲突）
        // ============================================================

        private void Update()
        {
            if (GameManager.HasInstance && (!GameManager.Instance.State.IsInGame || GameManager.Instance.State.IsGameOver))
                return;

            if (GameStateMachine.Instance.CurrentPhase != GamePhase.Build)
                return;

            // 联机仅本地角色可交互（远程角色由对方驱动）
            byte localId = RoomSession.HasInstance && RoomSession.Instance.IsConnected
                ? RoomSession.Instance.LocalPlayerId
                : (byte)0;
            CharacterController local = CharacterSystem.Instance.GetCharacter((CharacterType)localId);            if (local == null) return;

            // Q：合成（携带最多的碎片类型 → 该类型第一个可用配方）
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (!_synthKeyHeld)
                {
                    _synthKeyHeld = true;
                    TrySynthesize(localId);
                }
            }
            else _synthKeyHeld = false;

            // E：在最近的未完成蓝图格建造
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!_buildKeyHeld)
                {
                    _buildKeyHeld = true;
                    TryBuildAtNearest(localId, local);
                }
            }
            else _buildKeyHeld = false;
        }

        /// <summary>合成入口：选携带最多的碎片类型，取其第一条配方产出</summary>
        private void TrySynthesize(byte playerId)
        {
            SynthesisSystem synth = SynthesisSystem.Instance;
            IFragmentSystem fragSys = ServiceLocator.Get<IFragmentSystem>();
            CharacterController character = CharacterSystem.Instance.GetCharacter((CharacterType)playerId);
            if (synth == null || fragSys == null || character?.Stats == null) return;

            // 统计各类型携带数，取最多者
            FragmentType bestType = FragmentType.IceCrystal;
            int bestCount = 0;
            var counts = new Dictionary<FragmentType, int>();
            foreach (int id in character.Stats.CarriedFragmentIds)
            {
                if (!fragSys.TryGetFragmentType(id, out FragmentType t)) continue;
                counts.TryGetValue(t, out int c);
                counts[t] = c + 1;
                if (counts[t] > bestCount)
                {
                    bestCount = counts[t];
                    bestType = t;
                }
            }

            if (bestCount == 0)
            {
                Debug.Log("[GameplayDriver] 无携带碎片，无法合成");
                return;
            }

            List<SynthesisRecipe> recipes = synth.GetAvailableRecipes(bestType);
            if (recipes.Count == 0)
            {
                Debug.Log($"[GameplayDriver] {bestType} 无可用配方（当前环境）");
                return;
            }

            SynthesisRecipe? started = synth.TryStartSynthesis(playerId, bestType, recipes[0].OutputType);
            if (started.HasValue)
                Debug.Log($"[GameplayDriver] 玩家{playerId} 开始合成 {started.Value.OutputType}");
        }

        /// <summary>建造入口：本地角色位置最近且未完成的蓝图格放置建筑</summary>
        private void TryBuildAtNearest(byte playerId, CharacterController character)
        {
            BuildingSystem build = BuildingSystem.Instance;
            if (build == null || build.CurrentBlueprint.Count == 0) return;

            Vector2 worldPos = character.transform.position;

            BlueprintBlock? best = null;
            float bestDist = float.MaxValue;
            foreach (var block in build.CurrentBlueprint)
            {
                if (block.IsCompleted) continue;
                float dist = Vector2.Distance(worldPos, GridCoord.WorldFromGrid(block.GridPosition));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = block;
                }
            }

            if (!best.HasValue)
            {
                Debug.Log("[GameplayDriver] 蓝图已全部完成");
                return;
            }

            // 距离限制：必须在蓝图格 1.5 格内（防止隔空建造）
            if (bestDist > 1.5f)
            {
                Debug.Log($"[GameplayDriver] 距离最近蓝图格 {bestDist:F1} 格，走近后再按 E 建造");
                return;
            }

            build.PlaceBuilding(playerId, best.Value.BuildingType, best.Value.RequiredMaterial,
                best.Value.GridPosition, best.Value.Facing);
        }
    }
}
