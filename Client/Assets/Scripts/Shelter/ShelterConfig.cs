/// ============================================================
/// 文件名: ShelterConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 庇护系统配置数据（ScriptableObject）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Shelter
{
    /// <summary>
    /// 庇护系统配置。
    /// 引用：庇护系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "ShelterConfig", menuName = "DualEnigma/ShelterConfig")]
    public class ShelterConfig : ScriptableObject
    {
        [Header("庇护能量参数")]
        [SerializeField] private ShelterParams _params = new ShelterParams();

        [Header("环境扣血速率（火山/洪水/暴风雪/地震/陨石）")]
        [SerializeField] private float[] _environmentDamageRates = { 3f, 3f, 2f, 3f, 0f };

        [Header("濒死保护")]
        [SerializeField] private float _dyingProtectThreshold = 30f;
        [SerializeField] private float _dyingProtectReduction = 0.3f;

        [Header("章节恢复")]
        [SerializeField] private int _chapterRestoreHP = 15;

        /// <summary>庇护参数</summary>
        public ShelterParams Params => _params;
        /// <summary>环境扣血速率</summary>
        public float[] EnvironmentDamageRates => _environmentDamageRates;
        /// <summary>濒死保护阈值（HP%）</summary>
        public float DyingProtectThreshold => _dyingProtectThreshold;
        /// <summary>濒死保护扣血降低比例</summary>
        public float DyingProtectReduction => _dyingProtectReduction;
        /// <summary>章节恢复HP</summary>
        public int ChapterRestoreHP => _chapterRestoreHP;

        /// <summary>获取指定环境的扣血速率</summary>
        public float GetDamageRate(ShelterEnvironment env)
        {
            return _environmentDamageRates[(int)env];
        }
    }
}
