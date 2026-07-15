/// ============================================================
/// 文件名: DisasterParams.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难基础参数数据结构。
/// ============================================================

using UnityEngine;
using DualEnigma.Shelter;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难基础参数。
    /// 引用：灾难系统.md §2.2
    /// </summary>
    [System.Serializable]
    public class DisasterParams
    {
        /// <summary>灾难ID</summary>
        public DisasterId Id;
        /// <summary>灾难名称</summary>
        public string Name;
        /// <summary>灾难类别</summary>
        public DisasterCategory Category;
        /// <summary>庇护环境</summary>
        public ShelterEnvironment Environment;
        /// <summary>基础DPS</summary>
        public float BaseDPS;
        /// <summary>影响范围</summary>
        public float Range;
        /// <summary>持续时间（秒）</summary>
        public float Duration = 20f;
        /// <summary>随机种子</summary>
        public uint RandomSeed;
        /// <summary>难度倍率</summary>
        public float DifficultyMultiplier;
        /// <summary>灾难实际位置（世界坐标）</summary>
        public Vector2 Position = Vector2.zero;
    }
}
