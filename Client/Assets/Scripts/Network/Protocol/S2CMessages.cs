/// ============================================================
/// 文件名: S2CMessages.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 10 种下行消息 DTO（公开化，自 GameServerClient 嵌套类逐字段搬运，
///       字段名/嵌套结构一字不改——协议字节不变铁律）。
///       结构 {type, timestamp?, playerId?, data:{...}}（顶层 playerId 仅
///       OpponentDisconnect 使用）。MidFreqState.shelterEnergy 保持 int
///       （服务器 Math.round 后发送，对齐现状）。
/// 引用：NetProtocolTypes.cs, NetJson.cs
/// ============================================================

using System;
using System.Collections.Generic;
using DualEnigma.Framework.Network;

namespace DualEnigma.Network
{
    /// <summary>进房确认 {data:{playerId, roomCode}}</summary>
    [Serializable]
    public class S2C_ConnectAck : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public int playerId;
            public string roomCode;
        }
    }

    /// <summary>对局开始 {data:{chapter, section, round}}</summary>
    [Serializable]
    public class S2C_GameStart : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public int chapter;
            public int section;
            public int round;
        }
    }

    /// <summary>玩家加入房间广播 {data:{playerId, playerCount}}</summary>
    [Serializable]
    public class S2C_PlayerJoined : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public int playerId;
            public int playerCount;
        }
    }

    /// <summary>
    /// 阶段切换 {type, timestamp, data:{phase, durationMs, phaseEndTime}}。
    /// 时钟差值法依赖信封 timestamp（剩余时长 = phaseEndTime - timestamp）。
    /// </summary>
    [Serializable]
    public class S2C_PhaseChange : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public string phase;
            public int durationMs;
            public long phaseEndTime;
        }
    }

    /// <summary>对方高频状态转发 {data:{playerId, position, velocity, animState, facing, hp, shelterEnergy}}</summary>
    [Serializable]
    public class S2C_HighFreqState : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public int playerId;
            public NetVec2 position;
            public NetVec2 velocity;
            public string animState;
            public bool facing;
            public int hp;
            public float shelterEnergy;
        }
    }

    /// <summary>中频快照 {data:{players:[{playerId, hp, shelterEnergy, carriedFragments}]}}（shelterEnergy 保持 int）</summary>
    [Serializable]
    public class S2C_MidFreqState : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public List<PlayerData> players;
        }

        [Serializable]
        public class PlayerData
        {
            public int playerId;
            public int hp;
            public int shelterEnergy;
            public int[] carriedFragments;
        }
    }

    /// <summary>对手断线 {顶层 playerId=离开者, data:{state: lobby|waiting}}</summary>
    [Serializable]
    public class S2C_OpponentDisconnect : INetMessage
    {
        public string type;
        public long timestamp;
        public int playerId;
        public Data data;

        [Serializable]
        public class Data
        {
            public string state;
        }
    }

    /// <summary>碎片掉落计划 {data:{plan:[{fragmentId, type, position, dropTime, seed}]}}</summary>
    [Serializable]
    public class S2C_FragmentDropPlan : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public List<PlanItem> plan;
        }

        [Serializable]
        public class PlanItem
        {
            public int fragmentId;
            public int type;
            public NetVec2 position;
            public float dropTime;
            public long seed;
        }
    }

    /// <summary>碎片接住结果广播 {data:{fragmentId, playerId, multiplier, isSimultaneous}}</summary>
    [Serializable]
    public class S2C_FragmentResult : INetMessage
    {
        public string type;
        public long timestamp;
        public Data data;

        [Serializable]
        public class Data
        {
            public int fragmentId;
            public int playerId;
            public int multiplier;
            public bool isSimultaneous;
        }
    }

    /// <summary>心跳回应（RTT 语义专用）</summary>
    [Serializable]
    public class S2C_HeartbeatAck : INetMessage
    {
        public string type;
        public long timestamp;
    }

    /// <summary>
    /// 统一请求回执 {data:{reqId, code, message}}。
    /// 每个携带 reqId 的 C2S 请求必须且仅收到一条：code=0 成功、非 0 失败原因。
    /// 高频流（C2S_HighFreqState）豁免；心跳以 S2C_HeartbeatAck 为专属回执。
    /// </summary>
    [Serializable]
    public class S2C_Resp : INetMessage
    {
        public string type;
        public long timestamp;
        public int playerId;
        public Data data;

        [Serializable]
        public class Data
        {
            public int reqId;
            public int code;
            public string message;
        }
    }
}
