/// ============================================================
/// 文件名: BuildingSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建造系统管理器，管理蓝图、网格、建筑放置和抗性。
/// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Synthesis;
using DualEnigma.Character;
using DualEnigma.Disaster;
using DualEnigma.Shelter;
using CharacterController = DualEnigma.Character.CharacterController;

namespace DualEnigma.Building
{
    /// <summary>
    /// 建造系统管理器。继承 Singleton<T>，注册 IBuildSystem 到 ServiceLocator。
    /// 引用：建造系统.md §3.1
    /// </summary>
    public class BuildingSystem : Singleton<BuildingSystem>, IBuildSystem
    {
        /// <summary>当前蓝图块列表</summary>
        public List<BlueprintBlock> CurrentBlueprint { get; } = new List<BlueprintBlock>();

        /// <summary>所有已放置建筑</summary>
        public List<BuildingData> Buildings { get; } = new List<BuildingData>();

        [SerializeField] private BuildingConfig _buildingConfig;

        /// <summary>网格管理</summary>
        private readonly BuildingGrid _grid = new BuildingGrid();

        /// <summary>建筑ID自增计数器</summary>
        private int _nextBuildingId;

        /// <summary>M4预言干扰：蓝图材料变化</summary>
        private bool _m4ProphecyInterference;

        /// <summary>安全区中心坐标（网格坐标）</summary>
        private Vector2Int _safeZoneCenter = new Vector2Int(7, 4);

        /// <summary>安全区半径（格数）</summary>
        private float _safeZoneRadius = 5f;

        /// <summary>当前庇护环境</summary>
        private ShelterEnvironment _currentEnvironment;

        /// <summary>灾难来源方向（网格方向向量），用于朝向抗性判断</summary>
        private Vector2Int _disasterDirection = Vector2Int.up;

        /// <summary>建造视觉层（蓝图/建筑 SpriteRenderer）</summary>
        private readonly BuildingVisualizer _visualizer = new BuildingVisualizer();

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IBuildSystem>(this);
            Debug.Log("[BuildingSystem] 建造系统初始化完成");
        }

        /// <summary>
        /// 生成蓝图。根据灾难类型选择建筑形状、类型和材料。
        /// 引用：建造系统.md §3.3 蓝图生成
        /// </summary>
        /// <param name="disasterType">灾难类别</param>
        /// <param name="round">当前轮次（1-3，最终关=4）</param>
        public void GenerateBlueprint(DisasterCategory disasterType, int round)
        {
            CurrentBlueprint.Clear();
            _grid.Clear();

            // 蓝图块数：轮次1=3-4, 轮次2=5-6, 轮次3=7-8, 最终关=9-10
            int minBlocks = round * 2 + 1;
            int maxBlocks = minBlocks + 1;
            int blockCount = Random.Range(minBlocks, maxBlocks + 1);

            // 将灾难类别映射到庇护环境，用于确定建筑形状
            ShelterEnvironment env = MapCategoryToEnvironment(disasterType);
            _currentEnvironment = env;

            // 根据灾难类型推荐建筑类型和材料
            BuildingType recommendedType = GetRecommendedBuildingType(env);
            MaterialType recommendedMaterial = GetRecommendedMaterial(env);

            // 根据灾难类型生成建筑位置形状
            List<Vector2Int> positions = GenerateShapePositions(env, blockCount);

            for (int i = 0; i < positions.Count; i++)
            {
                MaterialType material = recommendedMaterial;

                // M4预言干扰：材料随机替换
                if (_m4ProphecyInterference)
                {
                    material = (MaterialType)Random.Range(0, 6);
                }

                CurrentBlueprint.Add(new BlueprintBlock
                {
                    GridPosition = positions[i],
                    BuildingType = recommendedType,
                    RequiredMaterial = material,
                    Facing = 0,
                    IsCompleted = false
                });
            }

            Debug.Log($"[BuildingSystem] 生成蓝图: {CurrentBlueprint.Count}块 (灾难类别={disasterType}, 环境={env}, 轮次={round})");

            // 蓝图视觉（半透明占位块）
            _visualizer.RenderBlueprint(CurrentBlueprint);
        }

        /// <summary>
        /// 清空蓝图与全部建筑（新局开始时由流程驱动调用）。
        /// </summary>
        public void ClearAll()
        {
            CurrentBlueprint.Clear();
            _grid.Clear();
            Buildings.Clear();
            _nextBuildingId = 0;
            _visualizer.ClearAll();
            Debug.Log("[BuildingSystem] 蓝图与建筑已清空");
        }

