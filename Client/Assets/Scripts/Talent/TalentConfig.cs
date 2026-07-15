/// ============================================================
/// 文件名: TalentConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 天赋系统配置数据（ScriptableObject）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Talent
{
    /// <summary>
    /// 天赋系统配置。
    /// 引用：天赋系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "TalentConfig", menuName = "DualEnigma/TalentConfig")]
    public class TalentConfig : ScriptableObject
    {
        [Header("水人天赋池")]
        [SerializeField] private List<TalentData> _aquaPool = new List<TalentData>();
        [Header("火人天赋池")]
        [SerializeField] private List<TalentData> _ignisPool = new List<TalentData>();
        [Header("共享天赋池")]
        [SerializeField] private List<TalentData> _sharedPool = new List<TalentData>();

        [Header("稀有度概率（按章节）")]
        [SerializeField] private float[] _chapter1Rates = { 0.75f, 0.20f, 0.05f };
        [SerializeField] private float[] _chapter2Rates = { 0.55f, 0.35f, 0.10f };
        [SerializeField] private float[] _chapter3Rates = { 0.40f, 0.40f, 0.20f };

        [Header("保底设置")]
        [SerializeField] private int _rarityBoostThreshold = 3;
        [SerializeField] private int _minFirstAidAppearances = 2;

        public List<TalentData> AquaPool => _aquaPool;
        public List<TalentData> IgnisPool => _ignisPool;
        public List<TalentData> SharedPool => _sharedPool;
        public int RarityBoostThreshold => _rarityBoostThreshold;
        public int MinFirstAidAppearances => _minFirstAidAppearances;

        /// <summary>获取指定章节的稀有度概率</summary>
        public float[] GetRarityRates(int chapter)
        {
            switch (chapter)
            {
                case 1: return _chapter1Rates;
                case 2: return _chapter2Rates;
                case 3: return _chapter3Rates;
                default: return _chapter1Rates;
            }
        }
    }
}
