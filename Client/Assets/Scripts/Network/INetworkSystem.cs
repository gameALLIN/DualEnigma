/// ============================================================
/// 文件名: INetworkSystem.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 网络系统服务接口。
/// ============================================================

using UnityEngine;
using DualEnigma.Character;

namespace DualEnigma.Network
{
    /// <summary>
    /// 网络系统服务接口，注册到 ServiceLocator。
    /// 引用：网络通信.md §二 同步架构选型
    /// </summary>
    public interface INetworkSystem
    {
        /// <summary>当前网络角色</summary>
        NetworkRole Role { get; }

        /// <summary>是否已连接</summary>
        bool IsConnected { get; }

        /// <summary>当前往返延迟（秒）</summary>
        float RoundTripTime { get; }

        /// <summary>发送高频状态</summary>
        void SendHighFrequencyState(byte playerId, Vector2 position, Vector2 velocity, AnimState animState, bool facing);

        /// <summary>发送中频状态</summary>
        void SendMidFrequencyState(byte playerId, int hp, float shelterEnergy);

        /// <summary>发送关键事件</summary>
        void SendKeyEvent(ushort eventType, byte playerId, uint timestamp, byte[] payload);

        /// <summary>创建房间（Host）</summary>
        bool CreateRoom(string roomName);

        /// <summary>加入房间（Client）</summary>
        bool JoinRoom(string roomName);

        /// <summary>断开连接</summary>
        void Disconnect();
    }
}
