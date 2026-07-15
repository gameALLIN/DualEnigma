/// ============================================================
/// 文件名: IBuildSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建造系统服务接口。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Synthesis;
using DualEnigma.Disaster;

namespace DualEnigma.Building
{
    /// <summary>
    /// 建造系统服务接口，注册到 ServiceLocator。
    /// 引用：建造系统.md §3.1
    /// </summary>
    public interface IBuildSystem
    {
        /// <summary>当前蓝图块列表</summary>
        List<BlueprintBlock> CurrentBlueprint { get; }

        /// <summary>所有已放置建筑</summary>
        List<BuildingData> Buildings { get; }

        /// <summary>生成蓝图</summary>
        void GenerateBlueprint(DisasterCategory disasterType, int round);

        /// <summary>放置建筑</summary>
        bool PlaceBuilding(byte playerId, BuildingType type, MaterialType material, Vector2Int gridPos, int facing);

        /// <summary>修补建筑</summary>
        bool RepairBuilding(byte playerId, int buildingId);

        /// <summary>拆除建筑</summary>
        bool DemolishBuilding(byte playerId, int buildingId);

        /// <summary>建筑受伤害</summary>
        void DamageBuilding(int buildingId, float damage);

        /// <summary>修整阶段校正所有建筑HP</summary>
        void SyncBuildingHPs();
    }
}
