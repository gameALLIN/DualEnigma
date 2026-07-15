/// ============================================================
/// 文件名: ShelterEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Shelter
{
    /// <summary>
    /// 5种庇护环境。
    /// 引用：双生庇护系统设计.md §三 五种庇护机制
    /// </summary>
    public enum ShelterEnvironment
    {
        /// <summary>火山 — 水人受影响(-3HP/s)，火人安全</summary>
        Volcano,
        /// <summary>洪水 — 火人受影响(-3HP/s)，水人安全</summary>
        Flood,
        /// <summary>暴风雪 — 水人受影响(-2HP/s, 移速-50%)，火人完全免疫</summary>
        Blizzard,
        /// <summary>地震 — 双方受影响(-3HP/次冲击波)</summary>
        Earthquake,
        /// <summary>陨石 — 双方受影响(被砸概率50%)</summary>
        Meteorite,
    }
}
