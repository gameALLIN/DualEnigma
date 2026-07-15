/// ============================================================
/// 文件名: CharacterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色系统管理器，管理水人和火人角色实例。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色系统管理器。继承 Singleton<T>，注册 ICharacterSystem 到 ServiceLocator。
    /// 引用：角色系统.md §3.1
    /// </summary>
    public class CharacterSystem : Singleton<CharacterSystem>, ICharacterSystem
    {
        /// <summary>水人角色实例</summary>
        public CharacterController Aqua { get; private set; }

        /// <summary>火人角色实例</summary>
        public CharacterController Ignis { get; private set; }

        /// <summary>角色配置数据（ScriptableObject）</summary>
        [SerializeField] private CharacterConfig _characterConfig;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ICharacterSystem>(this);
            Debug.Log("[CharacterSystem] 角色系统初始化完成");
        }

        /// <summary>
        /// 初始化角色系统，创建角色 GameObject。
        /// </summary>
        public void Initialize()
        {
            Aqua = CreateCharacter(CharacterType.Aqua, 0);
            Ignis = CreateCharacter(CharacterType.Ignis, 1);
            Debug.Log("[CharacterSystem] 角色实例创建完成");
        }

        /// <summary>
        /// 获取指定类型的角色。
        /// </summary>
        public CharacterController GetCharacter(CharacterType type)
        {
            return type == CharacterType.Aqua ? Aqua : Ignis;
        }

        private CharacterController CreateCharacter(CharacterType type, byte playerId)
        {
            string name = type == CharacterType.Aqua ? "Character_Aqua" : "Character_Ignis";
            GameObject go = new GameObject(name);

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);

            go.AddComponent<SpriteRenderer>();

            CharacterController controller = go.AddComponent<CharacterController>();

            // 从 CharacterConfig 加载属性，配置未赋值时使用默认值
            CharacterStats sourceStats = null;
            if (_characterConfig != null)
            {
                sourceStats = type == CharacterType.Aqua
                    ? _characterConfig.AquaStats
                    : _characterConfig.IgnisStats;
            }

            CharacterStats stats = sourceStats != null
                ? CloneStats(sourceStats)
                : CreateDefaultStats(type);

            controller.Initialize(stats, playerId);

            return controller;
        }

        /// <summary>
        /// 克隆配置属性（深拷贝，避免修改 ScriptableObject 原始数据）。
        /// CurrentHP 重置为 MaxHP，确保新局开始满血。
        /// </summary>
        private CharacterStats CloneStats(CharacterStats source)
        {
            return new CharacterStats
            {
                Type = source.Type,
                MaxHP = source.MaxHP,
                CurrentHP = source.MaxHP,
                MoveSpeed = source.MoveSpeed,
                JumpHeight = source.JumpHeight,
                CanDoubleJump = source.CanDoubleJump,
                CarryLimit = source.CarryLimit,
                CarriedFragmentIds = new System.Collections.Generic.List<int>()
            };
        }

        /// <summary>
        /// 创建默认属性（CharacterConfig 未赋值时的兜底）。
        /// </summary>
        private CharacterStats CreateDefaultStats(CharacterType type)
        {
            return new CharacterStats
            {
                Type = type,
                MaxHP = 100,
                CurrentHP = 100,
                MoveSpeed = 4f,
                JumpHeight = 2f,
                CanDoubleJump = (type == CharacterType.Ignis),
                CarryLimit = 3,
                CarriedFragmentIds = new System.Collections.Generic.List<int>()
            };
        }
    }
}
