/// ============================================================
/// 文件名: NetConnError.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 连接层错误码（客户端本地判定，非服务器下发）。
///       与业务层错误码（S2C_Resp/NetErrorCode，R5）严格分层：
///       连接通不通看 NetConnError，请求成不成立看回执 code。
///       ServerDisconnectedEvent.reason 一律经 NetConnErrorText 生成，禁止散落字符串。
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>连接层错误（客户端本地判定）</summary>
    public enum NetConnError
    {
        /// <summary>连不上（握手失败/服务器未启动）</summary>
        ServerUnreachable,

        /// <summary>握手或进房（ConnectAck）超时</summary>
        ConnectTimeout,

        /// <summary>服务器主动断开（含心跳超时被踢）</summary>
        ClosedByServer,

        /// <summary>未归类异常</summary>
        Unknown,
    }

    /// <summary>连接层错误文案唯一出口</summary>
    public static class NetConnErrorText
    {
        /// <summary>获取标准文案（ServerDisconnectedEvent.reason 的唯一来源）</summary>
        public static string ToMessage(NetConnError error)
        {
            switch (error)
            {
                case NetConnError.ServerUnreachable:
                    return "无法连接服务器，请确认 game-server 已启动";
                case NetConnError.ConnectTimeout:
                    return "加入房间超时：房间可能不存在、已满员或已开局";
                case NetConnError.ClosedByServer:
                    return "与服务器的连接已断开";
                default:
                    return "网络异常";
            }
        }
    }
}
