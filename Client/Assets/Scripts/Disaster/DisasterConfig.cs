/// ============================================================
/// 文件名: DisasterConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 灾难系统配置数据（ScriptableObject）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Disaster
{
    /// <summary>
    /// 灾难系统配置。
    /// 引用：灾难系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "DisasterConfig", menuName = "DualEnigma/DisasterConfig")]
    public class DisasterConfig : ScriptableObject
    {
        [Header("35种灾难参数列表")]
        [SerializeField] private List<DisasterParams> _disasters = new List<DisasterParams>();

        [Header("E3强化版参数")]
        [SerializeField] private DisasterParams _e3Enhanced;

        [Header("渐进强度时间轴")]
        [SerializeField] private float[] _intensityCurve = { 0.3f, 0.6f, 1.0f, 0.8f };

        public List<DisasterParams> Disasters => _disasters;
        public DisasterParams E3Enhanced => _e3Enhanced;
        public float[] IntensityCurve => _intensityCurve;

        /// <summary>获取指定灾难的参数</summary>
        public DisasterParams GetDisaster(DisasterId id)
        {
            return _disasters.Find(d => d.Id == id);
        }
    }
}
