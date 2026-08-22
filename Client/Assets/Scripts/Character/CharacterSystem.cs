/// ============================================================
/// 文件名: CharacterSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色系统管理器，管理水人和火人角色实例。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Art;
using DualEnigma.Network;
using DualEnigma.Synthesis;

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

        private bool _isInitialized;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<ICharacterSystem>(this);

            // 经济链事件：碎片接住 → 入背包；材料产出 → 入背包
            EventBus.Instance.Subscribe<FragmentCollectedEvent>(OnFragmentCollected);
            EventBus.Instance.Subscribe<MaterialProducedEvent>(OnMaterialProduced);

            Debug.Log("[CharacterSystem] 角色系统初始化完成");
        }

        protected override void OnDestroy()
        {
            if (EventBus.HasInstance)
            {
                EventBus.Instance.Unsubscribe<FragmentCollectedEvent>(OnFragmentCollected);
                EventBus.Instance.Unsubscribe<MaterialProducedEvent>(OnMaterialProduced);
            }
            base.OnDestroy();
        }

        /// <summary>
        /// 碎片收集完成（本地判定/同接仲裁后由 FragmentSystem 发布）→ 加入对应玩家背包。
        /// 联机双端各自跑同一份判定（对方接住经 S2C_FragmentResult 驱动本地发布），背包双端一致。
        /// </summary>
        private void OnFragmentCollected(FragmentCollectedEvent e)
        {
            CharacterController character = GetCharacter((CharacterType)e.playerId);
            if (character == null) return;

            if (!character.AddFragment(e.fragmentId))
            {
                // 背包满：倍率合成前置约束，保持与打断返还一致的告警级别
                Debug.Log($"[CharacterSystem] 玩家{e.playerId}背包已满，碎片{e.fragmentId}未入包（倍率×{e.multiplier}）");
            }
        }

        /// <summary>合成产出（SynthesisSystem 发布）→ 加入对应玩家材料背包</summary>
        private void OnMaterialProduced(MaterialProducedEvent e)
        {
            CharacterController character = GetCharacter((CharacterType)e.playerId);
            if (character == null) return;

            character.AddMaterial((MaterialType)e.materialType, e.count);
        }

        /// <summary>
        /// 初始化角色系统，创建角色 GameObject。
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            Aqua = CreateCharacter(CharacterType.Aqua, 0);
            Ignis = CreateCharacter(CharacterType.Ignis, 1);
            _isInitialized = true;
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

            Vector2 spawnPosition = _characterConfig != null
                ? _characterConfig.GetSpawnPosition(type)
                : (type == CharacterType.Aqua ? new Vector2(-2f, 0f) : new Vector2(2f, 0f));
            go.transform.position = spawnPosition;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.freezeRotation = true;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CharacterSpriteGenerator.GenerateCharacterSprite(type);

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

            // 网络模式下按 本地/远程 区分挂载：本地=输入+上报，远程=插值驱动
            bool networked = RoomSession.HasInstance && RoomSession.Instance.IsConnected;
            bool isLocal = !networked || playerId == RoomSession.Instance.LocalPlayerId;

            if (isLocal)
            {
                CharacterInputController input = go.AddComponent<CharacterInputController>();
                // 联机：本地角色一律 WASD+Space（各自键盘）；单机：水人 WASD / 火人方向键
                input.SetScheme(networked
                    ? InputScheme.WASD
                    : (playerId == 0 ? InputScheme.WASD : InputScheme.Arrows));
                if (networked)
                    go.AddComponent<NetworkCharacterReporter>();       // 联机：本地角色上报
            }
            else
            {
                controller.IsRemoteControlled = true;
                rb.bodyType = RigidbodyType2D.Kinematic;              // 远程角色不参与本地物理模拟
                go.AddComponent<RemoteCharacterDriver>();
            }

            return controller;
        }

        /// <summary>
        /// 联机开局时重建角色：GameLaunch 启动时已按单机模式创建（双本地输入），
        /// 进入联机对局后需按 本地/远程 角色重新生成。
        /// </summary>
        public void RebuildForNetwork()
        {
            if (Aqua != null) Destroy(Aqua.gameObject);
            if (Ignis != null) Destroy(Ignis.gameObject);
            Aqua = null;
            Ignis = null;
            _isInitialized = false;

            Initialize();
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
