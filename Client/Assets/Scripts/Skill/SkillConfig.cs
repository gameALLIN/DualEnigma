/// ============================================================
/// 文件名: SkillConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统配置数据（ScriptableObject）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能系统配置。
    /// 引用：技能系统.md §6.1
    /// </summary>
    [CreateAssetMenu(fileName = "SkillConfig", menuName = "DualEnigma/SkillConfig")]
    public class SkillConfig : ScriptableObject
    {
        [Header("水人E技能卡池")]
        [SerializeField] private List<SkillData> _aquaEPool = new List<SkillData>();
        [Header("水人Q技能卡池")]
        [SerializeField] private List<SkillData> _aquaQPool = new List<SkillData>();
        [Header("火人E技能卡池")]
        [SerializeField] private List<SkillData> _ignisEPool = new List<SkillData>();
        [Header("火人Q技能卡池")]
        [SerializeField] private List<SkillData> _ignisQPool = new List<SkillData>();
        [Header("抽卡权重（普通/稀有/史诗）")]
        [SerializeField] private float[] _drawWeights = { 0.5f, 0.35f, 0.15f };

        [Header("被动技能卡池")]
        [SerializeField] private List<SkillData> _passivePool = new List<SkillData>();

        [Header("技能基础伤害")]
        [SerializeField] private float _baseSkillDamage = 50f;

        public List<SkillData> AquaEPool => _aquaEPool;
        public List<SkillData> AquaQPool => _aquaQPool;
        public List<SkillData> IgnisEPool => _ignisEPool;
        public List<SkillData> IgnisQPool => _ignisQPool;
        public List<SkillData> PassivePool => _passivePool;
        public float[] DrawWeights => _drawWeights;
        public float BaseSkillDamage => _baseSkillDamage;
    }
}
