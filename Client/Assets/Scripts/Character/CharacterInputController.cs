/// ============================================================
/// 文件名: CharacterInputController.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 角色输入控制器，将键鼠输入映射到角色移动和跳跃。
///       输入方案：联机模式本地角色一律 WASD+Space；
///       单机双人同屏保持分键（水人 WASD / 火人方向键）。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Character
{
    /// <summary>输入方案</summary>
    public enum InputScheme
    {
        /// <summary>WASD 移动 + W/Space 跳跃</summary>
        WASD,
        /// <summary>方向键移动 + ↑ 跳跃（单机双人同屏的火人方案）</summary>
        Arrows,
    }

    /// <summary>
    /// 角色输入控制器。挂载在角色 GameObject 上，在 Update 中读取输入，
    /// 在 FixedUpdate 中通过 CharacterController 应用移动和跳跃。
    /// 引用：角色系统.md §3.2
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterInputController : MonoBehaviour
    {
        /// <summary>当前输入方案（CharacterSystem 创建时按 单机/联机 指定）</summary>
        [SerializeField] private InputScheme m_Scheme = InputScheme.WASD;

        /// <summary>水平移动方向缓存（-1/0/1）</summary>
        private float _moveDirection;

        /// <summary>跳跃按键缓存</summary>
        private bool _jumpPressed;

        private CharacterController _controller;

        /// <summary>设置输入方案（创建角色后立即调用，晚于 Awake 也可生效）</summary>
        public void SetScheme(InputScheme scheme)
        {
            m_Scheme = scheme;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (m_Scheme == InputScheme.WASD)
            {
                // WASD：A/D 移动，W/Space 跳跃
                _moveDirection = 0f;
                if (Input.GetKey(KeyCode.A)) _moveDirection -= 1f;
                if (Input.GetKey(KeyCode.D)) _moveDirection += 1f;
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
                    _jumpPressed = true;
            }
            else
            {
                // 方向键：←→ 移动，↑ 跳跃
                _moveDirection = 0f;
                if (Input.GetKey(KeyCode.LeftArrow)) _moveDirection -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) _moveDirection += 1f;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _jumpPressed = true;
            }
        }

        private void FixedUpdate()
        {
            _controller.Move(_moveDirection);

            if (_jumpPressed)
            {
                _controller.Jump();
                _jumpPressed = false;
            }
        }
    }
}
