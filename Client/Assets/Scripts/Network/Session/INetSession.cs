/// ============================================================
/// 文件名: INetSession.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 会话只读接口（ServiceLocator 注册用）。替代旧 INetworkSystem：
///       只暴露读状态，写入路径收敛到 RoomSession 内部（单一事实来源）。
/// 引用：RoomSession.cs
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>会话只读视图（业务系统经 ServiceLocator 查询连接/房间状态）</summary>
    public interface INetSession
    {
        /// <summary>连接阶段（Disconnected/Connecting/Connected/InRoom）</summary>
        NetPhase Phase { get; }

        /// <summary>传输层已连接（握手完成，未必已进房）</summary>
        bool IsConnected { get; }

        /// <summary>已在房间（收到 ConnectAck）</summary>
        bool IsInRoom { get; }

        /// <summary>当前房间码</summary>
        string CurrentRoomCode { get; }

        /// <summary>本地玩家 ID（ConnectAck 分配，0/1）</summary>
        byte LocalPlayerId { get; }

        /// <summary>对手玩家 ID</summary>
        byte OpponentId { get; }

        /// <summary>对手 HP（服务器 10Hz 快照）</summary>
        int OpponentHP { get; }

        /// <summary>对手庇护能量（服务器 10Hz 快照）</summary>
        float OpponentShelterEnergy { get; }
    }

    /// <summary>会话阶段（严格单向迁移，见 RoomSession）</summary>
    public enum NetPhase
    {
        /// <summary>未连接</summary>
        Disconnected = 0,

        /// <summary>连接中（握手/等待进房 Ack）</summary>
        Connecting = 1,

        /// <summary>传输层已连接（等待 ConnectAck）</summary>
        Connected = 2,

        /// <summary>已在房间</summary>
        InRoom = 3,
    }
}
