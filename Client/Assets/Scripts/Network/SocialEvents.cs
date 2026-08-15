/// ============================================================
/// 文件名: SocialEvents.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 社交通知事件定义。由 SocialNotifyService 发布，
///       全局邀请弹窗与好友面板订阅。
/// ============================================================

using System.Collections.Generic;
using DualEnigma.Framework.Core;

namespace DualEnigma.Network
{
    /// <summary>
    /// 社交通知变化事件（每次轮询完成后发布，携带最新待处理列表）。
    /// 订阅方据此做全量对账：新增卡片 / 移除已处理的卡片。
    /// </summary>
    public struct SocialNotifyChangedEvent : IEventData
    {
        /// <summary>当前待处理邀请</summary>
        public List<InviteData> invites;

        /// <summary>当前待处理好友申请</summary>
        public List<FriendRequestData> requests;
    }
}
