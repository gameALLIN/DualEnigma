/// ============================================================
/// 文件名: NetEnvelope.cs
/// 创建时间: 2026-08-22
/// 作者: DualEnigma
/// 描述: 通用消息信封（仅下行解析与路由用）。
///       ⚠️ 上行不可用完整信封序列化：C2S 报文外层是 {type, data}，
///       多余字段（timestamp/playerId）曾导致服务端解码失败——
///       上行消息直接各自整体序列化（见 NetJson 注释）。
/// 引用：NetMessageRegistry.cs, NetJson.cs
/// ============================================================

using System;

namespace DualEnigma.Framework.Network
{
    /// <summary>消息信封：type 路由键 + 服务器时钟字段（S2C 专用）。</summary>
    [Serializable]
    public class NetEnvelope
    {
        /// <summary>消息类型标识（唯一路由键）</summary>
        public string type;

        /// <summary>服务器/客户端时钟（仅 S2C 填充；时钟差值法依赖此字段）</summary>
        public long timestamp;

        /// <summary>广播方填充（-1=系统广播）；C2S 不填</summary>
        public int playerId;
    }
}
