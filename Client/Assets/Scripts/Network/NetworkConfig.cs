/// ============================================================
/// 文件名: NetworkConfig.cs
/// 创建时间: 2026-07-13
/// 最后更新: 2026-08-22
/// 作者: DualEnigma
/// 描述: 网络系统配置数据（ScriptableObject）。R3 清理：
///       删除无消费方的遗留字段（TCP/UDP 端口、带宽、预测补偿、心跳超时、
///       重连窗口等 Protobuf/UDP 方案残留），仅保留现行消费方在用的配置。
///       心跳间隔为协议常量（GameConnection.HEARTBEAT_INTERVAL，与服务端对齐）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Network
{
    /// <summary>
    /// 网络系统配置。
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "DualEnigma/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("同步频率")]
        [Tooltip("高频状态上报频率（Hz），GameConnection 内部节流")]
        [SerializeField] private float _highFrequencyRate = 20f;

        [Header("远程角色插值（RemoteCharacterDriver）")]
        [SerializeField] private float _interpolationBuffer = 0.1f;

        [Tooltip("远程角色停滞阈值（秒）：超过该时长未收到新高频包进入外推模式，默认 0.5s")]
        [SerializeField] private float _stallThreshold = 0.5f;
        [Tooltip("外推上限（秒）：用最后速度外推的最大时长，超过即进入失联态，默认 0.1s（同步策略.md §2.3）")]
        [SerializeField] private float _maxExtrapolationTime = 0.1f;
        [Tooltip("失联态角色透明度（0-1），默认 0.5")]
        [SerializeField, Range(0f, 1f)] private float _disconnectedAlpha = 0.5f;
        [Tooltip("重连吸附时长（秒）：恢复收包后从当前渲染位置平滑过渡到权威位置，默认 0.2s")]
        [SerializeField] private float _resnapDuration = 0.2f;

        [Header("账号服 REST API")]
        [SerializeField] private string _accountServerUrl = "http://localhost:8081";

        [Header("游戏服 WebSocket")]
        [SerializeField] private string _gameServerWsUrl = "ws://localhost:8080/game";

        public float HighFrequencyRate => _highFrequencyRate;
        public float InterpolationBuffer => _interpolationBuffer;
        public float StallThreshold => _stallThreshold;
        public float MaxExtrapolationTime => _maxExtrapolationTime;
        public float DisconnectedAlpha => _disconnectedAlpha;
        public float ResnapDuration => _resnapDuration;
        public string AccountServerUrl => _accountServerUrl;
        public string GameServerWsUrl => _gameServerWsUrl;
    }
}
