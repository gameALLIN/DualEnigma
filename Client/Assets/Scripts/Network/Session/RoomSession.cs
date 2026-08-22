/// ============================================================
/// 文件名: RoomSession.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 房间会话唯一事实来源：连接阶段状态机（NetPhase）、房间码、
///       本地玩家 ID、对手快照。合并旧 NetworkSystem 的真实状态部分。
///       迁移规则：→Connecting 由 GameConnection.ConnectToRoom 触发（清房间态）；
///       Connected→InRoom 由 S2C_ConnectAck；任意→Disconnected 只经
///       MarkDisconnected 统一出口（清全部状态，修复审查报告 I：主动退出
///       状态残留）。对手快照由 GameConnection 的 MidFreq 处理器写入。
/// 引用：INetSession.cs, GameConnection.cs, NetworkEvents.cs
/// ============================================================

using UnityEngine;
using DualEnigma.Framework.Core;

namespace DualEnigma.Network
{
    /// <summary>房间会话（单一事实来源）</summary>
    public class RoomSession : Singleton<RoomSession>, INetSession
    {
        // ── 连接阶段 ──
        public NetPhase Phase { get; private set; } = NetPhase.Disconnected;
        public bool IsConnected => Phase >= NetPhase.Connected;
        public bool IsInRoom => Phase == NetPhase.InRoom;

        // ── 房间 ──
        public string CurrentRoomCode { get; private set; } = "";
        public byte LocalPlayerId { get; private set; }
        public byte OpponentId => (byte)(1 - LocalPlayerId);

        // ── 对手快照（服务器 10Hz）──
        public int OpponentHP { get; private set; } = 100;
        public float OpponentShelterEnergy { get; private set; } = 100f;

        protected override void OnSingletonInitialized()
        {
            ServiceLocator.Register<INetSession>(this);
            Debug.Log("[RoomSession] 房间会话初始化完成");
        }

        /// <summary>开始连接：清房间态（roomCode/playerId），进入 Connecting</summary>
        public void BeginConnecting()
        {
            Phase = NetPhase.Connecting;
            ClearRoomState();
        }

        /// <summary>握手成功：Connecting → Connected（等待 ConnectAck）</summary>
        public void MarkConnected()
        {
            if (Phase == NetPhase.Connecting)
                Phase = NetPhase.Connected;
        }

        /// <summary>收到 ConnectAck：→ InRoom，写入房间码与玩家 ID，发布 RoomConnectedEvent</summary>
        public void EnterRoom(int playerId, string roomCode)
        {
            Phase = NetPhase.InRoom;
            CurrentRoomCode = roomCode ?? "";
            LocalPlayerId = (byte)Mathf.Clamp(playerId, 0, 1);

            if (EventBus.HasInstance)
                EventBus.Instance.Publish(new RoomConnectedEvent { playerId = playerId, roomCode = CurrentRoomCode });

            Debug.Log($"[RoomSession] 已进入房间 {CurrentRoomCode} (playerId={playerId})");
        }

        /// <summary>
        /// 统一断开出口（唯一降到 Disconnected 的路径）：无论主动/异常都清全部状态
        /// （roomCode/playerId/对手快照），异常时额外发布 ServerDisconnectedEvent。
        /// 旧 bug（主动 Disconnect 不清状态 → 主界面死局）从结构上不可能复发。
        /// </summary>
        public void MarkDisconnected(bool manual, string reason = "")
        {
            bool wasConnected = Phase != NetPhase.Disconnected;
            Phase = NetPhase.Disconnected;
            ClearRoomState();

            if (!manual && wasConnected && EventBus.HasInstance)
                EventBus.Instance.Publish(new ServerDisconnectedEvent { reason = reason });

            if (!manual)
                Debug.LogWarning($"[RoomSession] 异常断开: {reason}");
        }

        /// <summary>对手快照更新（MidFreq 处理器调用）</summary>
        public void UpdateOpponentStats(int hp, float shelterEnergy)
        {
            OpponentHP = hp;
            OpponentShelterEnergy = shelterEnergy;
        }

        private void ClearRoomState()
        {
            CurrentRoomCode = "";
            LocalPlayerId = 0;
            OpponentHP = 100;
            OpponentShelterEnergy = 100f;
        }
    }
}
