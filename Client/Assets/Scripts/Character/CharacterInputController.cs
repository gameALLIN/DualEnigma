/// ============================================================
/// 文件名: CharacterInputController.cs
/// 创建时间: 2026-08-13
/// 作者: DualEnigma
/// 描述: 角色输入控制器，将键鼠输入映射到角色移动和跳跃。
///       本地测试用：WASD 控制水人，方向键控制火人。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Character
{
    /// <summary>
    /// 角色输入控制器。挂载在角色 GameObject 上，在 Update 中读取输入，
    /// 在 FixedUpdate 中通过 CharacterController 应用移动和跳跃。
    /// 引用：角色系统.md §3.2
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterInputController : MonoBehaviour
    {
        /// <summary>水平移动方向缓存（-1/0/1）</summary>
        private float _moveDirection;

        /// <summary>跳跃按键缓存</summary>
        private bool _jumpPressed;

        private CharacterController _controller;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_controller.PlayerId == 0)
            {
                // 水人：A/D 移动，W/Space 跳跃
                _moveDirection = 0f;
                if (Input.GetKey(KeyCode.A)) _moveDirection -= 1f;
                if (Input.GetKey(KeyCode.D)) _moveDirection += 1f;
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
                    _jumpPressed = true;
            }
            else
            {
                // 火人：方向键移动，上方向键跳跃
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