        /// <summary>
        /// 放置建筑。检查两人同时在安全区、材料消耗和安全区归属。
        /// 启动协程，等待0.5秒后实际创建建筑。
        /// </summary>
        public bool PlaceBuilding(byte playerId, BuildingType type, MaterialType material, Vector2Int gridPos, int facing)
        {
            if (_grid.IsOccupied(gridPos))
            {
                Debug.Log($"[BuildingSystem] 位置已占用: {gridPos}");
                return false;
            }

            // 通过 ICharacterSystem 获取角色系统
            ICharacterSystem charSystem = ServiceLocator.Get<ICharacterSystem>();

            // 1. 检查两个角色是否都在安全区范围内
            if (charSystem != null)
            {
                CharacterController aqua = charSystem.GetCharacter(CharacterType.Aqua);
                CharacterController ignis = charSystem.GetCharacter(CharacterType.Ignis);
                if (!IsCharacterInSafeZone(aqua) || !IsCharacterInSafeZone(ignis))
                {
                    Debug.Log("[BuildingSystem] 两个角色未同时处于安全区内，无法放置建筑");
                    return false;
                }
            }

            // 2. 查找该位置的蓝图块，获取所需材料
            MaterialType requiredMaterial = material;
            BlueprintBlock? matchingBlock = null;
            foreach (var block in CurrentBlueprint)
            {
                if (block.GridPosition == gridPos)
                {
                    matchingBlock = block;
                    requiredMaterial = block.RequiredMaterial;
                    break;
                }
            }

            // 3. 获取放置者 CharacterController，检查并消耗材料
            if (charSystem != null)
            {
                CharacterController character = charSystem.GetCharacter((CharacterType)playerId);
                if (character != null)
                {
                    if (!character.TryConsumeMaterial(requiredMaterial, 1))
                    {
                        Debug.Log($"[BuildingSystem] 材料不足: 需要 {requiredMaterial} x1");
                        return false;
                    }
                }
            }

            // 4. 计算建筑HP（冰砖仅在暴风雪环境下+50%）
            float baseHP = GetBuildingHP(type);
            if (material == MaterialType.IceBrick && _currentEnvironment == ShelterEnvironment.Blizzard)
                baseHP *= 1.5f;

            // 5. 计算建筑位置到安全区中心的距离
            Vector2Int diff = gridPos - _safeZoneCenter;
            float distance = Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);
            bool isInSafeZone = distance <= _safeZoneRadius;

            // 6. 启动放置协程，等待0.5秒后实际创建建筑
            StartCoroutine(PlaceBuildingCoroutine(type, material, gridPos, facing, baseHP, isInSafeZone, matchingBlock));

