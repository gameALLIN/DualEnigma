/// ============================================================
/// 文件名: CharacterController.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 角色控制器，负责移动、跳跃、碎片交互、朝向管理。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Core;
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
        /// <summary>Ground 层索引（Layer 7）</summary>
        private const int GROUND_LAYER = 7;

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

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private bool _hasDoubleJumped;

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
            CheckGrounded();
            UpdateAnimState();
        }

        /// <summary>
        /// 移动角色。
        /// </summary>
        /// <param name="direction">-1（左）到 1（右）</param>
        public void Move(float direction)
        {
            if (Stats == null) return;

            float speed = Stats.MoveSpeed;
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
        /// 受伤害。
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (Stats == null) return;

            Stats.CurrentHP -= damage;
            EventBus.Instance.Publish(new PlayerDamagedEvent
            {
                playerId = PlayerId,
                damage = damage
            });

            if (Stats.CurrentHP <= 0)
            {
                Stats.CurrentHP = 0;
                EventBus.Instance.Publish(new PlayerDiedEvent
                {
                    playerId = PlayerId
                });
            }
        }

        /// <summary>
        /// 治疗。
        /// </summary>
        public void Heal(int amount)
        {
            if (Stats == null) return;

            Stats.CurrentHP = Mathf.Min(Stats.CurrentHP + amount, Stats.MaxHP);
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

            float rayDistance = _collider.bounds.extents.y + 0.15f;
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                rayDistance,
                1 << GROUND_LAYER);

            IsGrounded = hit.collider != null;
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
