/// ============================================================
/// 文件名: CharacterConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色配置数据（ScriptableObject）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色配置数据。
    /// 引用：角色系统.md §2.3
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterConfig", menuName = "DualEnigma/CharacterConfig")]
    public class CharacterConfig : ScriptableObject
    {
        [Header("水人配置")]
        public CharacterStats AquaStats;

        [Header("火人配置")]
        public CharacterStats IgnisStats;

        [Header("生成位置")]
        [SerializeField] private Vector2 _aquaSpawnPosition = new Vector2(-2f, 0f);
        [SerializeField] private Vector2 _ignisSpawnPosition = new Vector2(2f, 0f);

        public Vector2 GetSpawnPosition(CharacterType type)
        {
            return type == CharacterType.Aqua ? _aquaSpawnPosition : _ignisSpawnPosition;
        }

        private void Reset()
        {
            AquaStats = new CharacterStats
            {
                Type = CharacterType.Aqua,
                MaxHP = 100,
                CurrentHP = 100,
                MoveSpeed = 4f,
                JumpHeight = 2f,
                CanDoubleJump = false,
                CarryLimit = 3
            };

            IgnisStats = new CharacterStats
            {
                Type = CharacterType.Ignis,
                MaxHP = 100,
                CurrentHP = 100,
                MoveSpeed = 4f,
                JumpHeight = 2f,
                CanDoubleJump = true,
                CarryLimit = 3
            };
        }
    }
}
