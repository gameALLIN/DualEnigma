/// ============================================================
/// 文件名: NetworkEvents.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 联机房间事件定义。由 GameConnection（R3 前 GameServerClient）发布，
///       UIRoom / 全局弹窗等 UI 订阅。
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Network
{
    /// <summary>已加入房间（收到 S2C_ConnectAck）</summary>
    public struct RoomConnectedEvent : IEventData
    {
        /// <summary>服务器分配的玩家 ID（0/1）</summary>
        public int playerId;

        /// <summary>房间码</summary>
        public string roomCode;
    }

    /// <summary>对局开始（收到 S2C_GameStart，双人满员）</summary>
    public struct RoomGameStartEvent : IEventData
    {
        public int chapter;
        public int section;
        public int round;
    }

    /// <summary>对手断线（收到 S2C_OpponentDisconnect）</summary>
    public struct OpponentDisconnectEvent : IEventData
    {
        /// <summary>断线玩家 ID</summary>
        public int playerId;

        /// <summary>断线场景："lobby" = 大厅离开（可补位再邀） | "waiting" = 对局中断线（重连窗口，断线重连里程碑处理）</summary>
        public string state;
    }

    /// <summary>有玩家加入房间（收到 S2C_PlayerJoined，房间内广播）</summary>
    public struct PlayerJoinedRoomEvent : IEventData
    {
        /// <summary>新加入的玩家 ID</summary>
        public int playerId;

        /// <summary>当前房间人数</summary>
        public int playerCount;
    }

    /// <summary>与 game-server 的连接断开</summary>
    public struct ServerDisconnectedEvent : IEventData
    {
        /// <summary>断开原因（连接失败/网络中断等）</summary>
        public string reason;
    }

    /// <summary>收到对方高频状态（20Hz，GameConnection 发布）</summary>
    public struct HighFreqStateReceivedEvent : IEventData
    {
        public byte playerId;
        public Vector2 position;
        public Vector2 velocity;
        public string animState;
        public bool facing;
    }

    /// <summary>
    /// 请求回执失败（R5：S2C_Resp.code != 0 或本地超时 -1，由 GameConnection 发布）。
    /// code 取 NetErrorCode；与 ServerDisconnectedEvent（连接层）严格分层。
    /// </summary>
    public struct NetworkErrorEvent : IEventData
    {
        /// <summary>失败码（NetErrorCode；-1=本地回执超时）</summary>
        public int code;

        /// <summary>服务器文案（本地超时为客户端兜底文案）</summary>
        public string message;

        /// <summary>来源请求类型（如 "C2S_Connect" / "C2S_StartGame"）</summary>
        public string source;
    }
}
