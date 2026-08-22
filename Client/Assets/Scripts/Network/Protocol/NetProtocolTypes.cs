/// ============================================================
/// 文件名: NetProtocolTypes.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 协议 type 字符串常量（与 Server/network Message 子类 @Type 一一对应，
///       禁止改动——协议字节不变铁律）。NetworkRole 枚举自 NetworkEnums.cs 迁入。
/// 引用：C2SMessages.cs, S2CMessages.cs
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>协议 type 字符串常量（唯一路由键，双端契约）</summary>
    public static class NetProto
    {
        // ── C2S 上行 ──
        public const string Connect = "C2S_Connect";
        public const string Heartbeat = "C2S_Heartbeat";
        public const string StartGame = "C2S_StartGame";
        public const string HighFreqState = "C2S_HighFreqState";
        public const string FragmentCaught = "C2S_FragmentCaught";

        // ── S2C 下行 ──
        public const string ConnectAck = "S2C_ConnectAck";
        public const string GameStart = "S2C_GameStart";
        public const string PlayerJoined = "S2C_PlayerJoined";
        public const string PhaseChange = "S2C_PhaseChange";
        public const string HighFreqStateS2C = "S2C_HighFreqState";
        public const string MidFreqState = "S2C_MidFreqState";
        public const string OpponentDisconnect = "S2C_OpponentDisconnect";
        public const string FragmentDropPlan = "S2C_FragmentDropPlan";
        public const string FragmentResult = "S2C_FragmentResult";
        public const string HeartbeatAck = "S2C_HeartbeatAck";
        public const string Resp = "S2C_Resp";
    }

    /// <summary>
    /// 业务层错误码（S2C_Resp.code，双端单一事实来源，逐项镜像服务器 NetErrorCode.java，禁止单侧改动）。
    /// 与连接层 NetConnError 严格分层：连接通不通看 NetConnError，请求成不成立看回执 code。
    /// </summary>
    public enum NetErrorCode
    {
        /// <summary>成功</summary>
        Ok = 0,

        /// <summary>回执超时（客户端本地判定，服务器不下发）</summary>
        LocalTimeout = -1,

        /// <summary>Token 校验失败（预留，当前匿名放行策略下不下发）</summary>
        TokenInvalid = 1001,

        /// <summary>未支持的消息类型 / 解码失败</summary>
        UnknownType = 1002,

        /// <summary>房间不存在</summary>
        RoomNotFound = 2001,

        /// <summary>房间已满</summary>
        RoomFull = 2002,

        /// <summary>对局已开始（拒绝进房）</summary>
        GameStarted = 2003,

        /// <summary>非房主（拒绝开局）</summary>
        NotHost = 3001,

        /// <summary>未满员（拒绝开局）</summary>
        NotFull = 3002,

        /// <summary>对局已在进行（拒绝开局）</summary>
        AlreadyStarted = 3003,

        /// <summary>碎片上报被拒（不在判定半径）</summary>
        FragmentRejected = 4002,
    }

    /// <summary>
    /// 网络角色身份（自 NetworkEnums.cs 迁入，字段原样）。
    /// 引用：网络通信.md §2.2 Host-Client 模式
    /// </summary>
    public enum NetworkRole
    {
        /// <summary>房主（权威状态管理）</summary>
        Host,
        /// <summary>加入方（本地预测+接收同步）</summary>
        Client,
    }
}
