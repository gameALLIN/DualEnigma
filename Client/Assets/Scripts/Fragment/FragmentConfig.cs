/// ============================================================
/// 文件名: FragmentConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 碎片系统配置数据（ScriptableObject）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Fragment
{
    /// <summary>
    /// 碎片系统配置。
    /// 引用：碎片系统.md §2.3, §2.4
    /// </summary>
    [CreateAssetMenu(fileName = "FragmentConfig", menuName = "DualEnigma/FragmentConfig")]
    public class FragmentConfig : ScriptableObject
    {
        [Header("密度系数（按轮次1/2/3）")]
        [SerializeField] private float[] _densityFactors = { 1.0f, 0.85f, 0.7f };

        [Header("存续时间（按轮次1/2/3，秒）")]
        [SerializeField] private float[] _lifetimes = { 3.5f, 3.0f, 2.5f };

        [Header("掉落数量")]
        [SerializeField] private int _previewCount = 5;
        [SerializeField] private int _collectPhaseCount = 25;

        [Header("碎片掉落范围")]
        [SerializeField] private float _dropRangeMin = 8f;
        [SerializeField] private float _dropRangeMax = 18f;

        /// <summary>密度系数</summary>
        public float[] DensityFactors => _densityFactors;
        /// <summary>存续时间</summary>
        public float[] Lifetimes => _lifetimes;
        /// <summary>预告阶段碎片数</summary>
        public int PreviewCount => _previewCount;
        /// <summary>收集阶段碎片数</summary>
        public int CollectPhaseCount => _collectPhaseCount;
        /// <summary>掉落范围最小值</summary>
        public float DropRangeMin => _dropRangeMin;
        /// <summary>掉落范围最大值</summary>
        public float DropRangeMax => _dropRangeMax;

        /// <summary>获取指定轮次的密度系数</summary>
        public float GetDensityFactor(int round)
        {
            int index = Mathf.Clamp(round - 1, 0, _densityFactors.Length - 1);
            return _densityFactors[index];
        }

        /// <summary>获取指定轮次的存续时间</summary>
        public float GetLifetime(int round)
        {
            int index = Mathf.Clamp(round - 1, 0, _lifetimes.Length - 1);
            return _lifetimes[index];
        }
    }
}
