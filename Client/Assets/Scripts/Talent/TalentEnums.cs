/// ============================================================
/// 文件名: TalentEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 天赋系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Talent
{
    /// <summary>
    /// 天赋目标类型
    /// </summary>
    public enum TalentTarget
    {
        /// <summary>水人专属</summary>
        Aqua,
        /// <summary>火人专属</summary>
        Ignis,
        /// <summary>共享</summary>
        Shared,
    }

    /// <summary>
    /// 天赋效果类型
    /// </summary>
    public enum TalentEffectType
    {
        /// <summary>数值叠加类（HP+20、能量+30等）</summary>
        NumericAdd,
        /// <summary>百分比类（冷却-20%、范围+30%等）</summary>
        PercentageModify,
        /// <summary>机制改变类（双重释放等）</summary>
        MechanismChange,
        /// <summary>条件触发类（HP<30%时触发等）</summary>
        ConditionalTrigger,
    }

    /// <summary>
    /// 天赋具体效果标识，用于替代字符串匹配。
    /// </summary>
    public enum TalentEffectId
    {
        None,
        HPBonus,
        EnergyMaxBonus,
        EnergyRecovery,
        ShelterDistance,
        DamageReduction,
        CooldownReduction,
        RangeMultiplier,
        CarryLimit,
        MoveSpeed,
        DoubleRelease,
        PassiveChance,
        SynthesisSpeed,
        FragmentAttract,
        FragmentLifetime,
        BuildingHP,
        PlaceSpeed,
    }
}
