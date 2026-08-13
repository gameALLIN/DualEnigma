/// ============================================================
/// 文件名: NetworkConfig.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 网络系统配置数据（ScriptableObject）。
/// ============================================================

using UnityEngine;

namespace DualEnigma.Network
{
    /// <summary>
    /// 网络系统配置。
    /// 引用：网络通信.md §七 网络参数建议
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "DualEnigma/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("同步频率")]
        [SerializeField] private float _highFrequencyRate = 20f;
        [SerializeField] private float _midFrequencyRate = 10f;

        [Header("延迟补偿")]
        [SerializeField] private float _interpolationBuffer = 0.1f;
        [SerializeField] private float _correctionThreshold = 0.5f;
        [SerializeField] private int _predictionCacheFrames = 10;
        [SerializeField] private float _maxPredictionDistance = 2f;

        [Header("心跳与超时")]
        [SerializeField] private float _heartbeatInterval = 1f;
        [SerializeField] private float _heartbeatTimeout = 5f;
        [SerializeField] private float _reconnectWindow = 30f;
        [SerializeField] private float _aiTakeoverTimeout = 30f;
        [SerializeField] private float _finalTimeout = 120f;

        [Header("带宽")]
        [SerializeField] private float _maxBandwidthKBps = 50f;

        [Header("端口与地址")]
        [SerializeField] private int _tcpPort = 7777;
        [SerializeField] private int _udpPort = 7778;
        [SerializeField] private string _defaultHostAddress = "127.0.0.1";

        [Header("账号服 REST API")]
        [SerializeField] private string _accountServerUrl = "http://localhost:8081";

        public float HighFrequencyRate => _highFrequencyRate;
        public float MidFrequencyRate => _midFrequencyRate;
        public float InterpolationBuffer => _interpolationBuffer;
        public float CorrectionThreshold => _correctionThreshold;
        public int PredictionCacheFrames => _predictionCacheFrames;
        public float MaxPredictionDistance => _maxPredictionDistance;
        public float HeartbeatInterval => _heartbeatInterval;
        public float HeartbeatTimeout => _heartbeatTimeout;
        public float ReconnectWindow => _reconnectWindow;
        public float AiTakeoverTimeout => _aiTakeoverTimeout;
        public float FinalTimeout => _finalTimeout;
        public float MaxBandwidthKBps => _maxBandwidthKBps;
        public int TcpPort => _tcpPort;
        public int UdpPort => _udpPort;
        public string DefaultHostAddress => _defaultHostAddress;
        public string AccountServerUrl => _accountServerUrl;
    }
}