            Debug.Log($"[BuildingSystem] 建筑放置中: {type}, {material}, {gridPos}, 预计0.5秒后完成");
            return true;
        }

        /// <summary>
        /// 放置建筑协程。等待0.5秒后实际创建建筑实例。
        /// </summary>
        private IEnumerator PlaceBuildingCoroutine(BuildingType type, MaterialType material, Vector2Int gridPos, int facing, float baseHP, bool isInSafeZone, BlueprintBlock? matchingBlock)
        {
            yield return new WaitForSeconds(0.5f);

            // 对局已结束（退出/结算）则放弃放置
            if (GameManager.HasInstance && GameManager.Instance.State.IsGameOver)
                yield break;

            // 再次检查位置是否被占用（等待期间可能被其他操作占用）
            if (_grid.IsOccupied(gridPos))
            {
                Debug.Log($"[BuildingSystem] 放置取消：位置已被占用 {gridPos}");
                yield break;
            }

            BuildingData building = new BuildingData
            {
                BuildingId = _nextBuildingId++,
                Type = type,
                Material = material,
                GridPosition = gridPos,
                Facing = facing,
                BaseHP = baseHP,
                CurrentHP = baseHP,
                IsInSafeZone = isInSafeZone
            };

            Buildings.Add(building);
            _grid.SetOccupied(gridPos, building.BuildingId);

            // 标记蓝图块完成
            if (matchingBlock.HasValue)
            {
                int idx = CurrentBlueprint.IndexOf(matchingBlock.Value);
                if (idx >= 0)
                {
                    var b = CurrentBlueprint[idx];
                    b.IsCompleted = true;
                    CurrentBlueprint[idx] = b;
                    _visualizer.MarkBlueprintCompleted(b.GridPosition, b.RequiredMaterial);
                }
            }

            // 建筑视觉（实心材料色块）
            _visualizer.ShowBuilding(building.BuildingId, gridPos, material);

            EventBus.Instance.Publish(new BuildingPlacedEvent
            {
                buildingId = building.BuildingId,
                type = type,
                gridPos = gridPos
            });

            Debug.Log($"[BuildingSystem] 建筑放置完成: ID={building.BuildingId}, {type}, {material}, {gridPos}, 安全区={isInSafeZone}");
        }

        /// <summary>
        /// 修补建筑。消耗1个对应材料，1秒后恢复至满血。
        /// </summary>
        public bool RepairBuilding(byte playerId, int buildingId)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return false;

            ICharacterSystem charSystem = ServiceLocator.Get<ICharacterSystem>();
            if (charSystem != null)
            {
                CharacterController character = charSystem.GetCharacter((CharacterType)playerId);
                if (character != null)
                {
                    if (!character.TryConsumeMaterial(building.Material, 1))
                    {
                        Debug.Log($"[BuildingSystem] 修补材料不足: 需要 {building.Material} x1");
                        return false;
                    }
                }
            }

            StartCoroutine(RepairBuildingCoroutine(buildingId));
            Debug.Log($"[BuildingSystem] 建筑修补中: ID={buildingId}, 预计1秒后完成");
            return true;
        }

        private IEnumerator RepairBuildingCoroutine(int buildingId)
        {
            yield return new WaitForSeconds(1f);

            if (GameManager.HasInstance && GameManager.Instance.State.IsGameOver)
                yield break;

            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) yield break;

            building.CurrentHP = building.BaseHP;
            _visualizer.UpdateBuildingVisual(buildingId, 1f);
            Debug.Log($"[BuildingSystem] 建筑修补完成: ID={buildingId}, HP={building.CurrentHP}/{building.BaseHP}");
        }

        /// <summary>
        /// 拆除建筑。仅修整阶段允许拆除，1秒后完成并返还50%材料。
        /// </summary>
        public bool DemolishBuilding(byte playerId, int buildingId)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return false;

            GamePhase currentPhase = GameStateMachine.Instance.CurrentPhase;
            if (currentPhase != GamePhase.Rest)
            {
                Debug.Log($"[BuildingSystem] 仅修整阶段可拆除建筑，当前阶段: {currentPhase}");
                return false;
            }

            MaterialType returnMaterial = building.Material;
            StartCoroutine(DemolishBuildingCoroutine(buildingId, returnMaterial, playerId));
            Debug.Log($"[BuildingSystem] 建筑拆除中: ID={buildingId}, 预计1秒后完成");
            return true;
        }

        private IEnumerator DemolishBuildingCoroutine(int buildingId, MaterialType material, byte playerId)
        {
            yield return new WaitForSeconds(1f);

            if (GameManager.HasInstance && GameManager.Instance.State.IsGameOver)
                yield break;

            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) yield break;

            _grid.ClearOccupied(building.GridPosition);
            Buildings.Remove(building);
            _visualizer.RemoveBuilding(buildingId);

            ICharacterSystem charSystem = ServiceLocator.Get<ICharacterSystem>();
            if (charSystem != null)
            {
                CharacterController character = charSystem.GetCharacter((CharacterType)playerId);
                if (character != null)
                {
                    character.AddMaterial(material, 1);
                }
            }

            Debug.Log($"[BuildingSystem] 建筑拆除完成: ID={buildingId}, 返还1个{material}");
        }

        /// <summary>
        /// 建筑受伤害。考虑抗性矩阵、朝向抗性和安全区减免。
        /// </summary>
        public void DamageBuilding(int buildingId, float damage)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return;

            float resistanceCoeff = GetResistanceCoefficient(building.Material, _currentEnvironment);
            float facingMultiplier = IsFacingCorrect(building) ? 1f : 1.5f;
            float zoneMultiplier = building.IsInSafeZone ? 0.5f : 1f;

            float finalDamage = damage * resistanceCoeff * facingMultiplier * zoneMultiplier;

            building.CurrentHP -= finalDamage;

            if (building.CurrentHP <= 0f)
            {
                building.CurrentHP = 0f;
                _grid.ClearOccupied(building.GridPosition);
                Buildings.Remove(building);
                _visualizer.RemoveBuilding(buildingId);

                EventBus.Instance.Publish(new BuildingDestroyedEvent
                {
                    buildingId = buildingId
                });

                Debug.Log($"[BuildingSystem] 建筑被摧毁: ID={buildingId}");
            }
            else
            {
                _visualizer.UpdateBuildingVisual(buildingId, building.CurrentHP / building.BaseHP);
                Debug.Log($"[BuildingSystem] 建筑受损: ID={buildingId}, 伤害={finalDamage}(抗性={resistanceCoeff}, 朝向={facingMultiplier}, 安全区={zoneMultiplier}), HP={building.CurrentHP}/{building.BaseHP}");
            }
        }

        /// <summary>
        /// 获取抗性系数。5种环境×6种材料的完整矩阵。
        /// ★★★ 免疫(0×) | ★★ 强抗性(0.3×) | ★ 抗性(0.6×) | — 正常(1.0×) | ✗ 弱点(1.5×)
        /// 引用：灾难系统设计.md §4.3 建筑×材料×环境 抗性矩阵
        /// </summary>
        private float GetResistanceCoefficient(MaterialType material, ShelterEnvironment environment)
        {
            float[,] matrix = new float[,]
            {
                { 0.3f, 0f,   1.5f, 1.5f, 0.6f, 0.6f },
                { 1.5f, 1.5f, 0.3f, 0f,   0.6f, 0.6f },
                { 0.3f, 0f,   0.3f, 0.3f, 0.3f, 0.6f },
                { 0.6f, 0.6f, 0.6f, 0.6f, 0f,   0.6f },
                { 0.6f, 0.6f, 0.6f, 0.6f, 0f,   0.6f },
            };

            int envIdx = (int)environment;
            int matIdx = (int)material;

            if (envIdx < 0 || envIdx >= 5 || matIdx < 0 || matIdx >= 6)
                return 1.0f;

            return matrix[envIdx, matIdx];
        }

        /// <summary>
        /// 修整阶段校正所有建筑HP（Host调用）。
        /// 遍历所有建筑，将 CurrentHP 校正到 [0, MaxHP] 范围。
        /// </summary>
        public void SyncBuildingHPs()
        {
            int syncedCount = 0;
            foreach (var building in Buildings)
            {
                float clampedHP = Mathf.Clamp(building.CurrentHP, 0f, building.MaxHP);
                if (!Mathf.Approximately(clampedHP, building.CurrentHP))
                {
                    building.CurrentHP = clampedHP;
                    syncedCount++;
                }
            }

            Debug.Log($"[BuildingSystem] 修整阶段已同步 {syncedCount}/{Buildings.Count} 个建筑HP");
        }

        /// <summary>设置 M4 预言干扰状态</summary>
        public void SetM4ProphecyInterference(bool enabled)
        {
            _m4ProphecyInterference = enabled;
        }

        /// <summary>
        /// 设置安全区中心和半径（供外部系统调用）。
        /// </summary>
        /// <param name="center">安全区中心网格坐标</param>
        /// <param name="radius">安全区半径（格数）</param>
        public void SetSafeZone(Vector2Int center, float radius)
        {
            _safeZoneCenter = center;
            _safeZoneRadius = radius;
            Debug.Log($"[BuildingSystem] 安全区设置: 中心={center}, 半径={radius}");
        }

        /// <summary>
        /// 设置当前庇护环境。
        /// </summary>
        /// <param name="env">庇护环境类型</param>
        public void SetEnvironment(ShelterEnvironment env)
        {
            _currentEnvironment = env;
            Debug.Log($"[BuildingSystem] 庇护环境设置: {env}");
        }

        /// <summary>
        /// 设置灾难来源方向（建筑应面向此方向以获得正常抗性）。
        /// </summary>
        /// <param name="direction">灾难来源的网格方向向量</param>
        public void SetDisasterDirection(Vector2Int direction)
        {
            _disasterDirection = direction;
            Debug.Log($"[BuildingSystem] 灾难来源方向设置: {direction}");
        }

        /// <summary>
        /// 判断角色是否在安全区范围内。
        /// 角色为世界坐标，安全区中心为网格坐标——统一经 GridCoord 换算到世界系再比较。
        /// </summary>
        private bool IsCharacterInSafeZone(CharacterController character)
        {
            if (character == null) return false;
            Vector2 charPos = character.transform.position;
            Vector2 center = GridCoord.WorldFromGrid(_safeZoneCenter);
            float distance = Vector2.Distance(charPos, center);
            return distance <= _safeZoneRadius;
        }

        /// <summary>
        /// 判断建筑朝向是否正确（面对灾难来源方向）。
        /// Facing: 0=上, 1=右, 2=下, 3=左。
        /// 无朝向要求的建筑类型（ReinforcedTower, Shelter）始终返回 true。
        /// </summary>
        private bool IsFacingCorrect(BuildingData building)
        {
            // 无朝向要求的建筑类型：ReinforcedTower(2), Shelter(3)
            if (_buildingConfig != null && !_buildingConfig.HasFacing(building.Type))
                return true;

            // 将 Facing 归一化到 [0,3]
            int facingIdx = ((building.Facing % 4) + 4) % 4;
            Vector2Int[] facingDirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            Vector2Int buildingFacing = facingDirs[facingIdx];

            // 建筑应面向灾难来源方向
            return buildingFacing == _disasterDirection;
        }

        /// <summary>
        /// 将灾难类别映射到庇护环境，用于确定建筑形状。
        /// Element→火山, Environment→洪水, TimeSpace→暴风雪,
        /// Perception→地震, Physics→陨石, Mechanism→随机
        /// </summary>
        private ShelterEnvironment MapCategoryToEnvironment(DisasterCategory category)
        {
            switch (category)
            {
                case DisasterCategory.Element:
                    return ShelterEnvironment.Volcano;
                case DisasterCategory.Environment:
                    return ShelterEnvironment.Flood;
                case DisasterCategory.TimeSpace:
                    return ShelterEnvironment.Blizzard;
                case DisasterCategory.Perception:
                    return ShelterEnvironment.Earthquake;
                case DisasterCategory.Physics:
                    return ShelterEnvironment.Meteorite;
                case DisasterCategory.Mechanism:
                    return (ShelterEnvironment)Random.Range(0, 5);
                default:
                    return ShelterEnvironment.Volcano;
            }
        }

        /// <summary>
        /// 根据庇护环境推荐建筑类型。
        /// </summary>
        private BuildingType GetRecommendedBuildingType(ShelterEnvironment env)
        {
            switch (env)
            {
                case ShelterEnvironment.Flood:
                    return BuildingType.FloodBarrier;
                case ShelterEnvironment.Volcano:
                    return BuildingType.FireWall;
                case ShelterEnvironment.Earthquake:
                    return BuildingType.ReinforcedTower;
                case ShelterEnvironment.Meteorite:
                    return BuildingType.Deflector;
                case ShelterEnvironment.Blizzard:
                    return BuildingType.Shelter;
                default:
                    return BuildingType.FloodBarrier;
            }
        }

        /// <summary>
        /// 根据庇护环境推荐材料类型。
        /// </summary>
        private MaterialType GetRecommendedMaterial(ShelterEnvironment env)
        {
            switch (env)
            {
                case ShelterEnvironment.Flood:
                    return MaterialType.WaterBrick;
                case ShelterEnvironment.Volcano:
                    return MaterialType.FireBrick;
                case ShelterEnvironment.Earthquake:
                    return MaterialType.StoneBrick;
                case ShelterEnvironment.Meteorite:
                    return MaterialType.StoneBrick;
                case ShelterEnvironment.Blizzard:
                    return MaterialType.IceBrick;
                default:
                    return MaterialType.WaterBrick;
            }
        }

        /// <summary>
        /// 根据庇护环境生成建筑位置形状。
        /// Flood→横线, Volcano→竖线, Earthquake→金字塔,
        /// Meteorite→分散, Blizzard→拱形
        /// </summary>
        private List<Vector2Int> GenerateShapePositions(ShelterEnvironment env, int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            switch (env)
            {
                case ShelterEnvironment.Flood:
                {
                    // 横线形状（水平连续格子，防洪堤）
                    int y = BuildingGrid.Height / 2;
                    int startX = Mathf.Clamp((BuildingGrid.Width - count) / 2, 0, BuildingGrid.Width - 1);
                    for (int i = 0; i < count; i++)
                    {
                        int x = Mathf.Clamp(startX + i, 0, BuildingGrid.Width - 1);
                        var pos = new Vector2Int(x, y);
                        if (used.Add(pos))
                            positions.Add(pos);
                    }
                    break;
                }

                case ShelterEnvironment.Volcano:
                {
                    // 竖线形状（垂直连续格子，防火墙）
                    int x = BuildingGrid.Width / 2;
                    int startY = Mathf.Clamp((BuildingGrid.Height - count) / 2, 0, BuildingGrid.Height - 1);
                    for (int i = 0; i < count; i++)
                    {
                        int y = Mathf.Clamp(startY + i, 0, BuildingGrid.Height - 1);
                        var pos = new Vector2Int(x, y);
                        if (used.Add(pos))
                            positions.Add(pos);
                    }
                    break;
                }

                case ShelterEnvironment.Earthquake:
                {
                    // 金字塔形状（底层宽向上收窄，加固塔）
                    int centerX = BuildingGrid.Width / 2;
                    int baseY = 0;
                    int remaining = count;
                    int layer = 0;
                    while (remaining > 0 && baseY + layer < BuildingGrid.Height)
                    {
                        // 当前层宽度：随层数递减，保证至少1
                        int layerWidth = Mathf.Max(1, count - layer);
                        layerWidth = Mathf.Min(layerWidth, remaining);
                        int halfWidth = (layerWidth - 1) / 2;
                        for (int i = -halfWidth; i <= halfWidth && remaining > 0; i++)
                        {
                            int x = Mathf.Clamp(centerX + i, 0, BuildingGrid.Width - 1);
                            int y = Mathf.Clamp(baseY + layer, 0, BuildingGrid.Height - 1);
                            var pos = new Vector2Int(x, y);
                            if (used.Add(pos))
                            {
                                positions.Add(pos);
                                remaining--;
                            }
                            else
                            {
                                remaining--;
                            }
                        }
                        layer++;
                    }
                    break;
                }

                case ShelterEnvironment.Meteorite:
                {
                    // 分散布局（间隔放置，偏转器）
                    int maxAttempts = count * 10;
                    int attempts = 0;
                    while (positions.Count < count && attempts < maxAttempts)
                    {
                        int x = Random.Range(0, BuildingGrid.Width);
                        int y = Random.Range(0, BuildingGrid.Height);
                        var pos = new Vector2Int(x, y);
                        if (used.Add(pos))
                            positions.Add(pos);
                        attempts++;
                    }
                    break;
                }

                case ShelterEnvironment.Blizzard:
                {
                    // 拱形（半圆形排列，庇护所）
                    float radius = Mathf.Max(1f, count / Mathf.PI);
                    int cx = BuildingGrid.Width / 2;
                    int cy = 1;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = Mathf.PI * (i + 0.5f) / count;
                        int x = Mathf.Clamp(cx + Mathf.RoundToInt(Mathf.Cos(angle) * radius), 0, BuildingGrid.Width - 1);
                        int y = Mathf.Clamp(cy + Mathf.RoundToInt(Mathf.Sin(angle) * radius), 0, BuildingGrid.Height - 1);
                        var pos = new Vector2Int(x, y);
                        if (used.Add(pos))
                            positions.Add(pos);
                    }
                    break;
                }

                default:
                {
                    // 兜底：随机位置
                    int maxAttempts = count * 10;
                    int attempts = 0;
                    while (positions.Count < count && attempts < maxAttempts)
                    {
                        int x = Random.Range(0, BuildingGrid.Width);
                        int y = Random.Range(0, BuildingGrid.Height);
                        var pos = new Vector2Int(x, y);
                        if (used.Add(pos))
                            positions.Add(pos);
                        attempts++;
                    }
                    break;
                }
            }

            // 保险：如果形状生成不足，用随机位置补齐
            while (positions.Count < count)
            {
                int x = Random.Range(0, BuildingGrid.Width);
                int y = Random.Range(0, BuildingGrid.Height);
                var pos = new Vector2Int(x, y);
                if (used.Add(pos))
                    positions.Add(pos);
            }

            return positions;
        }

        private float GetBuildingHP(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.FireWall: return 50f;
                case BuildingType.FloodBarrier: return 40f;
                case BuildingType.ReinforcedTower: return 60f;
                case BuildingType.Shelter: return 40f;
                case BuildingType.Deflector: return 40f;
                default: return 40f;
            }
        }
    }
}
