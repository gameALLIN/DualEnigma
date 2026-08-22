/// ============================================================
/// 文件名: C2SMessages.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 5 种上行消息 DTO（公开化，自 GameServerClient 嵌套类逐字段搬运，
///       字段名/嵌套结构一字不改——协议字节不变铁律）。
///       ⚠️ 上行不套完整信封：结构为 {type, data:{...}}；HighFreqSendData
///       无 playerId 字段（服务端 HighFreqData 无此字段，多余字段曾导致解码失败，
///       禁止"补全"）。
/// 引用：NetProtocolTypes.cs, NetJson.cs
/// ============================================================

using System;
using DualEnigma.Framework.Network;

namespace DualEnigma.Network
{
    /// <summary>进房请求 {type, reqId, data:{roomCode, token}}</summary>
    [Serializable]
    public class C2S_Connect : INetMessage
    {
        public string type = NetProto.Connect;
        public int reqId;
        public Data data = new Data();

        [Serializable]
        public class Data
        {
            public string roomCode = "";
            public string token = "";
        }
    }

    /// <summary>应用层心跳 {type, data:{}}</summary>
    [Serializable]
    public class C2S_Heartbeat : INetMessage
    {
        public string type = NetProto.Heartbeat;
        public Data data = new Data();

        [Serializable]
        public class Data { }
    }

    /// <summary>房主请求开局 {type, reqId, data:{}}</summary>
    [Serializable]
    public class C2S_StartGame : INetMessage
    {
        public string type = NetProto.StartGame;
        public int reqId;
        public Data data = new Data();

        [Serializable]
        public class Data { }
    }

    /// <summary>二维向量（高频状态内嵌）</summary>
    [Serializable]
    public class NetVec2
    {
        public float x;
        public float y;
    }

    /// <summary>
    /// 高频状态上报 {type, data:{...}}。
    /// ⚠️ data 不含 playerId——服务端 HighFreqData 无此字段，
    /// 多余字段曾导致服务端解码失败，禁止补全。
    /// </summary>
    [Serializable]
    public class C2S_HighFreqState : INetMessage
    {
        public string type = NetProto.HighFreqState;
        public Data data;

        [Serializable]
        public class Data
        {
            public NetVec2 position;
            public NetVec2 velocity;
            public string animState;
            public bool facing;
            public int hp;
            public float shelterEnergy;
        }
    }

    /// <summary>碎片接住上报 {type, reqId, data:{fragmentId, posX, posY}}（坐标供服务器几何判定同接；resp(0) 为权威确认锚点）</summary>
    [Serializable]
    public class C2S_FragmentCaught : INetMessage
    {
        public string type = NetProto.FragmentCaught;
        public int reqId;
        public Data data = new Data();

        [Serializable]
        public class Data
        {
            public int fragmentId;
            public float posX;
            public float posY;
        }
    }
}
