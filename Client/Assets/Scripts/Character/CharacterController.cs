/// ============================================================
/// 文件名: CharacterController.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色控制器，负责移动、跳跃、碎片交互、朝向管理。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Synthesis;
using DualEnigma.Fragment;
using DualEnigma.Shelter;

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色控制器，挂载在角色 GameObject 上。
    /// 负责移动、跳跃、碎片交互、朝向管理。
    /// 引用：角色系统.md §3.2
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterController : MonoBehaviour
    {
        /// <summary>Ground 层索引（动态获取）</summary>
        private static int GROUND_LAYER = -1;

        /// <summary>角色属性</summary>
        public CharacterStats Stats { get; private set; }

        /// <summary>玩家ID（0=Aqua, 1=Ignis）</summary>
        public byte PlayerId { get; private set; }

        /// <summary>朝向（true=右，false=左）</summary>
        public bool FacingRight { get; private set; } = true;

        /// <summary>是否在地面上</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>动画状态</summary>
        public AnimState CurrentAnimState { get; private set; } = AnimState.Idle;

        /// <summary>是否由网络驱动（远程角色：跳过本地动画推导/地面检测）</summary>
        public bool IsRemoteControlled { get; set; }

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private bool _hasDoubleJumped;

        /// <summary>移动速度乘数（受环境效果影响，如暴风雪）</summary>
        private float _moveSpeedMultiplier = 1f;

        /// <summary>
        /// 初始化角色控制器。
        /// </summary>
        public void Initialize(CharacterStats stats, byte playerId)
        {
            Stats = stats;
            PlayerId = playerId;
            _hasDoubleJumped = false;
        }

        private void Awake()
        {
            if (GROUND_LAYER < 0)
                GROUND_LAYER = LayerMask.NameToLayer("Ground");

            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();

            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider2D>();
        }

        private void FixedUpdate()
        {
            if (IsRemoteControlled) return; // 远程角色：动画/朝向/位置均由网络驱动

            CheckGrounded();
            UpdateAnimState();
        }

        /// <summary>网络下发动画状态（远程角色专用，RemoteCharacterDriver 调用）</summary>
        public void SetNetworkAnimState(AnimState state)
        {
            CurrentAnimState = state;
        }

        /// <summary>网络下发朝向（复用本地翻转逻辑，RemoteCharacterDriver 调用）</summary>
        public void SetNetworkFacing(bool facingRight)
        {
            if (facingRight != FacingRight)
                Flip();
        }

        /// <summary>
        /// 移动角色。
        /// </summary>
        /// <param name="direction">-1（左）到 1（右）</param>
        public void Move(float direction)
        {
            if (Stats == null) return;

            direction = Mathf.Clamp(direction, -1f, 1f);
            float speed = Stats.MoveSpeed * _moveSpeedMultiplier;
            _rb.velocity = new Vector2(direction * speed, _rb.velocity.y);

            if (direction > 0.01f && !FacingRight)
                Flip();
            else if (direction < -0.01f && FacingRight)
                Flip();
        }

        /// <summary>
        /// 跳跃。水人单跳，火人可二段跳。
        /// </summary>
        public void Jump()
        {
            if (Stats == null) return;

            if (IsGrounded)
            {
                ApplyJumpForce(Stats.JumpHeight);
                _hasDoubleJumped = false;
            }
            else if (Stats.CanDoubleJump && !_hasDoubleJumped)
            {
                ApplyJumpForce(Stats.JumpHeight * 0.8f);
                _hasDoubleJumped = true;
            }
        }

        /// <summary>
        /// 添加携带碎片。
        /// </summary>
        /// <returns>是否添加成功</returns>
        public bool AddFragment(int fragmentId)
        {
            if (Stats == null || Stats.CarriedFragmentIds.Count >= Stats.CarryLimit)
                return false;

            if (Stats.CarriedFragmentIds.Contains(fragmentId))
                return false;

            Stats.CarriedFragmentIds.Add(fragmentId);
            return true;
        }

        /// <summary>
        /// 移除携带碎片。
        /// </summary>
        public void RemoveFragment(int fragmentId)
        {
            if (Stats != null)
                Stats.CarriedFragmentIds.Remove(fragmentId);
        }

        /// <summary>
        /// 尝试消耗携带的材料。
        /// </summary>
        /// <param name="type">材料类型</param>
        /// <param name="count">消耗数量</param>
        /// <returns>数量足够且扣除成功返回 true，否则 false</returns>
        public bool TryConsumeMaterial(MaterialType type, int count = 1)
        {
            if (Stats == null) return false;

            if (!Stats.CarriedMaterials.ContainsKey(type) || Stats.CarriedMaterials[type] < count)
                return false;

            Stats.CarriedMaterials[type] -= count;
            if (Stats.CarriedMaterials[type] <= 0)
                Stats.CarriedMaterials.Remove(type);

            return true;
        }

        /// <summary>
        /// 添加携带材料（合成产出时调用）。
        /// </summary>
        /// <param name="type">材料类型</param>
        /// <param name="count">添加数量</param>
        public void AddMaterial(MaterialType type, int count = 1)
        {
            if (Stats == null) return;

            if (!Stats.CarriedMaterials.ContainsKey(type))
                Stats.CarriedMaterials[type] = 0;

            Stats.CarriedMaterials[type] += count;
        }

        /// <summary>
        /// 受伤害。委托 ShelterSystem 处理，由 ShelterSystem 作为HP唯一权威。
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (Stats == null) return;

            IShelterSystem shelterSys = ServiceLocator.Get<IShelterSystem>();
            if (shelterSys != null)
                shelterSys.DealDamage(Stats.Type, damage);

            CurrentAnimState = AnimState.Hurt;
        }

        /// <summary>
        /// 治疗。委托 ShelterSystem 处理，由 ShelterSystem 作为HP唯一权威。
        /// </summary>
        public void Heal(int amount)
        {
            if (Stats == null) return;

            IShelterSystem shelterSys = ServiceLocator.Get<IShelterSystem>();
            if (shelterSys != null)
                shelterSys.Heal(Stats.Type, amount);
        }

        /// <summary>
        /// 设置移动速度乘数（供外部系统调用，如暴风雪环境降低移速）。
        /// </summary>
        /// <param name="multiplier">乘数（1.0=正常，0.5=50%移速）</param>
        public void SetMoveSpeedMultiplier(float multiplier)
        {
            _moveSpeedMultiplier = multiplier;
        }

        /// <summary>
        /// 碰撞检测：碎片收集。
        /// 引用：角色系统.md §4.3 碎片交互
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            FragmentController fragment = other.GetComponent<FragmentController>();
            if (fragment != null)
            {
                bool isJumping = !IsGrounded;
                IFragmentSystem fragSys = ServiceLocator.Get<IFragmentSystem>();
                if (fragSys != null)
                    fragSys.OnFragmentCollected(fragment.FragmentId, PlayerId, isJumping);
            }
        }

        private void ApplyJumpForce(float heightInUnits)
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y * _rb.gravityScale);
            if (gravity <= 0f) return;

            float jumpVelocity = Mathf.Sqrt(2f * gravity * heightInUnits);
            _rb.velocity = new Vector2(_rb.velocity.x, jumpVelocity);
        }

        private void CheckGrounded()
        {
            if (_collider == null) return;

            Bounds bounds = _collider.bounds;
            float rayDistance = bounds.extents.y + 0.15f;
            int groundMask = 1 << GROUND_LAYER;
            float halfWidth = bounds.extents.x;
            Vector2 center = transform.position;

            IsGrounded = Physics2D.Raycast(center, Vector2.down, rayDistance, groundMask).collider != null
                || Physics2D.Raycast(new Vector2(center.x - halfWidth, center.y), Vector2.down, rayDistance, groundMask).collider != null
                || Physics2D.Raycast(new Vector2(center.x + halfWidth, center.y), Vector2.down, rayDistance, groundMask).collider != null;
        }

        private void UpdateAnimState()
        {
            if (!IsGrounded)
            {
                CurrentAnimState = _rb.velocity.y > 0 ? AnimState.Jump : AnimState.Fall;
            }
            else if (Mathf.Abs(_rb.velocity.x) > 0.1f)
            {
                CurrentAnimState = AnimState.Run;
            }
            else
            {
                CurrentAnimState = AnimState.Idle;
            }
        }

        private void Flip()
        {
            FacingRight = !FacingRight;
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = !FacingRight;
        }
    }
}
