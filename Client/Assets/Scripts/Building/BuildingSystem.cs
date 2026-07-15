/// ============================================================
/// 文件名: BuildingSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建造系统管理器，管理蓝图、网格、建筑放置和抗性。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Synthesis;
using DualEnigma.Character;
using DualEnigma.Disaster;
using DualEnigma.Shelter;

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
        }

        /// <summary>
        /// 放置建筑。检查材料消耗和安全区归属。
        /// </summary>
        public bool PlaceBuilding(byte playerId, BuildingType type, MaterialType material, Vector2Int gridPos, int facing)
        {
            if (_grid.IsOccupied(gridPos))
            {
                Debug.Log($"[BuildingSystem] 位置已占用: {gridPos}");
                return false;
            }

            // 查找该位置的蓝图块，获取所需材料
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

            // 通过 ICharacterSystem 获取放置者 CharacterController，检查并消耗材料
            ICharacterSystem charSystem = ServiceLocator.Get<ICharacterSystem>();
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

            float baseHP = GetBuildingHP(type);
            if (material == MaterialType.IceBrick)
                baseHP *= 1.5f;

            // 计算建筑位置到安全区中心的距离
            Vector2Int diff = gridPos - _safeZoneCenter;
            float distance = Mathf.Sqrt(diff.x * diff.x + diff.y * diff.y);
            bool isInSafeZone = distance <= _safeZoneRadius;

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
                }
            }

            EventBus.Instance.Publish(new BuildingPlacedEvent
            {
                buildingId = building.BuildingId,
                type = type,
                gridPos = gridPos
            });

            Debug.Log($"[BuildingSystem] 建筑放置: ID={building.BuildingId}, {type}, {material}, {gridPos}, 安全区={isInSafeZone}");
            return true;
        }

        /// <summary>
        /// 修补建筑。
        /// </summary>
        public bool RepairBuilding(byte playerId, int buildingId)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return false;

            building.CurrentHP = Mathf.Min(building.CurrentHP + building.BaseHP * 0.5f, building.BaseHP);
            Debug.Log($"[BuildingSystem] 建筑修补: ID={buildingId}, HP={building.CurrentHP}/{building.BaseHP}");
            return true;
        }

        /// <summary>
        /// 拆除建筑。
        /// </summary>
        public bool DemolishBuilding(byte playerId, int buildingId)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return false;

            _grid.ClearOccupied(building.GridPosition);
            Buildings.Remove(building);
            Debug.Log($"[BuildingSystem] 建筑拆除: ID={buildingId}");
            return true;
        }

        /// <summary>
        /// 建筑受伤害。
        /// </summary>
        public void DamageBuilding(int buildingId, float damage)
        {
            BuildingData building = Buildings.Find(b => b.BuildingId == buildingId);
            if (building == null) return;

            building.CurrentHP -= damage;

            if (building.CurrentHP <= 0f)
            {
                building.CurrentHP = 0f;
                _grid.ClearOccupied(building.GridPosition);
                Buildings.Remove(building);

                EventBus.Instance.Publish(new BuildingDestroyedEvent
                {
                    buildingId = buildingId
                });

                Debug.Log($"[BuildingSystem] 建筑被摧毁: ID={buildingId}");
            }
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
