/// ============================================================
/// 文件名: NetworkEnums.cs
/// 创建时间: 2026-07-13
/// 作者: DualEnigma
/// 描述: 网络系统相关枚举定义。
/// ============================================================

namespace DualEnigma.Network
{
    /// <summary>
    /// 同步类型分类。
    /// 引用：网络通信.md §3.1 同步类型总览
    /// </summary>
    public enum SyncType
    {
        /// <summary>高频状态（角色位置/速度/动画，20Hz）</summary>
        HighFrequency,
        /// <summary>中频状态（HP/能量/碎片位置，10Hz）</summary>
        MidFrequency,
        /// <summary>关键事件（碎片接住/建筑放置/技能释放）</summary>
        KeyEvent,
        /// <summary>阶段切换（可靠有序）</summary>
        PhaseChange,
        /// <summary>天赋选择（可靠）</summary>
        TalentSelect,
    }

    /// <summary>
    /// 网络角色身份。
    /// 引用：网络通信.md §2.2 Host-Client 模式
    /// </summary>
    public enum NetworkRole
    {
        /// <summary>房主（权威状态管理）</summary>
        Host,
        /// <summary>加入方（本地预测+接收同步）</summary>
        Client,
    }

    /// <summary>
    /// 网络消息类型标识。用于消息头中区分消息类型。
    /// 引用：网络通信.md §3.1 同步类型总览
    /// </summary>
    public enum MessageType : ushort
    {
        /// <summary>高频状态（20Hz，不可靠UDP）</summary>
        HighFrequencyState,
        /// <summary>中频状态（10Hz，可靠UDP）</summary>
        MidFrequencyState,
        /// <summary>关键事件（可靠有序TCP）</summary>
        KeyEvent,
        /// <summary>阶段切换（可靠有序TCP）</summary>
        PhaseChange,
        /// <summary>天赋选择（可靠TCP）</summary>
        TalentSelect,
        /// <summary>重连快照（可靠有序TCP）</summary>
        ReconnectSnapshot,
        /// <summary>心跳包</summary>
        Heartbeat,
        /// <summary>心跳回应包</summary>
        HeartbeatAck,
        /// <summary>中频状态确认包（可靠UDP用）</summary>
        MidFreqAck,
    }

    /// <summary>
    /// 网络连接状态。
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>未连接</summary>
        Disconnected,
        /// <summary>Host 监听中，等待 Client 连接</summary>
        Listening,
        /// <summary>Client 连接中</summary>
        Connecting,
        /// <summary>已连接</summary>
        Connected,
    }

    /// <summary>
    /// 断线重连状态。
    /// 引用：网络通信.md §6.1 断线处理时间轴
    /// </summary>
    public enum ReconnectState
    {
        /// <summary>正常连接中</summary>
        Connected,
        /// <summary>已断线，等待重连（0-30秒）</summary>
        WaitingReconnect,
        /// <summary>AI 接管（30-120秒）</summary>
        AiTakeover,
        /// <summary>最终超时，游戏结束</summary>
        FinalTimeout,
    }
}
