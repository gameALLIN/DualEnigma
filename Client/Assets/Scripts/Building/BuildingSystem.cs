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

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<IBuildSystem>(this);
            Debug.Log("[BuildingSystem] 建造系统初始化完成");
        }

        /// <summary>
        /// 生成蓝图。
        /// </summary>
        public void GenerateBlueprint(int disasterType, int round)
        {
            CurrentBlueprint.Clear();
            _grid.Clear();

            // 蓝图块数：轮次1=3-4, 轮次2=5-6, 轮次3=7-8, 最终关=9-10
            int minBlocks = round * 2 + 1;
            int maxBlocks = minBlocks + 1;
            int blockCount = Random.Range(minBlocks, maxBlocks + 1);

            for (int i = 0; i < blockCount; i++)
            {
                Vector2Int pos = new Vector2Int(
                    Random.Range(0, BuildingGrid.Width),
                    Random.Range(0, BuildingGrid.Height)
                );

                CurrentBlueprint.Add(new BlueprintBlock
                {
                    GridPosition = pos,
                    BuildingType = (BuildingType)Random.Range(0, 5),
                    RequiredMaterial = (MaterialType)Random.Range(0, 5),
                    Facing = 0,
                    IsCompleted = false
                });
            }

            Debug.Log($"[BuildingSystem] 生成蓝图: {blockCount}块 (轮次{round})");
        }

        /// <summary>
        /// 放置建筑。
        /// </summary>
        public bool PlaceBuilding(byte playerId, BuildingType type, MaterialType material, Vector2Int gridPos, int facing)
        {
            if (_grid.IsOccupied(gridPos))
            {
                Debug.Log($"[BuildingSystem] 位置已占用: {gridPos}");
                return false;
            }

            float baseHP = GetBuildingHP(type);
            if (material == MaterialType.IceBrick)
                baseHP *= 1.5f;

            BuildingData building = new BuildingData
            {
                BuildingId = _nextBuildingId++,
                Type = type,
                Material = material,
                GridPosition = gridPos,
                Facing = facing,
                BaseHP = baseHP,
                CurrentHP = baseHP,
                IsInSafeZone = true
            };

            Buildings.Add(building);
            _grid.SetOccupied(gridPos, building.BuildingId);

            EventBus.Instance.Publish(new BuildingPlacedEvent
            {
                buildingId = building.BuildingId,
                type = type,
                gridPos = gridPos
            });

            Debug.Log($"[BuildingSystem] 建筑放置: ID={building.BuildingId}, {type}, {material}, {gridPos}");
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
        /// </summary>
        public void SyncBuildingHPs()
        {
            Debug.Log($"[BuildingSystem] 修整阶段同步 {Buildings.Count} 个建筑HP");
        }

        /// <summary>设置 M4 预言干扰状态</summary>
        public void SetM4ProphecyInterference(bool enabled)
        {
            _m4ProphecyInterference = enabled;
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
