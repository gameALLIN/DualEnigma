/// ============================================================
/// 文件名: BuildingType.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 建筑类型枚举（临时定义，待 Building 模块完善后迁移）。
///       引用：建造系统.md §2.1
/// ============================================================

namespace DualEnigma.Building
{
    /// <summary>
    /// 5种建筑类型。
    /// 引用：GDD v6.1 §6.3 建筑类型
    /// </summary>
    public enum BuildingType
    {
        /// <summary>防火墙 — 竖直墙体，阻挡火焰蔓延</summary>
        FireWall,
        /// <summary>防洪堤 — 水平屏障，阻挡水位上涨</summary>
        FloodBarrier,
        /// <summary>加固塔 — 金字塔结构，抗震稳定</summary>
        ReinforcedTower,
        /// <summary>避难所 — 封闭空间，防风雪/陨石</summary>
        Shelter,
        /// <summary>导流板 — 倾斜结构，偏转陨石轨迹</summary>
        Deflector,
    }
}
