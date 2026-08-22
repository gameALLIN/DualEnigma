/// ============================================================
/// 文件名: NetProtocolTypes.cs
/// 创建时间: 2026-08-22
/// 最后更新: 2026-08-22（PC-2：JSON 协议退役，NetProto 常量类删除；
///           字符串常量仅保留作 RequestTracker 的 source 标识，不再进线协议）
/// 作者: DualEnigma
/// 描述: 网络协议辅助类型：请求来源标识（RequestTracker source / NetworkErrorEvent source）
///       + 业务错误码枚举 + NetworkRole。线上协议见 Protocol/proto/game.proto（Protobuf）。
/// 引用：RequestTracker.cs, GameConnection.cs, UIHomeCtrl.cs
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>请求来源标识（仅本地日志/事件 source 用，非线上协议）</summary>
    public static class NetProto
    {
        public const string Connect = "C2S_Connect";
        public const string Heartbeat = "C2S_Heartbeat";
        public const string StartGame = "C2S_StartGame";
        public const string HighFreqState = "C2S_HighFreqState";
        public const string FragmentCaught = "C2S_FragmentCaught";
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
