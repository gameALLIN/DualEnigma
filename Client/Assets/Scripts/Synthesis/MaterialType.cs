/// ============================================================
/// 文件名: MaterialType.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 材料类型枚举。
/// ============================================================

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 5种基础材料 + 1种特殊材料（温砖）。
    /// 引用：GDD v6.1 §5.1 合成规则
    /// </summary>
    public enum MaterialType
    {
        /// <summary>水砖（冰晶×2，1秒，基础防火）</summary>
        WaterBrick,
        /// <summary>冰砖（冰晶×3，1.5秒，免疫山火）</summary>
        IceBrick,
        /// <summary>火砖（熔岩×2，1秒，基础防水）</summary>
        FireBrick,
        /// <summary>岩浆砖（熔岩×3，1.5秒，免疫洪水）</summary>
        LavaBrick,
        /// <summary>石砖（冰晶+熔岩+岩石，2秒，免疫地震）</summary>
        StoneBrick,
        /// <summary>温砖（特殊材料，火人点燃+水人冻结触发）</summary>
        WarmBrick,
    }
}
