/// ============================================================
/// 文件名: UIFriendsModel.cs
/// 创建时间: 2026-08-15
/// 作者: DualEnigma
/// 描述: 好友面板数据模型。缓存好友/申请/邀请列表。
/// ============================================================

using System.Collections.Generic;
using DualEnigma.Framework.UI;
using DualEnigma.Network;

namespace DualEnigma.UI
{
    public class UIFriendsModel : UIModelBase
    {
        /// <summary>好友列表</summary>
        public List<FriendData> Friends { get; private set; } = new List<FriendData>();

        /// <summary>收到的好友申请</summary>
        public List<FriendRequestData> Requests { get; private set; } = new List<FriendRequestData>();

        /// <summary>收到的房间邀请</summary>
        public List<InviteData> Invites { get; private set; } = new List<InviteData>();

        /// <summary>搜索结果</summary>
        public List<FriendData> SearchResults { get; private set; } = new List<FriendData>();

        public void SetFriends(List<FriendData> friends)
        {
            Friends = friends ?? new List<FriendData>();
        }

        public void SetRequests(List<FriendRequestData> requests)
        {
            Requests = requests ?? new List<FriendRequestData>();
        }

        public void SetInvites(List<InviteData> invites)
        {
            Invites = invites ?? new List<InviteData>();
        }

        public void SetSearchResults(List<FriendData> results)
        {
            SearchResults = results ?? new List<FriendData>();
        }
    }
}
