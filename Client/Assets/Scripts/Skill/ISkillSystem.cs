/// ============================================================
/// 文件名: ISkillSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 技能系统服务接口。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Character;

namespace DualEnigma.Skill
{
    /// <summary>
    /// 技能系统服务接口，注册到 ServiceLocator。
    /// 引用：技能系统.md §3.1
    /// </summary>
    public interface ISkillSystem
    {
        /// <summary>抽取卡牌</summary>
        List<SkillData> DrawCards(CharacterType owner, SkillType type);

        /// <summary>选择卡牌</summary>
        void SelectCard(CharacterType owner, SkillType type, int skillId);

        /// <summary>释放技能</summary>
        void ActivateSkill(CharacterType owner, SkillType type, Vector2 targetPosition);

        /// <summary>每帧更新（冷却倒计时）</summary>
        void OnUpdate(float deltaTime);
    }
}
