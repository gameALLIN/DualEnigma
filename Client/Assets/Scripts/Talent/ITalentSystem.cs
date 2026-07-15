/// ============================================================
/// 文件名: ITalentSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 天赋系统服务接口。
/// ============================================================

using System.Collections.Generic;
using DualEnigma.Character;

namespace DualEnigma.Talent
{
    /// <summary>
    /// 天赋系统服务接口，注册到 ServiceLocator。
    /// 引用：天赋系统.md §3.1
    /// </summary>
    public interface ITalentSystem
    {
        /// <summary>水人已选天赋列表</summary>
        List<TalentData> AquaTalents { get; }

        /// <summary>火人已选天赋列表</summary>
        List<TalentData> IgnisTalents { get; }

        /// <summary>发放3个天赋供选择</summary>
        List<TalentData> DrawTalents(CharacterType owner, int chapter);

        /// <summary>选择天赋</summary>
        void SelectTalent(CharacterType owner, int talentId);

        /// <summary>获取已选天赋的叠加效果</summary>
        TalentEffectSummary GetEffectSummary(CharacterType owner);
    }
}
