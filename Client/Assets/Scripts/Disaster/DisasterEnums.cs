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
    /// 灾难ID枚举（35种 + E3强化版）。
    /// 百位数区分6大类：0xx元素、1xx环境、2xx时空、3xx感知、4xx物理、5xx机制。
    /// </summary>
    public enum DisasterId
    {
        // E系列 — 元素灾害 (0xx)
        E1 = 1, E2 = 2, E3 = 3, E4 = 4, E5 = 5, E6 = 6, E7 = 7, E8 = 8,
        // V系列 — 环境灾害 (1xx)
        V1 = 100, V2 = 101, V3 = 102, V4 = 103, V5 = 104, V6 = 105,
        // T系列 — 时空灾害 (2xx)
        T1 = 200, T2 = 201, T3 = 202, T4 = 203, T5 = 204,
        // S系列 — 感知灾害 (3xx)
        S1 = 300, S2 = 301, S3 = 302, S4 = 303, S5 = 304,
        // P系列 — 物理灾害 (4xx)
        P1 = 400, P2 = 401, P3 = 402, P4 = 403, P5 = 404,
        // M系列 — 机制灾害 (5xx)
        M1 = 500, M2 = 501, M3 = 502, M4 = 503, M5 = 504, M6 = 505,
        // E3强化版
        E3Enhanced = 9,
    }
}
