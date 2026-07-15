/// ============================================================
/// 文件名: SynthesisRecipe.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 合成配方数据结构。
/// ============================================================

using DualEnigma.Fragment;

namespace DualEnigma.Synthesis
{
    /// <summary>
    /// 单条合成配方。按庇护环境索引。
    /// 引用：灾难系统设计.md §5.1 合成表
    /// </summary>
    [System.Serializable]
    public struct SynthesisRecipe
    {
        /// <summary>输入碎片类型</summary>
        public FragmentType InputType;
        /// <summary>输出材料类型</summary>
        public MaterialType OutputType;
        /// <summary>所需碎片数量</summary>
        public int RequiredCount;
        /// <summary>合成时间（秒）</summary>
        public float SynthesisTime;
    }
}
