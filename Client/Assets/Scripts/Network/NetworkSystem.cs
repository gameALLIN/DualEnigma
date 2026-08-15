/// ============================================================
/// 文件名: NetworkSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 网络系统管理器，管理联机同步、房间管理和延迟补偿。
/// ============================================================

using UnityEngine;
using DualEnigma.Core;
using DualEnigma.Framework.Core;
using DualEnigma.Character;
using DualEnigma.Data;

namespace DualEnigma.Network
{
    /// <summary>
    /// 网络系统管理器。继承 Singleton<T>，注册 INetworkSystem 到 ServiceLocator。
    /// 引用：网络通信.md §二 同步架构选型
    /// </summary>
    public class NetworkSystem : Singleton<NetworkSystem>, INetworkSystem
    {
        /// <summary>当前网络角色</summary>
        public NetworkRole Role { get; private set; } = NetworkRole.Host;

        /// <summary>是否已连接</summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 当前房间码。由服务器 ConnectAck 分配（WebSocket 通道接通后写入），
        /// 好友面板读取它向好友发起邀请。
        /// </summary>
        public string CurrentRoomCode { get; private set; } = "";

        /// <summary>设置当前房间码（收到 S2C_ConnectAck 时调用）</summary>
        public void SetRoomCode(string roomCode)
        {
            CurrentRoomCode = roomCode ?? "";
        }

        /// <summary>当前往返延迟（秒）</summary>
        public float RoundTripTime { get; private set; }

        /// <summary>网络配置</summary>
        private NetworkConfig _config;

        /// <summary>高频状态发送计时器</summary>
        private float _highFreqTimer;
        /// <summary>中频状态发送计时器</summary>
        private float _midFreqTimer;
        /// <summary>心跳计时器</summary>
        private float _heartbeatTimer;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<INetworkSystem>(this);
            _config = DataManager.Instance.LoadConfig<NetworkConfig>("NetworkConfig");
            Debug.Log("[NetworkSystem] 网络系统初始化完成 (Host模式)");
        }

        /// <summary>
        /// 发送高频状态。
        /// </summary>
        public void SendHighFrequencyState(byte playerId, Vector2 position, Vector2 velocity, AnimState animState, bool facing)
        {
            if (!IsConnected) return;

            _highFreqTimer += Time.deltaTime;
            float interval = 1f / (_config != null ? _config.HighFrequencyRate : 20f);

            if (_highFreqTimer < interval)
                return;

            _highFreqTimer = 0f;
        }

        /// <summary>
        /// 发送中频状态。
        /// </summary>
        public void SendMidFrequencyState(byte playerId, int hp, float shelterEnergy)
        {
            if (!IsConnected) return;

            _midFreqTimer += Time.deltaTime;
            float interval = 1f / (_config != null ? _config.MidFrequencyRate : 10f);

            if (_midFreqTimer < interval)
                return;

            _midFreqTimer = 0f;
        }

        /// <summary>
        /// 发送关键事件。
        /// </summary>
        public void SendKeyEvent(ushort eventType, byte playerId, uint timestamp, byte[] payload)
        {
            if (!IsConnected) return;
        }

        /// <summary>
        /// 创建房间（Host）。
        /// </summary>
        public bool CreateRoom(string roomName)
        {
            Role = NetworkRole.Host;
            IsConnected = true;
            Debug.Log($"[NetworkSystem] 创建房间: {roomName} (Host)");
            return true;
        }

        /// <summary>
        /// 加入房间（Client）。
        /// </summary>
        public bool JoinRoom(string roomName)
        {
            Role = NetworkRole.Client;
            IsConnected = true;
            Debug.Log($"[NetworkSystem] 加入房间: {roomName} (Client)");
            return true;
        }

        /// <summary>
        /// 断开连接。
        /// </summary>
        public void Disconnect()
        {
            IsConnected = false;
            Debug.Log("[NetworkSystem] 断开连接");
        }

        private void Update()
        {
            if (!IsConnected) return;

            _heartbeatTimer += Time.deltaTime;
            float heartbeatInterval = _config != null ? _config.HeartbeatInterval : 1f;

            if (_heartbeatTimer >= heartbeatInterval)
            {
                _heartbeatTimer = 0f;
            }
        }
    }
}
