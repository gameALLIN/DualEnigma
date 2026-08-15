/// ============================================================
/// 文件名: RemoteCharacterDriver.cs
/// 创建时间: 2026-08-16
/// 作者: DualEnigma
/// 描述: 远程角色驱动器。订阅 HighFreqStateReceivedEvent，
///       以插值缓冲（默认 100ms）回放对方位置，平滑网络抖动。
///       组件挂在远程角色上（Kinematic 刚体，不参与本地物理模拟）。
/// ============================================================

using System.Collections.Generic;
using UnityEngine;
using DualEnigma.Framework.Core;
using DualEnigma.Data;
using DualEnigma.Network;

namespace DualEnigma.Character
{
    [RequireComponent(typeof(CharacterController))]
    public class RemoteCharacterDriver : MonoBehaviour
    {
        private struct Sample
        {
            public float Time;
            public Vector2 Position;
        }

        private CharacterController _controller;
        private Rigidbody2D _rb;
        private readonly List<Sample> _buffer = new List<Sample>();
        private float _bufferSec = 0.1f;
        private string _lastAnim = "";
        private bool _lastFacing = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _rb = GetComponent<Rigidbody2D>();

            NetworkConfig config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
            if (config != null) _bufferSec = config.InterpolationBuffer;
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<HighFreqStateReceivedEvent>(OnStateReceived);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<HighFreqStateReceivedEvent>(OnStateReceived);
        }

        private void OnStateReceived(HighFreqStateReceivedEvent e)
        {
            if (_controller == null || e.playerId != _controller.PlayerId) return;

            _buffer.Add(new Sample { Time = Time.time, Position = e.position });
            if (_buffer.Count > 40) _buffer.RemoveAt(0); // ~2s @20Hz 上限，防内存增长

            if (e.animState != _lastAnim)
            {
                _lastAnim = e.animState;
                if (System.Enum.TryParse(_lastAnim, out AnimState anim))
                    _controller.SetNetworkAnimState(anim);
            }

            if (e.facing != _lastFacing)
            {
                _lastFacing = e.facing;
                _controller.SetNetworkFacing(e.facing);
            }
        }

        private void Update()
        {
            if (_buffer.Count < 2 || _rb == null) return;

            float renderTime = Time.time - _bufferSec;

            // 丢弃被追上的过期样本（保留最近 2 个用于插值）
            while (_buffer.Count > 2 && _buffer[1].Time <= renderTime)
                _buffer.RemoveAt(0);

            Sample a = _buffer[0];
            Sample b = _buffer[1];
            float span = b.Time - a.Time;
            float t = span > 0f ? Mathf.Clamp01((renderTime - a.Time) / span) : 1f;

            _rb.position = Vector2.Lerp(a.Position, b.Position, t);
        }
    }
}
