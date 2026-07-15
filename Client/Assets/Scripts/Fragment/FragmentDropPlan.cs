/// ============================================================
/// 文件名: FragmentDropPlan.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片掉落计划数据结构。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 单个碎片的掉落计划项。由 Host 生成，同步给 Client。
    /// 引用：网络通信.md §4.1 碎片掉落计划同步
    /// </summary>
    [System.Serializable]
    public struct FragmentDropPlan
    {
        /// <summary>碎片唯一ID</summary>
        public int FragmentId;
        /// <summary>碎片类型</summary>
        public FragmentType Type;
        /// <summary>掉落位置（世界坐标）</summary>
        public Vector2 Position;
        /// <summary>掉落时间（相对阶段开始的秒数）</summary>
        public float DropTime;
        /// <summary>随机种子（用于物理模拟一致性）</summary>
        public uint Seed;
    }
}
