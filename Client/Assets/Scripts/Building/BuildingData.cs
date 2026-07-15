/// ============================================================
/// 文件名: BuildingData.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建筑实例数据和蓝图数据结构。
/// ============================================================

using UnityEngine;
using DualEnigma.Synthesis;

namespace DualEnigma.Building
{
    /// <summary>
    /// 单个建筑实例的数据。
    /// 引用：建造系统.md §2.2
    /// </summary>
    [System.Serializable]
    public class BuildingData
    {
        /// <summary>建筑唯一ID</summary>
        public int BuildingId;
        /// <summary>建筑类型</summary>
        public BuildingType Type;
        /// <summary>使用的材料类型</summary>
        public MaterialType Material;
        /// <summary>网格坐标</summary>
        public Vector2Int GridPosition;
        /// <summary>朝向</summary>
        public int Facing;
        /// <summary>基础HP</summary>
        public float BaseHP;
        /// <summary>当前HP</summary>
        public float CurrentHP;
        /// <summary>是否在安全区内</summary>
        public bool IsInSafeZone;
    }

    /// <summary>
    /// 单个蓝图块的数据。
    /// </summary>
    [System.Serializable]
    public struct BlueprintBlock
    {
        /// <summary>网格坐标</summary>
        public Vector2Int GridPosition;
        /// <summary>要求的建筑类型</summary>
        public BuildingType BuildingType;
        /// <summary>要求的材料类型（M4时可能变化）</summary>
        public MaterialType RequiredMaterial;
        /// <summary>朝向</summary>
        public int Facing;
        /// <summary>是否已完成</summary>
        public bool IsCompleted;
    }

    /// <summary>
    /// 抗性等级
    /// 引用：灾难系统设计.md §4.4
    /// </summary>
    public enum ResistanceLevel
    {
        /// <summary>★★★ 免疫（0×伤害）</summary>
        Immune,
        /// <summary>★★ 强抗性（0.3×伤害）</summary>
        StrongResist,
        /// <summary>★ 抗性（0.6×伤害）</summary>
        Resist,
        /// <summary>— 无加成（1.0×伤害）</summary>
        Normal,
        /// <summary>✗ 弱点（1.5×伤害）</summary>
        Weakness,
    }
}
