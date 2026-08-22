/// ============================================================
/// 文件名: NetworkCharacterReporter.cs
/// 创建时间: 2026-08-16
/// 最后更新: 2026-08-22
/// 作者: DualEnigma
/// 描述: 本地角色网络上报器。固定频率上报自身位置/速度/动画/朝向/HP/能量，
///       经服务器转发给对方客户端（限频由 GameConnection 内部 20Hz 节流）。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Shelter;
using DualEnigma.Network;

namespace DualEnigma.Character
{
    [RequireComponent(typeof(CharacterController))]
    public class NetworkCharacterReporter : MonoBehaviour
    {
        private CharacterController _controller;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            if (_controller == null || _rb == null) return;
            if (!GameConnection.HasInstance) return;

            int hp = GameManager.HasInstance
                ? (_controller.PlayerId == 0 ? GameManager.Instance.AquaHP : GameManager.Instance.IgnisHP)
                : 100;
            float energy = ShelterSystem.HasInstance
                ? (_controller.PlayerId == 0 ? ShelterSystem.Instance.AquaEnergy : ShelterSystem.Instance.IgnisEnergy)
                : 100f;

            GameConnection.Instance.SendHighFreqState(
                _rb.position,
                _rb.velocity,
                _controller.CurrentAnimState,
                _controller.FacingRight,
                hp,
                energy);
        }
    }
}
