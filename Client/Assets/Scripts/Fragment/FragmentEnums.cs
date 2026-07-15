/// ============================================================
/// 文件名: FragmentEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 碎片类型。只有3种基础碎片。
    /// 引用：GDD v6.1 §4.1
    /// </summary>
    public enum FragmentType
    {
        /// <summary>冰晶碎片（★ 数量最多，蓝白棱形）</summary>
        IceCrystal,
        /// <summary>熔岩碎片（★★ 橙红不规则形）</summary>
        Lava,
        /// <summary>岩石碎片（★★★ 数量最少，灰色方形）</summary>
        Rock,
    }

    /// <summary>
    /// 碎片状态枚举
    /// </summary>
    public enum FragmentState
    {
        /// <summary>掉落中（空中或刚落地，可被接住）</summary>
        Falling,
        /// <summary>已被接住</summary>
        Collected,
        /// <summary>自然消失</summary>
        Despawned,
        /// <summary>被点燃（火人被动触发）</summary>
        Ignited,
        /// <summary>被冻结（水人被动触发）</summary>
        Frozen,
        /// <summary>转化为温砖</summary>
        ConvertedToWarmBrick,
    }
}
