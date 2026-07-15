/// ============================================================
/// 文件名: DisasterEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难系统相关枚举定义。
/// ============================================================

using DualEnigma.Shelter;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难类别。6大类35种。
    /// 引用：灾难系统设计.md §二 灾难概念库
    /// </summary>
    public enum DisasterCategory
    {
        Element,
        Environment,
        TimeSpace,
        Perception,
        Physics,
        Mechanism,
    }

    /// <summary>
    /// 灾难ID枚举（35种 + E3强化版）
    /// </summary>
    public enum DisasterId
    {
        E1, E2, E3, E4, E5, E6, E7, E8,
        V1, V2, V3, V4, V5, V6,
        T1, T2, T3, T4, T5,
        S1, S2, S3, S4, S5,
        P1, P2, P3, P4, P5,
        M1, M2, M3, M4, M5, M6,
        E3Enhanced,
    }
}
